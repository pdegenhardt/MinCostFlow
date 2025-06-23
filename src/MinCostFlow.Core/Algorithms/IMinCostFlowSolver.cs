using MinCostFlow.Core.Types;

namespace MinCostFlow.Core.Algorithms;

/// <summary>
/// Interface for minimum cost flow solvers.
/// </summary>
public interface IMinCostFlowSolver
{
    /// <summary>
    /// Runs the algorithm to solve the minimum cost flow problem.
    /// </summary>
    /// <returns>The status of the solution.</returns>
    SolverStatus Solve();
    
    /// <summary>
    /// Gets the current solver status.
    /// </summary>
    SolverStatus Status { get; }
    
    /// <summary>
    /// Gets the flow value on the specified arc after solving.
    /// </summary>
    long GetFlow(Arc arc);
    
    /// <summary>
    /// Gets the potential (dual variable) of the specified node after solving.
    /// </summary>
    long GetPotential(Node node);
    
    /// <summary>
    /// Gets the total cost of the solution.
    /// </summary>
    long GetTotalCost();
}