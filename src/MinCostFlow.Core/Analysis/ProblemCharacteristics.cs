namespace MinCostFlow.Core.Analysis;

/// <summary>
/// Represents characteristics of a minimum cost flow problem instance.
/// Used for automatic optimization configuration selection.
/// </summary>
public class ProblemCharacteristics
{
    /// <summary>
    /// Number of nodes in the network.
    /// </summary>
    public int NodeCount { get; set; }

    /// <summary>
    /// Number of arcs in the network.
    /// </summary>
    public int ArcCount { get; set; }

    /// <summary>
    /// Network density: arcs / (nodes * (nodes - 1))
    /// Range: [0, 1] where 0 is empty and 1 is fully connected.
    /// </summary>
    public double Density { get; set; }

    /// <summary>
    /// Average degree of nodes (average number of incident arcs).
    /// </summary>
    public double AverageDegree { get; set; }

    /// <summary>
    /// Maximum degree among all nodes.
    /// </summary>
    public int MaxDegree { get; set; }

    /// <summary>
    /// Variance of node degrees.
    /// High variance indicates uneven connectivity.
    /// </summary>
    public double DegreeVariance { get; set; }

    /// <summary>
    /// Coefficient of variation for node degrees (stddev / mean).
    /// Normalized measure of degree distribution spread.
    /// </summary>
    public double DegreeCV { get; set; }

    /// <summary>
    /// Number of source nodes (positive supply).
    /// </summary>
    public int SourceCount { get; set; }

    /// <summary>
    /// Number of sink nodes (negative supply).
    /// </summary>
    public int SinkCount { get; set; }

    /// <summary>
    /// Number of transshipment nodes (zero supply).
    /// </summary>
    public int TransshipmentCount { get; set; }

    /// <summary>
    /// Range of arc costs (max - min).
    /// </summary>
    public long CostRange { get; set; }

    /// <summary>
    /// Variance of arc costs.
    /// High variance suggests heterogeneous costs.
    /// </summary>
    public double CostVariance { get; set; }

    /// <summary>
    /// Average arc cost.
    /// </summary>
    public double AverageCost { get; set; }

    /// <summary>
    /// Coefficient of variation for arc costs.
    /// </summary>
    public double CostCV { get; set; }

    /// <summary>
    /// Range of arc capacities (max - min).
    /// </summary>
    public long CapacityRange { get; set; }

    /// <summary>
    /// Average arc capacity.
    /// </summary>
    public double AverageCapacity { get; set; }

    /// <summary>
    /// Total supply in the network.
    /// </summary>
    public long TotalSupply { get; set; }

    /// <summary>
    /// Maximum absolute supply value among all nodes.
    /// </summary>
    public long MaxAbsoluteSupply { get; set; }

    /// <summary>
    /// Detected problem type based on characteristics.
    /// </summary>
    public ProblemType DetectedType { get; set; }

    /// <summary>
    /// Whether the network appears to be layered/time-expanded.
    /// </summary>
    public bool IsLayered { get; set; }

    /// <summary>
    /// Whether the network has uniform costs.
    /// </summary>
    public bool HasUniformCosts { get; set; }

    /// <summary>
    /// Whether the network has uniform capacities.
    /// </summary>
    public bool HasUniformCapacities { get; set; }

    /// <summary>
    /// Ratio of arcs with finite capacity to total arcs.
    /// </summary>
    public double FiniteCapacityRatio { get; set; }

    /// <summary>
    /// Whether the problem is considered dense (density greater than threshold or arc count greater than threshold).
    /// </summary>
    public bool IsDense { get; set; }

    /// <summary>
    /// Whether the problem is considered sparse (density less than threshold).
    /// </summary>
    public bool IsSparse { get; set; }

    public override string ToString()
    {
        return $"Network: {NodeCount} nodes, {ArcCount} arcs, density={Density:F4}\n" +
               $"Degrees: avg={AverageDegree:F1}, max={MaxDegree}, CV={DegreeCV:F2}\n" +
               $"Supply: {SourceCount} sources, {SinkCount} sinks, {TransshipmentCount} transshipment\n" +
               $"Costs: range={CostRange}, CV={CostCV:F2}\n" +
               $"Type: {DetectedType}, Dense={IsDense}, Sparse={IsSparse}, Layered={IsLayered}";
    }
}

/// <summary>
/// Types of minimum cost flow problems.
/// </summary>
public enum ProblemType
{
    /// <summary>
    /// General minimum cost flow problem.
    /// </summary>
    General,

    /// <summary>
    /// Pure circulation problem (all supplies are zero).
    /// </summary>
    Circulation,

    /// <summary>
    /// Assignment problem (bipartite with unit supplies).
    /// </summary>
    Assignment,

    /// <summary>
    /// Transportation problem (bipartite, sources to sinks).
    /// </summary>
    Transportation,

    /// <summary>
    /// Transshipment problem (with intermediate nodes).
    /// </summary>
    Transshipment,

    /// <summary>
    /// Time-expanded network (layered structure).
    /// </summary>
    TimeExpanded
}