using System;

namespace MinCostFlow.Problems.Models;

/// <summary>
/// Metadata about a minimum cost flow problem.
/// </summary>
public class ProblemMetadata
{
    /// <summary>
    /// Gets or sets the name of the problem.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source of the problem (e.g., "DIMACS", "NETGEN", "Generated").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the problem category (e.g., "Transportation", "Circulation", "TimeExpanded").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional description of the problem.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the expected optimal cost value, if known.
    /// </summary>
    public long? OptimalCost { get; set; }

    /// <summary>
    /// Gets or sets the file path if loaded from a file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the problem was created/loaded.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}