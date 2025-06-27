using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// A zero-indexed vector that supports negative indices, ported from OR-Tools' ZVector.
/// Uses pointer arithmetic to allow direct indexing without offset calculations.
/// </summary>
/// <typeparam name="T">The type of elements stored in the vector (must be unmanaged)</typeparam>
public sealed unsafe class ZVector<T> : IDisposable where T : unmanaged
{
    private T* _base;
    private T[] _storage;
    private GCHandle _handle;
    private long _minIndex;
    private long _maxIndex;
    private long _size;
    private bool _disposed;

    /// <summary>
    /// Creates a new ZVector with the specified index range.
    /// </summary>
    /// <param name="minIndex">The minimum index (inclusive)</param>
    /// <param name="maxIndex">The maximum index (inclusive)</param>
    public ZVector(long minIndex, long maxIndex)
    {
        if (maxIndex < minIndex)
        {
            throw new ArgumentException($"maxIndex ({maxIndex}) must be >= minIndex ({minIndex})");
        }

        _minIndex = minIndex;
        _maxIndex = maxIndex;
        _size = maxIndex - minIndex + 1;

        // Allocate storage and pin it
        _storage = new T[_size];
        _handle = GCHandle.Alloc(_storage, GCHandleType.Pinned);
        
        // Set base pointer with offset so that _base[index] works directly
        T* storagePtr = (T*)_handle.AddrOfPinnedObject();
        _base = storagePtr - minIndex;
    }

    /// <summary>
    /// Creates a new ZVector for arc storage where reverse arcs have negative indices.
    /// </summary>
    /// <param name="maxArcs">The maximum number of forward arcs</param>
    /// <returns>A ZVector with range [-maxArcs, maxArcs-1]</returns>
    public static ZVector<T> ForArcs(int maxArcs)
    {
        return new ZVector<T>(-maxArcs, maxArcs - 1);
    }

    /// <summary>
    /// Gets the minimum valid index.
    /// </summary>
    public long MinIndex => _minIndex;

    /// <summary>
    /// Gets the maximum valid index.
    /// </summary>
    public long MaxIndex => _maxIndex;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The index (can be negative)</param>
    public T this[long index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if DEBUG
            if (index < _minIndex || index > _maxIndex)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range [{_minIndex}, {_maxIndex}]");
            }
#endif
            return _base[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
#if DEBUG
            if (index < _minIndex || index > _maxIndex)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range [{_minIndex}, {_maxIndex}]");
            }
#endif
            _base[index] = value;
        }
    }

    /// <summary>
    /// Gets or sets the element at the specified index (int overload for convenience).
    /// </summary>
    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[(long)index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => this[(long)index] = value;
    }

    /// <summary>
    /// Returns the value stored at index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Value(long index)
    {
#if DEBUG
        if (index < _minIndex || index > _maxIndex)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range [{_minIndex}, {_maxIndex}]");
        }
#endif
        return _base[index];
    }

    /// <summary>
    /// Sets to value the content of the array at index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(long index, T value)
    {
#if DEBUG
        if (index < _minIndex || index > _maxIndex)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range [{_minIndex}, {_maxIndex}]");
        }
#endif
        _base[index] = value;
    }

    /// <summary>
    /// Sets all elements in the array to the specified value.
    /// </summary>
    public void SetAll(T value)
    {
        // For zero values, use Array.Clear for better performance
        if (IsZero(value))
        {
            Array.Clear(_storage, 0, _storage.Length);
        }
        else
        {
            // Use span for efficient bulk operation
            var span = _storage.AsSpan();
            span.Fill(value);
        }
    }

    /// <summary>
    /// Clears all elements to their default value.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_storage, 0, _storage.Length);
    }

    /// <summary>
    /// Creates a copy of this ZVector.
    /// </summary>
    public ZVector<T> Clone()
    {
        var clone = new ZVector<T>(_minIndex, _maxIndex);
        Array.Copy(_storage, clone._storage, _storage.Length);
        return clone;
    }

    /// <summary>
    /// Copies data from another ZVector.
    /// </summary>
    public void CopyFrom(ZVector<T> other)
    {
        if (other._minIndex != _minIndex || other._maxIndex != _maxIndex)
        {
            throw new ArgumentException("ZVectors must have the same index range");
        }
        Array.Copy(other._storage, _storage, _storage.Length);
    }

    /// <summary>
    /// Returns a span over the underlying storage for efficient bulk operations.
    /// Note: The span is 0-based, not using the ZVector's index range.
    /// </summary>
    public Span<T> AsSpan()
    {
        return _storage.AsSpan();
    }

    /// <summary>
    /// Gets a pointer to the element at the specified index.
    /// Use with caution - no bounds checking in release mode.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* GetPointer(long index)
    {
#if DEBUG
        if (index < _minIndex || index > _maxIndex)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range [{_minIndex}, {_maxIndex}]");
        }
#endif
        return &_base[index];
    }

    private static bool IsZero(T value)
    {
        // Efficient zero checking for common types
        if (typeof(T) == typeof(long))
        {
            return Unsafe.As<T, long>(ref value) == 0;
        }
        if (typeof(T) == typeof(int))
        {
            return Unsafe.As<T, int>(ref value) == 0;
        }
        if (typeof(T) == typeof(double))
        {
            return Unsafe.As<T, double>(ref value) == 0.0;
        }
        
        // Generic comparison
        return value.Equals(default(T));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
            _base = null;
            _storage = null;
            _disposed = true;
        }
    }
}