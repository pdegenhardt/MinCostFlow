using System;
using System.Runtime.CompilerServices;

namespace MinCostFlow.Core.Lemon.DataStructures;

/// <summary>
/// Efficient arc list management for Network Simplex algorithm.
/// Provides O(1) arc state queries and updates.
/// </summary>
/// <remarks>
/// Initializes arc lists with the specified capacity.
/// </remarks>
public struct ArcLists(int arcCount) : IEquatable<ArcLists>
{
    // Arc properties (Structure of Arrays)
    /// <summary>
    /// Gets or sets the array of source node IDs for each arc.
    /// </summary>
    public int[] Source { get; set; } = new int[arcCount];
    /// <summary>
    /// Gets or sets the array of target node IDs for each arc.
    /// </summary>
    public int[] Target { get; set; } = new int[arcCount];

    private readonly int _arcCount = arcCount;

    /// <summary>
    /// Sets the source and target for an arc.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetArc(int arcId, int source, int target)
    {
        Source[arcId] = source;
        Target[arcId] = target;
    }
    
    /// <summary>
    /// Gets the opposite node of an arc given one endpoint.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetOpposite(int arcId, int node)
    {
        return Source[arcId] == node ? Target[arcId] : Source[arcId];
    }
    
    /// <summary>
    /// Checks if the arc goes from u to v in the forward direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsForward(int arcId, int u, int v)
    {
        return Source[arcId] == u && Target[arcId] == v;
    }
    
    /// <summary>
    /// Determines whether this ArcLists equals another ArcLists.
    /// </summary>
    public readonly bool Equals(ArcLists other)
    {
        return _arcCount == other._arcCount &&
               Source == other.Source &&
               Target == other.Target;
    }
    
    /// <summary>
    /// Determines whether this ArcLists equals another object.
    /// </summary>
    public override readonly bool Equals(object? obj) => obj is ArcLists other && Equals(other);
    
    /// <summary>
    /// Returns the hash code for this ArcLists.
    /// </summary>
    public override readonly int GetHashCode() => HashCode.Combine(_arcCount, Source, Target);
    
    /// <summary>
    /// Determines whether two ArcLists are equal.
    /// </summary>
    public static bool operator ==(ArcLists left, ArcLists right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two ArcLists are not equal.
    /// </summary>
    public static bool operator !=(ArcLists left, ArcLists right) => !left.Equals(right);
}