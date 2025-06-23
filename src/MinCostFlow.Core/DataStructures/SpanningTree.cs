using System;
using System.Runtime.CompilerServices;
using MinCostFlow.Core.Types;

namespace MinCostFlow.Core.DataStructures;

/// <summary>
/// Represents a spanning tree structure for Network Simplex algorithm.
/// Based on LEMON's thread index implementation for O(1) tree updates.
/// </summary>
public struct SpanningTree : IEquatable<SpanningTree>
{
    // Tree structure arrays
#pragma warning disable CA1819 // Properties should not return arrays
    /// <summary>
    /// Gets or sets the parent node in tree (-1 for root).
    /// </summary>
    public int[] Parent { get; set; }
    /// <summary>
    /// Gets or sets the predecessor arc in tree (-1 for root).
    /// </summary>
    public int[] Pred { get; set; }
    /// <summary>
    /// Gets or sets the thread index for tree traversal.
    /// </summary>
    public int[] Thread { get; set; }
    /// <summary>
    /// Gets or sets the reverse thread index.
    /// </summary>
    public int[] RevThread { get; set; }
    /// <summary>
    /// Gets or sets the number of successors (subtree size).
    /// </summary>
    public int[] SuccNum { get; set; }
    /// <summary>
    /// Gets or sets the last successor in subtree.
    /// </summary>
    public int[] LastSucc { get; set; }
    /// <summary>
    /// Gets or sets the direction of predecessor arc (1: up, -1: down).
    /// </summary>
    public sbyte[] PredDir { get; set; }
    /// <summary>
    /// Gets or sets the arc state (-1: lower, 0: tree, 1: upper).
    /// </summary>
    public sbyte[] State { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    
    // Constants for arc states (matching LEMON)
#pragma warning disable CA1707 // Identifiers should not contain underscores
    /// <summary>
    /// Arc state constant for upper bound.
    /// </summary>
    public const sbyte STATE_UPPER = -1;
    /// <summary>
    /// Arc state constant for tree arc.
    /// </summary>
    public const sbyte STATE_TREE = 0;
    /// <summary>
    /// Arc state constant for lower bound.
    /// </summary>
    public const sbyte STATE_LOWER = 1;
    
    // Constants for arc directions
    /// <summary>
    /// Direction constant for down.
    /// </summary>
    public const sbyte DIR_DOWN = -1;
    /// <summary>
    /// Direction constant for up.
    /// </summary>
    public const sbyte DIR_UP = 1;
#pragma warning restore CA1707 // Identifiers should not contain underscores
    
    private readonly int _nodeCount;
    private readonly int _arcCount;
    
    /// <summary>
    /// Initializes a new spanning tree structure.
    /// </summary>
    public SpanningTree(int nodeCount, int arcCount)
    {
        _nodeCount = nodeCount;
        _arcCount = arcCount;
        
        // Allocate arrays
        Parent = new int[nodeCount];
        Pred = new int[nodeCount];
        Thread = new int[nodeCount];
        RevThread = new int[nodeCount];
        SuccNum = new int[nodeCount];
        LastSucc = new int[nodeCount];
        PredDir = new sbyte[nodeCount];
        State = new sbyte[arcCount];
        
        // Initialize with invalid values
        Array.Fill(Parent, -1);
        Array.Fill(Pred, -1);
    }
    
    /// <summary>
    /// Finds the join node (Lowest Common Ancestor) of two nodes in the tree.
    /// Uses the thread index structure for efficient traversal.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindJoinNode(int u, int v)
    {
        while (u != v)
        {
            if (SuccNum[u] < SuccNum[v])
            {
                u = Parent[u];
            }
            else
            {
                v = Parent[v];
            }
        }
        return u;
    }
    
    /// <summary>
    /// Checks if node v is in the subtree rooted at node u.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInSubtree(int u, int v)
    {
        return Thread[u] <= Thread[v] && Thread[v] <= Thread[LastSucc[u]];
    }
    
    
    
    
    /// <summary>
    /// Initializes the spanning tree as a star rooted at the given node.
    /// Used for initial basic feasible solution.
    /// </summary>
    public void InitializeAsStart(int root)
    {
        // Set root
        Parent[root] = -1;
        Pred[root] = -1;
        Thread[root] = 0;
        RevThread[0] = root;
        SuccNum[root] = _nodeCount;
        LastSucc[root] = _nodeCount - 1;
        PredDir[root] = 0;
        
        // Initialize other nodes
        int thread = 1;
        for (int u = 0; u < _nodeCount; u++)
        {
            if (u != root)
            {
                Parent[u] = root;
                Thread[u] = thread;
                RevThread[thread] = u;
                SuccNum[u] = 1;
                LastSucc[u] = u;
                thread++;
            }
        }
    }
    
    /// <summary>
    /// Determines whether this SpanningTree equals another SpanningTree.
    /// </summary>
    public bool Equals(SpanningTree other)
    {
        return _nodeCount == other._nodeCount &&
               _arcCount == other._arcCount &&
               Parent == other.Parent &&
               Pred == other.Pred &&
               Thread == other.Thread &&
               RevThread == other.RevThread &&
               SuccNum == other.SuccNum &&
               LastSucc == other.LastSucc &&
               PredDir == other.PredDir &&
               State == other.State;
    }
    
    /// <summary>
    /// Determines whether this SpanningTree equals another object.
    /// </summary>
    public override bool Equals(object? obj) => obj is SpanningTree other && Equals(other);
    
    /// <summary>
    /// Returns the hash code for this SpanningTree.
    /// </summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_nodeCount);
        hash.Add(_arcCount);
        hash.Add(Parent);
        hash.Add(Pred);
        hash.Add(Thread);
        hash.Add(RevThread);
        hash.Add(SuccNum);
        hash.Add(LastSucc);
        hash.Add(PredDir);
        hash.Add(State);
        return hash.ToHashCode();
    }
    
    /// <summary>
    /// Determines whether two SpanningTrees are equal.
    /// </summary>
    public static bool operator ==(SpanningTree left, SpanningTree right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two SpanningTrees are not equal.
    /// </summary>
    public static bool operator !=(SpanningTree left, SpanningTree right) => !left.Equals(right);
}