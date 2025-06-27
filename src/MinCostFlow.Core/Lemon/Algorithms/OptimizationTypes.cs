using System;

namespace MinCostFlow.Core.Lemon.Algorithms;

/// <summary>
/// Optimization flags for NetworkSimplex solver.
/// </summary>
[Flags]
public enum OptimizationFlags
{
    None = 0,
    AdaptiveBlockSize = 1 << 0,
    SmallBlocksForDense = 1 << 1,
    ReducedCostCaching = 1 << 2,
    CandidateListPivot = 1 << 3,
    HotColdSplitting = 1 << 4,
    EarlyTermination = 1 << 5,
    All = AdaptiveBlockSize | SmallBlocksForDense | ReducedCostCaching | 
          CandidateListPivot | HotColdSplitting | EarlyTermination
}

/// <summary>
/// Configuration for optimizations.
/// </summary>
public class OptimizationConfig
{
    public OptimizationFlags Flags { get; set; } = OptimizationFlags.None;
    public int MaxBlockSize { get; set; } = 100;
    public int MinBlockSize { get; set; } = 25;  // Increased from 10
    public int DenseNetworkThreshold { get; set; } = 10000; // arcs
    public double CandidateListRatio { get; set; } = 0.1;
    public double BlockSizeGrowthFactor { get; set; } = 1.2;  // Gentler from 1.5
    public double BlockSizeShrinkFactor { get; set; } = 0.8;  // Gentler from 0.5
    public double LowHitRateThreshold { get; set; } = 0.05;  // More aggressive from 0.1
    public double HighHitRateThreshold { get; set; } = 0.3;   // Lower from 0.5
    public int ConsecutiveHitsBeforeAdapt { get; set; } = 3;  // New parameter
    public double MinBlockSizeRatio { get; set; } = 0.125;    // Min block = sqrt(m) * ratio
}

/// <summary>
/// Metrics collected during solver execution for performance analysis.
/// </summary>
public class SolverMetrics
{
    public long PivotSearchTimeMs { get; set; }
    public long TreeUpdateTimeMs { get; set; }
    public long PotentialUpdateTimeMs { get; set; }
    public long TotalSolveTimeMs { get; set; }
    public double PivotSearchTimeMicros { get; set; }
    public double TreeUpdateTimeMicros { get; set; }
    public double PotentialUpdateTimeMicros { get; set; }
    public double TotalSolveTimeMicros { get; set; }
    public int Iterations { get; set; }
    public int InitialBlockSize { get; set; }
    public int FinalBlockSize { get; set; }
    public double AverageArcsCheckedPerPivot { get; set; }
    public long TotalArcsChecked { get; set; }
    public int BaselineIterations { get; set; }
    public double IterationRatio { get; set; }
    
    public override string ToString()
    {
        return $"Iterations: {Iterations}, " +
               $"PivotSearch: {PivotSearchTimeMs}ms ({PivotSearchTimeMs * 100.0 / TotalSolveTimeMs:F1}%), " +
               $"TreeUpdate: {TreeUpdateTimeMs}ms ({TreeUpdateTimeMs * 100.0 / TotalSolveTimeMs:F1}%), " +
               $"PotentialUpdate: {PotentialUpdateTimeMs}ms ({PotentialUpdateTimeMs * 100.0 / TotalSolveTimeMs:F1}%), " +
               $"BlockSize: {InitialBlockSize}->{FinalBlockSize}, " +
               $"AvgArcsChecked: {AverageArcsCheckedPerPivot:F0}";
    }
}