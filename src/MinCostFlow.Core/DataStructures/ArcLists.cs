using System;
using System.Runtime.CompilerServices;

namespace MinCostFlow.Core.DataStructures;

/// <summary>
/// Efficient arc list management for Network Simplex algorithm.
/// Provides O(1) arc state queries and updates.
/// </summary>
public struct ArcLists : IEquatable<ArcLists>
{
    // Arc properties (Structure of Arrays)
    /// <summary>
    /// Gets or sets the array of source node IDs for each arc.
    /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
    public int[] Source { get; set; }
    /// <summary>
    /// Gets or sets the array of target node IDs for each arc.
    /// </summary>
    public int[] Target { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    
    private readonly int _arcCount;
    
    /// <summary>
    /// Initializes arc lists with the specified capacity.
    /// </summary>
    public ArcLists(int arcCount)
    {
        _arcCount = arcCount;
        Source = new int[arcCount];
        Target = new int[arcCount];
    }
    
    /// <summary>
    /// Sets the source and target for an arc.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetArc(int arcId, int source, int target)
    {
        Source[arcId] = source;
        Target[arcId] = target;
    }
    
    /// <summary>
    /// Gets the opposite node of an arc given one endpoint.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOpposite(int arcId, int node)
    {
        return Source[arcId] == node ? Target[arcId] : Source[arcId];
    }
    
    /// <summary>
    /// Checks if the arc goes from u to v in the forward direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsForward(int arcId, int u, int v)
    {
        return Source[arcId] == u && Target[arcId] == v;
    }
    
    /// <summary>
    /// Determines whether this ArcLists equals another ArcLists.
    /// </summary>
    public bool Equals(ArcLists other)
    {
        return _arcCount == other._arcCount &&
               Source == other.Source &&
               Target == other.Target;
    }
    
    /// <summary>
    /// Determines whether this ArcLists equals another object.
    /// </summary>
    public override bool Equals(object? obj) => obj is ArcLists other && Equals(other);
    
    /// <summary>
    /// Returns the hash code for this ArcLists.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(_arcCount, Source, Target);
    
    /// <summary>
    /// Determines whether two ArcLists are equal.
    /// </summary>
    public static bool operator ==(ArcLists left, ArcLists right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two ArcLists are not equal.
    /// </summary>
    public static bool operator !=(ArcLists left, ArcLists right) => !left.Equals(right);
}