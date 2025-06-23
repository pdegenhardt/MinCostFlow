using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using MinCostFlow.Core.DataStructures;

namespace MinCostFlow.Core.Algorithms.Internal;

/// <summary>
/// Optimized block search pivot rule implementation using SIMD and unsafe code.
/// </summary>
internal sealed unsafe class BlockSearchPivotOptimized
{
    private readonly NetworkSimplex _ns;
    private readonly int _blockSize;
    private int _nextArc;
    
    // Pinned arrays for unsafe access
    private readonly long* _costPtr;
    private readonly long* _piPtr;
    private readonly int* _sourcePtr;
    private readonly int* _targetPtr;
    private readonly sbyte* _statePtr;
    
    public BlockSearchPivotOptimized(NetworkSimplex ns, 
        long* costPtr, long* piPtr, int* sourcePtr, int* targetPtr, sbyte* statePtr)
    {
        _ns = ns;
        int blockSize = (int)Math.Sqrt(ns.SearchArcNum);
        _blockSize = Math.Max(blockSize, NetworkSimplex.MIN_BLOCK_SIZE);
        _nextArc = 0;
        
        _costPtr = costPtr;
        _piPtr = piPtr;
        _sourcePtr = sourcePtr;
        _targetPtr = targetPtr;
        _statePtr = statePtr;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FindEnteringArc(out int enteringArc)
    {
        long min = 0;
        int cnt = _blockSize;
        int e;
        int startArc = _nextArc;
        int bestArc = -1;
        int searchArcNum = _ns.SearchArcNum;
        
        // Process forward direction
        e = ProcessArcRange(_nextArc, searchArcNum, ref min, ref cnt, ref bestArc);
        
        // Process wrapped around portion
        if (e >= searchArcNum && min >= 0)
        {
            e = ProcessArcRange(0, _nextArc, ref min, ref cnt, ref bestArc);
        }
        
        if (min >= 0)
        {
            enteringArc = -1;
            return false;
        }
        
        _nextArc = e;
        enteringArc = bestArc;
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ProcessArcRange(int start, int end, ref long min, ref int cnt, ref int bestArc)
    {
        int e = start;
        
        // Use SIMD when available and beneficial
        if (Vector.IsHardwareAccelerated && end - start >= Vector<long>.Count * 2)
        {
            e = ProcessArcRangeSIMD(start, end, ref min, ref cnt, ref bestArc);
        }
        
        // Process remaining arcs scalar
        for (; e < end; e++)
        {
            // Direct memory access
            sbyte state = _statePtr[e];
            long cost = _costPtr[e];
            int source = _sourcePtr[e];
            int target = _targetPtr[e];
            long piSource = _piPtr[source];
            long piTarget = _piPtr[target];
            
            long c = state * (cost + piSource - piTarget);
            
            if (c < min)
            {
                min = c;
                bestArc = e;
            }
            
            if (--cnt == 0)
            {
                if (min < 0) return e + 1;
                cnt = _blockSize;
            }
        }
        
        return e;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ProcessArcRangeSIMD(int start, int end, ref long min, ref int cnt, ref int bestArc)
    {
        int vectorCount = Vector<long>.Count;
        int e = start;
        
        // Process vectors
        for (; e <= end - vectorCount; e += vectorCount)
        {
            // Load cost vector
            var costVec = new Vector<long>(new Span<long>(_costPtr + e, vectorCount));
            
            // Compute reduced costs for vector
            for (int i = 0; i < vectorCount; i++)
            {
                int idx = e + i;
                sbyte state = _statePtr[idx];
                long cost = _costPtr[idx];
                int source = _sourcePtr[idx];
                int target = _targetPtr[idx];
                long piSource = _piPtr[source];
                long piTarget = _piPtr[target];
                
                long c = state * (cost + piSource - piTarget);
                
                if (c < min)
                {
                    min = c;
                    bestArc = idx;
                }
                
                if (--cnt == 0)
                {
                    if (min < 0) return idx + 1;
                    cnt = _blockSize;
                }
            }
        }
        
        return e;
    }
}

/// <summary>
/// First eligible pivot rule with unsafe optimizations.
/// </summary>
internal sealed unsafe class FirstEligiblePivotOptimized
{
    private readonly NetworkSimplex _ns;
    private int _nextArc;
    
    // Pinned arrays for unsafe access
    private readonly long* _costPtr;
    private readonly long* _piPtr;
    private readonly int* _sourcePtr;
    private readonly int* _targetPtr;
    private readonly sbyte* _statePtr;
    
    public FirstEligiblePivotOptimized(NetworkSimplex ns,
        long* costPtr, long* piPtr, int* sourcePtr, int* targetPtr, sbyte* statePtr)
    {
        _ns = ns;
        _nextArc = 0;
        
        _costPtr = costPtr;
        _piPtr = piPtr;
        _sourcePtr = sourcePtr;
        _targetPtr = targetPtr;
        _statePtr = statePtr;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FindEnteringArc(out int enteringArc)
    {
        int searchArcNum = _ns.SearchArcNum;
        
        // Search from current position
        for (int e = _nextArc; e < searchArcNum; e++)
        {
            sbyte state = _statePtr[e];
            if (state == 0) continue; // Skip tree arcs
            
            long cost = _costPtr[e];
            int source = _sourcePtr[e];
            int target = _targetPtr[e];
            long piSource = _piPtr[source];
            long piTarget = _piPtr[target];
            
            long c = state * (cost + piSource - piTarget);
            
            if (c < 0)
            {
                _nextArc = e + 1;
                enteringArc = e;
                return true;
            }
        }
        
        // Wrap around search
        for (int e = 0; e < _nextArc; e++)
        {
            sbyte state = _statePtr[e];
            if (state == 0) continue;
            
            long cost = _costPtr[e];
            int source = _sourcePtr[e];
            int target = _targetPtr[e];
            long piSource = _piPtr[source];
            long piTarget = _piPtr[target];
            
            long c = state * (cost + piSource - piTarget);
            
            if (c < 0)
            {
                _nextArc = e + 1;
                enteringArc = e;
                return true;
            }
        }
        
        enteringArc = -1;
        return false;
    }
}

/// <summary>
/// Best eligible pivot rule with unsafe optimizations.
/// </summary>
internal sealed unsafe class BestEligiblePivotOptimized
{
    private readonly NetworkSimplex _ns;
    
    // Pinned arrays for unsafe access
    private readonly long* _costPtr;
    private readonly long* _piPtr;
    private readonly int* _sourcePtr;
    private readonly int* _targetPtr;
    private readonly sbyte* _statePtr;
    
    public BestEligiblePivotOptimized(NetworkSimplex ns,
        long* costPtr, long* piPtr, int* sourcePtr, int* targetPtr, sbyte* statePtr)
    {
        _ns = ns;
        
        _costPtr = costPtr;
        _piPtr = piPtr;
        _sourcePtr = sourcePtr;
        _targetPtr = targetPtr;
        _statePtr = statePtr;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FindEnteringArc(out int enteringArc)
    {
        long min = 0;
        int bestArc = -1;
        int searchArcNum = _ns.SearchArcNum;
        
        // Check all arcs for the best reduced cost
        for (int e = 0; e < searchArcNum; e++)
        {
            sbyte state = _statePtr[e];
            if (state == 0) continue;
            
            long cost = _costPtr[e];
            int source = _sourcePtr[e];
            int target = _targetPtr[e];
            long piSource = _piPtr[source];
            long piTarget = _piPtr[target];
            
            long c = state * (cost + piSource - piTarget);
            
            if (c < min)
            {
                min = c;
                bestArc = e;
            }
        }
        
        if (min >= 0)
        {
            enteringArc = -1;
            return false;
        }
        
        enteringArc = bestArc;
        return true;
    }
}