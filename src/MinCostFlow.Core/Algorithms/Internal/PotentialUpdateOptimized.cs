using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using MinCostFlow.Core.DataStructures;

namespace MinCostFlow.Core.Algorithms.Internal;

/// <summary>
/// Optimized potential update operations using SIMD and unsafe code.
/// </summary>
internal static unsafe class PotentialUpdateOptimized
{
    private const int CacheLineSize = 64;
    private const int LongsPerCacheLine = CacheLineSize / sizeof(long);
    
    /// <summary>
    /// Updates potentials for a subtree using SIMD when beneficial.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdatePotentials(
        long* pi, int* thread, int* lastSucc,
        int startNode, long delta)
    {
        int endNode = thread[lastSucc[startNode]];
        int nodeCount = CountNodes(thread, startNode, endNode);
        
        // Choose strategy based on subtree size
        if (nodeCount < 8)
        {
            // Small subtree - simple scalar update
            UpdatePotentialsScalar(pi, thread, startNode, endNode, delta);
        }
        else if (Vector.IsHardwareAccelerated && nodeCount >= Vector<long>.Count * 2)
        {
            // Large subtree - use SIMD
            UpdatePotentialsSIMD(pi, thread, startNode, endNode, delta);
        }
        else
        {
            // Medium subtree - cache-friendly scalar
            UpdatePotentialsCacheFriendly(pi, thread, startNode, endNode, delta);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdatePotentialsScalar(
        long* pi, int* thread, int startNode, int endNode, long delta)
    {
        for (int u = startNode; u != endNode; u = thread[u])
        {
            pi[u] += delta;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdatePotentialsSIMD(
        long* pi, int* thread, int startNode, int endNode, long delta)
    {
        // Collect nodes into a buffer for vectorized processing
        const int BufferSize = 256;
        int* nodeBuffer = stackalloc int[BufferSize];
        
        int bufferPos = 0;
        int u = startNode;
        
        var deltaVec = new Vector<long>(delta);
        
        while (u != endNode)
        {
            nodeBuffer[bufferPos++] = u;
            u = thread[u];
            
            // Process buffer when full or at end
            if (bufferPos == BufferSize || u == endNode)
            {
                ProcessNodeBufferSIMD(pi, nodeBuffer, bufferPos, deltaVec);
                bufferPos = 0;
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessNodeBufferSIMD(
        long* pi, int* nodes, int count, Vector<long> deltaVec)
    {
        int vectorCount = Vector<long>.Count;
        int i = 0;
        
        // Process groups that can be vectorized
        long* tempBuffer = stackalloc long[vectorCount];
        
        for (; i <= count - vectorCount; i += vectorCount)
        {
            // Gather potentials from scattered nodes
            for (int j = 0; j < vectorCount; j++)
            {
                tempBuffer[j] = pi[nodes[i + j]];
            }
            
            // Apply delta using SIMD
            var potVec = new Vector<long>(new Span<long>(tempBuffer, vectorCount));
            potVec += deltaVec;
            
            // Scatter back
            for (int j = 0; j < vectorCount; j++)
            {
                pi[nodes[i + j]] = tempBuffer[j];
            }
        }
        
        // Handle remainder scalar
        for (; i < count; i++)
        {
            pi[nodes[i]] += deltaVec[0];
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdatePotentialsCacheFriendly(
        long* pi, int* thread, int startNode, int endNode, long delta)
    {
        // Process in cache-line sized chunks
        const int ChunkSize = 8;
        int* chunk = stackalloc int[ChunkSize];
        int chunkPos = 0;
        
        for (int u = startNode; u != endNode; u = thread[u])
        {
            chunk[chunkPos++] = u;
            
            if (chunkPos == ChunkSize)
            {
                // Process chunk
                for (int i = 0; i < ChunkSize; i++)
                {
                    pi[chunk[i]] += delta;
                }
                chunkPos = 0;
            }
        }
        
        // Process remaining
        for (int i = 0; i < chunkPos; i++)
        {
            pi[chunk[i]] += delta;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountNodes(int* thread, int start, int end)
    {
        int count = 0;
        for (int u = start; u != end; u = thread[u])
        {
            count++;
            if (count > 1000) break; // Safety limit
        }
        return count;
    }
    
    /// <summary>
    /// Batch update potentials for multiple independent subtrees.
    /// </summary>
    public static void BatchUpdatePotentials(
        long* pi, int* thread, int* lastSucc,
        Span<int> roots, Span<long> deltas)
    {
        if (roots.Length != deltas.Length)
            throw new ArgumentException("Roots and deltas must have same length");
            
        // Process each subtree
        for (int i = 0; i < roots.Length; i++)
        {
            UpdatePotentials(pi, thread, lastSucc, roots[i], deltas[i]);
        }
    }
}

/// <summary>
/// Optimized tree traversal operations.
/// </summary>
internal static unsafe class TreeTraversalOptimized
{
    /// <summary>
    /// Find the join node of two nodes in the tree using optimized traversal.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindJoinNode(int* parent, int* depth, int u, int v)
    {
        // Bring nodes to same depth
        while (depth[u] > depth[v])
        {
            u = parent[u];
        }
        while (depth[v] > depth[u])
        {
            v = parent[v];
        }
        
        // Walk up together until they meet
        while (u != v)
        {
            u = parent[u];
            v = parent[v];
        }
        
        return u;
    }
    
    /// <summary>
    /// Count nodes in a subtree efficiently.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountSubtreeNodes(int* thread, int* lastSucc, int root)
    {
        int count = 0;
        int end = thread[lastSucc[root]];
        
        for (int u = root; u != end; u = thread[u])
        {
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// Collect nodes in a subtree into a buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CollectSubtreeNodes(
        int* thread, int* lastSucc, int root,
        int* buffer, int maxCount)
    {
        int count = 0;
        int end = thread[lastSucc[root]];
        
        for (int u = root; u != end && count < maxCount; u = thread[u])
        {
            buffer[count++] = u;
        }
        
        return count;
    }
}