namespace MinCostFlow.Core.Types;

/// <summary>
/// Constants for selecting the type of the supply constraints.
/// Corresponds to LEMON's SupplyType enum.
/// </summary>
public enum SupplyType
{
    /// <summary>
    /// There are "greater or equal" supply/demand constraints.
    /// </summary>
    Geq = 0,

    /// <summary>
    /// There are "less or equal" supply/demand constraints.
    /// </summary>
    Leq = 1
}