namespace MinCostFlow.Problems.Models;

/// <summary>
/// Represents a minimum cost flow problem read from DIMACS format.
/// </summary>
public class DimacsMinCostFlowProblem : MinCostFlowProblem
{
    /// <summary>
    /// Creates a new instance of DimacsMinCostFlowProblem.
    /// </summary>
    public DimacsMinCostFlowProblem()
    {
        Metadata = new ProblemMetadata
        {
            Source = "DIMACS"
        };
    }
}