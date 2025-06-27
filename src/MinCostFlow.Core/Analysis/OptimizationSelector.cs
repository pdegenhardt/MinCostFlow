using System;
using MinCostFlow.Core.Lemon.Algorithms;

namespace MinCostFlow.Core.Analysis;

/// <summary>
/// Selects optimal optimization configuration based on problem characteristics.
/// </summary>
public static class OptimizationSelector
{
    /// <summary>
    /// Selects the best optimization configuration for the given problem characteristics.
    /// </summary>
    public static OptimizationConfig SelectConfiguration(ProblemCharacteristics characteristics)
    {
        var config = new OptimizationConfig();
        var flags = OptimizationFlags.None;

        // Dense networks benefit from smaller block sizes
        if (characteristics.IsDense)
        {
            flags |= OptimizationFlags.SmallBlocksForDense;
            config.MinBlockSize = 10;
            config.MaxBlockSize = 50;
            config.DenseNetworkThreshold = 5000; // Lower threshold for dense networks
        }
        else
        {
            // Default block sizes for non-dense networks
            config.MinBlockSize = 25;
            config.MaxBlockSize = 100;
        }

        // Networks with high degree variance benefit from adaptive block sizing
        if (characteristics.DegreeCV > 0.5)
        {
            flags |= OptimizationFlags.AdaptiveBlockSize;
            config.BlockSizeGrowthFactor = 1.3; // More aggressive adaptation
            config.BlockSizeShrinkFactor = 0.7;
            config.ConsecutiveHitsBeforeAdapt = 2; // Faster adaptation
        }
        else if (characteristics.DegreeCV > 0.3)
        {
            flags |= OptimizationFlags.AdaptiveBlockSize;
            // Use default adaptation parameters
        }

        // Very sparse networks can benefit from reduced cost caching
        if (characteristics.IsSparse && characteristics.ArcCount < 50000)
        {
            flags |= OptimizationFlags.ReducedCostCaching;
        }

        // Candidate list pivot for certain problem types
        if (ShouldUseCandidateList(characteristics))
        {
            flags |= OptimizationFlags.CandidateListPivot;
            config.CandidateListRatio = CalculateCandidateListRatio(characteristics);
        }

        // Hot-cold splitting for large networks with skewed degree distribution
        if (characteristics.NodeCount > 5000 && characteristics.DegreeCV > 1.0)
        {
            flags |= OptimizationFlags.HotColdSplitting;
        }

        // Early termination for certain problem types
        if (characteristics.DetectedType == ProblemType.Assignment ||
            characteristics.DetectedType == ProblemType.Transportation)
        {
            flags |= OptimizationFlags.EarlyTermination;
        }

        // Adjust thresholds based on problem size
        config.LowHitRateThreshold = characteristics.ArcCount > 10000 ? 0.03 : 0.05;
        config.HighHitRateThreshold = characteristics.ArcCount > 10000 ? 0.25 : 0.3;

        // Set minimum block size ratio based on network size
        if (characteristics.ArcCount > 100000)
        {
            config.MinBlockSizeRatio = 0.0625; // Smaller minimum for very large networks
        }
        else if (characteristics.ArcCount > 10000)
        {
            config.MinBlockSizeRatio = 0.125;
        }
        else
        {
            config.MinBlockSizeRatio = 0.25; // Larger minimum for small networks
        }

        config.Flags = flags;

        LogConfigurationChoice(characteristics, config);

        return config;
    }

    private static bool ShouldUseCandidateList(ProblemCharacteristics characteristics)
    {
        // Use candidate list for:
        // 1. Medium to large sparse networks
        // 2. Networks with uniform costs (where many arcs have similar reduced costs)
        // 3. Assignment and transportation problems
        
        if (characteristics.ArcCount < 1000)
        {
            return false; // Too small to benefit
        }

        return (characteristics.IsSparse && characteristics.ArcCount > 5000) ||
               characteristics.HasUniformCosts ||
               characteristics.DetectedType == ProblemType.Assignment ||
               characteristics.DetectedType == ProblemType.Transportation;
    }

    private static double CalculateCandidateListRatio(ProblemCharacteristics characteristics)
    {
        // Larger candidate lists for uniform cost problems
        if (characteristics.HasUniformCosts)
        {
            return 0.2;
        }

        // Smaller lists for very large networks
        if (characteristics.ArcCount > 100000)
        {
            return 0.05;
        }

        // Default
        return 0.1;
    }

    private static void LogConfigurationChoice(ProblemCharacteristics characteristics, OptimizationConfig config)
    {
        // Only log if verbose mode is enabled
        if (Environment.GetEnvironmentVariable("MCF_VERBOSE") != "1")
        {
            return;
        }

        var selectedFlags = config.Flags.ToString();
        Console.WriteLine($"Selected optimization configuration for {characteristics.DetectedType} problem:");
        Console.WriteLine($"  Network: {characteristics.NodeCount} nodes, {characteristics.ArcCount} arcs");
        Console.WriteLine($"  Density: {characteristics.Density:F4} (Dense={characteristics.IsDense}, Sparse={characteristics.IsSparse})");
        Console.WriteLine($"  Degree CV: {characteristics.DegreeCV:F2}");
        Console.WriteLine($"  Flags: {selectedFlags}");
        Console.WriteLine($"  Block size: {config.MinBlockSize}-{config.MaxBlockSize}");
    }

    /// <summary>
    /// Creates a configuration optimized for the specific problem type.
    /// </summary>
    public static OptimizationConfig CreateTypeSpecificConfiguration(ProblemType problemType)
    {
        var config = new OptimizationConfig();

        switch (problemType)
        {
            case ProblemType.Assignment:
                config.Flags = OptimizationFlags.CandidateListPivot | 
                              OptimizationFlags.EarlyTermination;
                config.MinBlockSize = 50;
                config.MaxBlockSize = 200;
                config.CandidateListRatio = 0.15;
                break;

            case ProblemType.Transportation:
                config.Flags = OptimizationFlags.AdaptiveBlockSize | 
                              OptimizationFlags.EarlyTermination;
                config.MinBlockSize = 25;
                config.MaxBlockSize = 100;
                break;

            case ProblemType.Circulation:
                config.Flags = OptimizationFlags.ReducedCostCaching |
                              OptimizationFlags.AdaptiveBlockSize;
                config.MinBlockSize = 20;
                config.MaxBlockSize = 80;
                break;

            case ProblemType.TimeExpanded:
                config.Flags = OptimizationFlags.HotColdSplitting |
                              OptimizationFlags.AdaptiveBlockSize;
                config.MinBlockSize = 30;
                config.MaxBlockSize = 120;
                break;

            case ProblemType.Transshipment:
            case ProblemType.General:
            default:
                config.Flags = OptimizationFlags.AdaptiveBlockSize;
                config.MinBlockSize = 25;
                config.MaxBlockSize = 100;
                break;
        }

        return config;
    }
}