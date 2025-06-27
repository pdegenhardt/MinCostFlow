namespace MinCostFlow.Experiment;

/// <summary>
/// Status enum for max flow computation results.
/// </summary>
public enum MaxFlowStatus
{
    NotSolved,    // The problem was not solved, or its data were edited.
    Optimal,      // Solve() was called and found an optimal solution.
    IntOverflow,  // There is a feasible flow > max possible flow.
    BadInput,     // Legacy - no longer used
    BadResult     // Legacy - no longer used
}
