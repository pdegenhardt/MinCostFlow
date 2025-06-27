using System;

namespace MinCostFlow.Core.Lemon.Types;

/// <summary>
/// Represents an arc (directed edge) in a graph, following LEMON's arc structure.
/// Uses integer ID for efficient indexing and memory layout.
/// </summary>
/// <remarks>
/// Creates a new arc with the specified ID.
/// </remarks>
public readonly struct Arc(int id) : IEquatable<Arc>, IComparable<Arc>
{
    /// <summary>
    /// Gets the internal ID of the arc. -1 represents an invalid arc.
    /// </summary>
    public int Id { get; } = id;

    /// <summary>
    /// Represents an invalid arc (similar to LEMON's INVALID).
    /// </summary>
    public static readonly Arc Invalid = new(-1);

    /// <summary>
    /// Checks if this arc is valid.
    /// </summary>
    public bool IsValid => Id >= 0;

    /// <summary>
    /// Determines whether this arc equals another arc.
    /// </summary>
    public bool Equals(Arc other) => Id == other.Id;
    /// <summary>
    /// Determines whether this arc equals another object.
    /// </summary>
    public override bool Equals(object? obj) => obj is Arc other && Equals(other);
    /// <summary>
    /// Returns the hash code for this arc.
    /// </summary>
    public override int GetHashCode() => Id;
    /// <summary>
    /// Compares this arc to another arc.
    /// </summary>
    public int CompareTo(Arc other) => Id.CompareTo(other.Id);

    /// <summary>
    /// Determines whether two arcs are equal.
    /// </summary>
    public static bool operator ==(Arc left, Arc right) => left.Id == right.Id;
    /// <summary>
    /// Determines whether two arcs are not equal.
    /// </summary>
    public static bool operator !=(Arc left, Arc right) => left.Id != right.Id;
    /// <summary>
    /// Determines whether one arc is less than another.
    /// </summary>
    public static bool operator <(Arc left, Arc right) => left.Id < right.Id;
    /// <summary>
    /// Determines whether one arc is greater than another.
    /// </summary>
    public static bool operator >(Arc left, Arc right) => left.Id > right.Id;
    /// <summary>
    /// Determines whether one arc is less than or equal to another.
    /// </summary>
    public static bool operator <=(Arc left, Arc right) => left.Id <= right.Id;
    /// <summary>
    /// Determines whether one arc is greater than or equal to another.
    /// </summary>
    public static bool operator >=(Arc left, Arc right) => left.Id >= right.Id;

    /// <summary>
    /// Returns a string representation of this arc.
    /// </summary>
    public override string ToString() => IsValid ? $"Arc({Id})" : "Arc(Invalid)";
}