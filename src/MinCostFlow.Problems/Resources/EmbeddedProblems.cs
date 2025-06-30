namespace MinCostFlow.Problems.Resources;

/// <summary>
/// Provides compile-time safe access to embedded problem resources.
/// This class ensures that tests and other code can reference embedded resources
/// without magic strings, and compilation will fail if resources are removed or renamed.
/// </summary>
public static class EmbeddedProblems
{
    private const string ResourcePrefix = "MinCostFlow.Problems.Resources.";
    
    /// <summary>
    /// Path-based network problems.
    /// </summary>
    public static class Path
    {
        public const string Path5Node = ResourcePrefix + "path.path_5node.min";
        public const string Path10Node = ResourcePrefix + "path.path_10node.min";
        public const string DiamondGraph = ResourcePrefix + "path.diamond_graph.min";
        public const string StarGraph = ResourcePrefix + "path.star_graph.min";
    }
    
    /// <summary>
    /// Grid-based network problems.
    /// </summary>
    public static class Grid
    {
        public const string Grid2x2 = ResourcePrefix + "circulation.grid_2x2.min";
        public const string Grid5x5 = ResourcePrefix + "grid.grid_5x5.min";
    }
    
    /// <summary>
    /// Transportation problems.
    /// </summary>
    public static class Transport
    {
        public const string Transport2x3 = ResourcePrefix + "transport.transport_2x3.min";
        public const string Transport40x30 = ResourcePrefix + "transport.transport_40x30.min";
        public const string Transport400x300 = ResourcePrefix + "transport.transport_400x300.min";
    }
    
    /// <summary>
    /// Assignment problems.
    /// </summary>
    public static class Assignment
    {
        public const string Assignment3x3 = ResourcePrefix + "assignment.assignment_3x3.min";
        public const string Assignment5x5 = ResourcePrefix + "assignment.assignment_5x5.min";
        public const string Assignment50x50 = ResourcePrefix + "assignment.assignment_50x50.min";
    }
    
    /// <summary>
    /// Circulation problems.
    /// </summary>
    public static class Circulation
    {
        public const string Circulation100_0_10 = ResourcePrefix + "circulation.circulation_100_0_10.min";
        public const string Circulation1000_0_05 = ResourcePrefix + "circulation.circulation_1000_0_05.min";
        public const string Circulation5000_0_05 = ResourcePrefix + "circulation.circulation_5000_0_05.min";
        public const string CycleShortcut = ResourcePrefix + "circulation.cycle_shortcut.min";
    }
    
    /// <summary>
    /// DIMACS Netgen benchmark problems.
    /// </summary>
    public static class Netgen
    {
        public const string Netgen8_08a = ResourcePrefix + "netgen.netgen_8_08a.min";
        public const string Netgen8_10a = ResourcePrefix + "netgen.netgen_8_10a.min";
        public const string Netgen8_13a = ResourcePrefix + "netgen.netgen_8_13a.min";
        public const string Netgen8_14a = ResourcePrefix + "netgen.netgen_8_14a.min";
        public const string Netgen8_15a = ResourcePrefix + "netgen.netgen_8_15a.min";
    }
    
    /// <summary>
    /// Knapsack-related problems.
    /// </summary>
    public static class Knapsack
    {
        public const string AURV19V6 = ResourcePrefix + "knapzack.AURV19V6.min";
        public const string AllBookingsShouldScheduleIllustration = ResourcePrefix + "knapzack.AllBookingsShouldScheduleIllustration.min";
        public const string CanonicalProblemBIllustration = ResourcePrefix + "knapzack.CanonicalProblemBIllustration.min";
        public const string CanonicalProblemIllustrationWithHighRetirementDelayPenalty = ResourcePrefix + "knapzack.CanonicalProblemIllustrationWithHighRetirementDelayPenalty.min";
        public const string CanonicalProblemIllustrationWithHighRevenueMultiplier = ResourcePrefix + "knapzack.CanonicalProblemIllustrationWithHighRevenueMultiplier.min";
        public const string CanonicalProblemIllustrationWithLowRetirementDelayPenalty = ResourcePrefix + "knapzack.CanonicalProblemIllustrationWithLowRetirementDelayPenalty.min";
        public const string DowntimeCanCauseDelays = ResourcePrefix + "knapzack.DowntimeCanCauseDelays.min";
        public const string DowntimeCanCauseLateRetirement = ResourcePrefix + "knapzack.DowntimeCanCauseLateRetirement.min";
        public const string DowntimeCanCauseRelocation = ResourcePrefix + "knapzack.DowntimeCanCauseRelocation.min";
        public const string DowntimeGetsAcceptedOverRental = ResourcePrefix + "knapzack.DowntimeGetsAcceptedOverRental.min";
        public const string DowntimeGetsRejected = ResourcePrefix + "knapzack.DowntimeGetsRejected.min";
        public const string DowntimeMixedAcceptReject = ResourcePrefix + "knapzack.DowntimeMixedAcceptReject.min";
        public const string RentalsDelaysAreLimited = ResourcePrefix + "knapzack.RentalsDelaysAreLimited.min";
        public const string RentalsGetAcceptedAndRejected = ResourcePrefix + "knapzack.RentalsGetAcceptedAndRejected.min";
        public const string RentalsGetDelayed = ResourcePrefix + "knapzack.RentalsGetDelayed.min";
        public const string SimpleProblemIllustration = ResourcePrefix + "knapzack.SimpleProblemIllustration.min";
        public const string SimpleProblemIllustration2 = ResourcePrefix + "knapzack.SimpleProblemIllustration2.min";
        public const string SimpleProblemIllustration2NonSparse = ResourcePrefix + "knapzack.SimpleProblemIllustration2NonSparse.min";
        public const string TablesAreCorrect = ResourcePrefix + "knapzack.TablesAreCorrect.min";
        public const string TablesAreCorrectSimple = ResourcePrefix + "knapzack.TablesAreCorrectSimple.min";
    }
}