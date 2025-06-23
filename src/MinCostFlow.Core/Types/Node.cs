using System;

namespace MinCostFlow.Core.Types;

/// <summary>
/// Represents a node in a graph, following LEMON's node structure.
/// Uses integer ID for efficient indexing and memory layout.
/// </summary>
public readonly struct Node : IEquatable<Node>, IComparable<Node>
{
    /// <summary>
    /// Gets the internal ID of the node. -1 represents an invalid node.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Creates a new node with the specified ID.
    /// </summary>
    public Node(int id) => Id = id;

    /// <summary>
    /// Represents an invalid node (similar to LEMON's INVALID).
    /// </summary>
    public static readonly Node Invalid = new(-1);

    /// <summary>
    /// Checks if this node is valid.
    /// </summary>
    public bool IsValid => Id >= 0;

    /// <summary>
    /// Determines whether this node equals another node.
    /// </summary>
    public bool Equals(Node other) => Id == other.Id;
    /// <summary>
    /// Determines whether this node equals another object.
    /// </summary>
    public override bool Equals(object? obj) => obj is Node other && Equals(other);
    /// <summary>
    /// Returns the hash code for this node.
    /// </summary>
    public override int GetHashCode() => Id;
    /// <summary>
    /// Compares this node to another node.
    /// </summary>
    public int CompareTo(Node other) => Id.CompareTo(other.Id);

    /// <summary>
    /// Determines whether two nodes are equal.
    /// </summary>
    public static bool operator ==(Node left, Node right) => left.Id == right.Id;
    /// <summary>
    /// Determines whether two nodes are not equal.
    /// </summary>
    public static bool operator !=(Node left, Node right) => left.Id != right.Id;
    /// <summary>
    /// Determines whether one node is less than another.
    /// </summary>
    public static bool operator <(Node left, Node right) => left.Id < right.Id;
    /// <summary>
    /// Determines whether one node is greater than another.
    /// </summary>
    public static bool operator >(Node left, Node right) => left.Id > right.Id;
    /// <summary>
    /// Determines whether one node is less than or equal to another.
    /// </summary>
    public static bool operator <=(Node left, Node right) => left.Id <= right.Id;
    /// <summary>
    /// Determines whether one node is greater than or equal to another.
    /// </summary>
    public static bool operator >=(Node left, Node right) => left.Id >= right.Id;

    /// <summary>
    /// Returns a string representation of this node.
    /// </summary>
    public override string ToString() => IsValid ? $"Node({Id})" : "Node(Invalid)";
}