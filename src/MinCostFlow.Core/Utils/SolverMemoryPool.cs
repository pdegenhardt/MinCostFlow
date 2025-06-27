using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MinCostFlow.Core.Utils;

/// <summary>
/// Memory pool optimized for Network Simplex solver operations.
/// Pre-allocates common buffer sizes to reduce allocation overhead.
/// </summary>
public sealed class SolverMemoryPool : IDisposable
{
    private readonly ArrayPool<int> _intPool;
    private readonly ArrayPool<long> _longPool;
    private readonly ArrayPool<sbyte> _sbytePool;
    
    // Pre-allocated buffers for common sizes
    private readonly int[][] _preAllocatedIntBuffers;
    private readonly long[][] _preAllocatedLongBuffers;
    private readonly bool[] _intBufferInUse;
    private readonly bool[] _longBufferInUse;
    
    // Common buffer sizes based on problem characteristics
    private static readonly int[] CommonIntSizes = { 64, 256, 1024, 4096 };
    private static readonly int[] CommonLongSizes = { 64, 256, 1024, 4096 };
    
    public SolverMemoryPool()
    {
        _intPool = ArrayPool<int>.Create();
        _longPool = ArrayPool<long>.Create();
        _sbytePool = ArrayPool<sbyte>.Create();
        
        // Pre-allocate common sizes
        _preAllocatedIntBuffers = new int[CommonIntSizes.Length][];
        _intBufferInUse = new bool[CommonIntSizes.Length];
        for (int i = 0; i < CommonIntSizes.Length; i++)
        {
            _preAllocatedIntBuffers[i] = new int[CommonIntSizes[i]];
        }
        
        _preAllocatedLongBuffers = new long[CommonLongSizes.Length][];
        _longBufferInUse = new bool[CommonLongSizes.Length];
        for (int i = 0; i < CommonLongSizes.Length; i++)
        {
            _preAllocatedLongBuffers[i] = new long[CommonLongSizes[i]];
        }
    }
    
    /// <summary>
    /// Pre-allocate buffers based on expected problem size.
    /// </summary>
    public void PreAllocate(int nodeCount, int arcCount)
    {
        // Warm up the pools with expected sizes
        int sqrtArcs = (int)Math.Sqrt(arcCount);
        
        // Rent and return to warm up pool
        WarmUpPool(_intPool, nodeCount);
        WarmUpPool(_intPool, arcCount);
        WarmUpPool(_intPool, sqrtArcs);
        
        WarmUpPool(_longPool, nodeCount);
        WarmUpPool(_longPool, arcCount);
        
        WarmUpPool(_sbytePool, arcCount);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] RentIntArray(int minimumLength)
    {
        // Check pre-allocated buffers first
        for (int i = 0; i < CommonIntSizes.Length; i++)
        {
            if (CommonIntSizes[i] >= minimumLength && !_intBufferInUse[i])
            {
                _intBufferInUse[i] = true;
                Array.Clear(_preAllocatedIntBuffers[i], 0, minimumLength);
                return _preAllocatedIntBuffers[i];
            }
        }
        
        return _intPool.Rent(minimumLength);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnIntArray(int[] array, bool clearArray = false)
    {
        if (array == null)
        {
            return;
        }

        // Check if it's a pre-allocated buffer
        for (int i = 0; i < CommonIntSizes.Length; i++)
        {
            if (ReferenceEquals(array, _preAllocatedIntBuffers[i]))
            {
                _intBufferInUse[i] = false;
                return;
            }
        }
        
        _intPool.Return(array, clearArray);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long[] RentLongArray(int minimumLength)
    {
        // Check pre-allocated buffers first
        for (int i = 0; i < CommonLongSizes.Length; i++)
        {
            if (CommonLongSizes[i] >= minimumLength && !_longBufferInUse[i])
            {
                _longBufferInUse[i] = true;
                Array.Clear(_preAllocatedLongBuffers[i], 0, minimumLength);
                return _preAllocatedLongBuffers[i];
            }
        }
        
        return _longPool.Rent(minimumLength);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnLongArray(long[] array, bool clearArray = false)
    {
        if (array == null)
        {
            return;
        }

        // Check if it's a pre-allocated buffer
        for (int i = 0; i < CommonLongSizes.Length; i++)
        {
            if (ReferenceEquals(array, _preAllocatedLongBuffers[i]))
            {
                _longBufferInUse[i] = false;
                return;
            }
        }
        
        _longPool.Return(array, clearArray);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte[] RentSByteArray(int minimumLength)
    {
        return _sbytePool.Rent(minimumLength);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnSByteArray(sbyte[] array, bool clearArray = false)
    {
        if (array != null)
        {
            _sbytePool.Return(array, clearArray);
        }
    }
    
    private static void WarmUpPool<T>(ArrayPool<T> pool, int size)
    {
        var temp = pool.Rent(size);
        pool.Return(temp);
    }
    
    public void Dispose()
    {
        // Arrays will be garbage collected
    }
}

/// <summary>
/// Scoped rental of an array from memory pool.
/// </summary>
public readonly struct PooledArray<T>(ArrayPool<T> pool, int minimumLength, bool clearOnReturn = false) : IDisposable
{
    private readonly ArrayPool<T> _pool = pool;
    private readonly T[] _array = pool.Rent(minimumLength);
    private readonly bool _clearOnReturn = clearOnReturn;
    
    public T[] Array => _array;
    public int Length => _array.Length;

    public void Dispose()
    {
        if (_array != null)
        {
            _pool.Return(_array, _clearOnReturn);
        }
    }
    
    public Span<T> AsSpan() => _array.AsSpan();
    public Span<T> AsSpan(int length) => _array.AsSpan(0, length);
}
