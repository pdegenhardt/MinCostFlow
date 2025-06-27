namespace MinCostFlow.Core.Lemon.Types;

/// <summary>
/// Represents the status of a minimum cost flow solver after execution.
/// Corresponds to LEMON's ProblemType enum.
/// </summary>
public enum SolverStatus
{
    /// <summary>
    /// The problem has not been solved yet.
    /// </summary>
    NotSolved = 0,

    /// <summary>
    /// The problem has optimal solution (i.e. it is feasible and bounded).
    /// The algorithm has found optimal flow and node potentials.
    /// </summary>
    Optimal = 1,

    /// <summary>
    /// The problem has no feasible solution (flow).
    /// </summary>
    Infeasible = 2,

    /// <summary>
    /// The objective function of the problem is unbounded.
    /// There is a directed cycle having negative total cost and infinite upper bound.
    /// </summary>
    Unbounded = 3,
    
    /// <summary>
    /// The problem is unbalanced (sum of supplies != sum of demands).
    /// </summary>
    Unbalanced = 4,
    
}