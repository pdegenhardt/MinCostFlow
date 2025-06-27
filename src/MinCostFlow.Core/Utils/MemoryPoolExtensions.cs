using System.Buffers;
using System.Runtime.CompilerServices;

namespace MinCostFlow.Core.Utils;

/// <summary>
/// Extension methods for memory pool usage.
/// </summary>
public static class MemoryPoolExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledArray<T> RentScoped<T>(this ArrayPool<T> pool, int minimumLength, bool clearOnReturn = false)
    {
        return new PooledArray<T>(pool, minimumLength, clearOnReturn);
    }
}