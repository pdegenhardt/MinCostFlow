namespace MinCostFlow.Core.Types;

/// <summary>
/// Constants for selecting the pivot rule in Network Simplex.
/// Corresponds to LEMON's PivotRule enum.
/// </summary>
public enum PivotRule
{
    /// <summary>
    /// The First Eligible pivot rule.
    /// The next eligible arc is selected in a wraparound fashion.
    /// </summary>
    FirstEligible = 0,

    /// <summary>
    /// The Best Eligible pivot rule.
    /// The best eligible arc is selected in every iteration.
    /// </summary>
    BestEligible = 1,

    /// <summary>
    /// The Block Search pivot rule (default).
    /// A specified number of arcs (√m) are examined in every iteration
    /// and the best eligible arc is selected from this block.
    /// </summary>
    BlockSearch = 2,

    /// <summary>
    /// The Candidate List pivot rule.
    /// In a major iteration a candidate list is built from eligible arcs
    /// and in the following minor iterations the best eligible arc is selected from this list.
    /// </summary>
    CandidateList = 3,

    /// <summary>
    /// The Altering Candidate List pivot rule.
    /// Modified version of the Candidate List method that keeps only
    /// a few of the best eligible arcs and extends the list in every iteration.
    /// </summary>
    AlteringList = 4
}