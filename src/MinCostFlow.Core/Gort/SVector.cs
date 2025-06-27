using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Symmetric Vector - A specialized container that provides bidirectional indexing 
/// with both negative and positive indices.
/// Valid indices range from -size to size-1 (inclusive).
/// </summary>
/// <typeparam name="T">The type of elements stored in the vector</typeparam>
public class SVector<T> : IEnumerable<T>
{
    private T[] _data;
    private int _size;
    private int _capacity;
    private const double GrowthFactor = 1.3;

    /// <summary>
    /// Gets the number of elements on each side of the vector.
    /// Total elements = 2 * Size
    /// </summary>
    public int Size => _size;

    /// <summary>
    /// Gets the current reserved space on each side.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Creates an empty SVector.
    /// </summary>
    public SVector()
    {
        _data = Array.Empty<T>();
        _size = 0;
        _capacity = 0;
    }

    /// <summary>
    /// Creates a deep copy of another SVector.
    /// </summary>
    public SVector(SVector<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        _size = other._size;
        _capacity = other._capacity;
        
        if (_capacity > 0)
        {
            _data = new T[2 * _capacity];
            // Copy all elements from the other vector
            for (int i = -_size; i < _size; i++)
            {
                this[i] = other[i];
            }
        }
        else
        {
            _data = Array.Empty<T>();
        }
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// Valid indices: [-size, size-1]
    /// </summary>
    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(index >= -_size && index < _size, 
                $"Index {index} out of range [-{_size}, {_size})");
            return _data[IndexToPhysical(index)];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            Debug.Assert(index >= -_size && index < _size, 
                $"Index {index} out of range [-{_size}, {_size})");
            _data[IndexToPhysical(index)] = value;
        }
    }

    /// <summary>
    /// Ensures capacity is at least n on each side.
    /// </summary>
    public void Reserve(int n)
    {
        if (n <= _capacity)
            return;

        Reallocate(n);
    }

    /// <summary>
    /// Changes size to exactly n.
    /// If growing, new elements are default-initialized.
    /// If shrinking, excess elements are destroyed.
    /// </summary>
    public void Resize(int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n), "Size cannot be negative");

        if (n > _capacity)
        {
            Reserve(n);
        }

        if (n > _size)
        {
            // Growing - default initialize new elements
            int physicalStart = IndexToPhysical(-n);
            int physicalEnd = IndexToPhysical(-_size - 1);
            if (physicalStart <= physicalEnd)
            {
                Array.Clear(_data, physicalStart, physicalEnd - physicalStart + 1);
            }
            else
            {
                Array.Clear(_data, physicalStart, _data.Length - physicalStart);
                Array.Clear(_data, 0, physicalEnd + 1);
            }

            physicalStart = IndexToPhysical(_size);
            physicalEnd = IndexToPhysical(n - 1);
            if (physicalStart <= physicalEnd)
            {
                Array.Clear(_data, physicalStart, physicalEnd - physicalStart + 1);
            }
            else
            {
                Array.Clear(_data, physicalStart, _data.Length - physicalStart);
                Array.Clear(_data, 0, physicalEnd + 1);
            }
        }
        else if (n < _size)
        {
            // Shrinking - clear excess elements
            for (int i = -_size; i < -n; i++)
            {
                _data[IndexToPhysical(i)] = default;
            }
            for (int i = n; i < _size; i++)
            {
                _data[IndexToPhysical(i)] = default;
            }
        }

        _size = n;
    }

    /// <summary>
    /// Adds one element at each end.
    /// </summary>
    public void Grow(T leftValue, T rightValue)
    {
        if (_size == _capacity)
        {
            // Need to handle potential self-reference before reallocation
            bool leftIsSelfRef = false;
            bool rightIsSelfRef = false;
            int leftIndex = -1, rightIndex = -1;

            if (_size > 0 && !typeof(T).IsValueType)
            {
                for (int i = -_size; i < _size; i++)
                {
                    if (ReferenceEquals(_data[IndexToPhysical(i)], leftValue))
                    {
                        leftIsSelfRef = true;
                        leftIndex = i;
                    }
                    if (ReferenceEquals(_data[IndexToPhysical(i)], rightValue))
                    {
                        rightIsSelfRef = true;
                        rightIndex = i;
                    }
                }
            }

            if (leftIsSelfRef || rightIsSelfRef)
            {
                // Copy values before reallocation
                T leftCopy = leftIsSelfRef ? this[leftIndex] : leftValue;
                T rightCopy = rightIsSelfRef ? this[rightIndex] : rightValue;
                
                int newCapacity = CalculateNewCapacity(_size + 1);
                Reallocate(newCapacity);
                
                leftValue = leftCopy;
                rightValue = rightCopy;
            }
            else
            {
                int newCapacity = CalculateNewCapacity(_size + 1);
                Reallocate(newCapacity);
            }
        }

        _size++;
        this[-_size] = leftValue;
        this[_size - 1] = rightValue;
    }

    /// <summary>
    /// Removes all elements but keeps capacity.
    /// </summary>
    public void Clear()
    {
        if (_size > 0)
        {
            Array.Clear(_data, 0, _data.Length);
            _size = 0;
        }
    }

    /// <summary>
    /// Clears all elements and releases memory.
    /// </summary>
    public void ClearAndDealloc()
    {
        _data = Array.Empty<T>();
        _size = 0;
        _capacity = 0;
    }

    /// <summary>
    /// Exchanges contents with another SVector.
    /// </summary>
    public void Swap(SVector<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        // Swap all fields
        (_data, other._data) = (other._data, _data);
        (_size, other._size) = (other._size, _size);
        (_capacity, other._capacity) = (other._capacity, _capacity);
    }

    /// <summary>
    /// Converts logical index to physical array index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexToPhysical(int logicalIndex)
    {
        if (logicalIndex < 0)
            return _capacity + logicalIndex;
        else
            return _capacity + logicalIndex;
    }

    /// <summary>
    /// Calculates new capacity based on growth factor.
    /// </summary>
    private int CalculateNewCapacity(int requiredSize)
    {
        if (_capacity == 0)
            return Math.Max(4, requiredSize);

        long newCapacity = (long)Math.Ceiling(_capacity * GrowthFactor);
        newCapacity = Math.Max(newCapacity, requiredSize);
        
        // Handle potential overflow
        if (newCapacity > int.MaxValue / 2)
            throw new OutOfMemoryException("SVector capacity would exceed maximum size");

        return (int)newCapacity;
    }

    /// <summary>
    /// Reallocates internal storage to new capacity.
    /// </summary>
    private void Reallocate(int newCapacity)
    {
        if (newCapacity <= _capacity)
            return;

        T[] newData = new T[2 * newCapacity];

        if (_size > 0)
        {
            // Copy negative elements
            for (int i = -_size; i < 0; i++)
            {
                newData[newCapacity + i] = _data[IndexToPhysical(i)];
            }

            // Copy positive elements
            for (int i = 0; i < _size; i++)
            {
                newData[newCapacity + i] = _data[IndexToPhysical(i)];
            }
        }

        _data = newData;
        _capacity = newCapacity;
    }

    #region IEnumerable Implementation

    /// <summary>
    /// Returns an enumerator that iterates from -size to size-1.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = -_size; i < _size; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns an enumerable for positive indices only [0, size).
    /// </summary>
    public IEnumerable<T> PositiveRange()
    {
        for (int i = 0; i < _size; i++)
        {
            yield return this[i];
        }
    }

    /// <summary>
    /// Returns an enumerable for negative indices only [-size, 0).
    /// </summary>
    public IEnumerable<T> NegativeRange()
    {
        for (int i = -_size; i < 0; i++)
        {
            yield return this[i];
        }
    }

    #endregion

    /// <summary>
    /// Creates a string representation of the SVector.
    /// </summary>
    public override string ToString()
    {
        if (_size == 0)
            return "SVector<" + typeof(T).Name + ">: []";

        var sb = new System.Text.StringBuilder();
        sb.Append("SVector<").Append(typeof(T).Name).Append(">: [");
        
        for (int i = -_size; i < _size; i++)
        {
            if (i > -_size)
                sb.Append(", ");
            sb.Append(i).Append(":").Append(this[i]);
        }
        
        sb.Append("]");
        return sb.ToString();
    }
}