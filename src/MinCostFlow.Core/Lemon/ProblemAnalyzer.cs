using System;
using MinCostFlow.Core.Analysis;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Core.Lemon.Types;

namespace MinCostFlow.Core.Lemon;

/// <summary>
/// Analyzes minimum cost flow problem instances to determine their characteristics.
/// </summary>
public static class ProblemAnalyzer
{
    private const double DENSE_THRESHOLD = 0.01;
    private const double SPARSE_THRESHOLD = 0.005;
    private const int DENSE_ARC_THRESHOLD = 10000;
    private const double UNIFORMITY_TOLERANCE = 0.01; // 1% tolerance for uniformity

    /// <summary>
    /// Analyzes a network problem to determine its characteristics.
    /// </summary>
    public static ProblemCharacteristics Analyze(
        IGraph graph,
        long[] lower,
        long[] upper,
        long[] cost,
        long[] supply)
    {
        var characteristics = new ProblemCharacteristics
        {
            NodeCount = graph.NodeCount,
            ArcCount = graph.ArcCount
        };

        // Calculate density
        long maxPossibleArcs = (long)graph.NodeCount * (graph.NodeCount - 1);
        characteristics.Density = maxPossibleArcs > 0 ? (double)graph.ArcCount / maxPossibleArcs : 0;

        // Analyze node degrees
        AnalyzeNodeDegrees(graph, characteristics);

        // Analyze supply distribution
        AnalyzeSupplyDistribution(supply, characteristics);

        // Analyze costs
        AnalyzeCosts(cost, graph.ArcCount, characteristics);

        // Analyze capacities
        AnalyzeCapacities(upper, lower, graph.ArcCount, characteristics);

        // Detect problem type
        characteristics.DetectedType = DetectProblemType(graph, characteristics);

        // Check for layered structure (time-expanded networks)
        characteristics.IsLayered = CheckForLayeredStructure(graph, characteristics);

        // Determine density classification
        characteristics.IsDense = characteristics.Density > DENSE_THRESHOLD || 
                                 graph.ArcCount > DENSE_ARC_THRESHOLD;
        characteristics.IsSparse = characteristics.Density < SPARSE_THRESHOLD;

        return characteristics;
    }

    private static void AnalyzeNodeDegrees(IGraph graph, ProblemCharacteristics characteristics)
    {
        var degrees = new int[graph.NodeCount];
        int totalDegree = 0;
        int maxDegree = 0;

        for (int i = 0; i < graph.NodeCount; i++)
        {
            var node = new Node(i);
            int outDegree = graph.GetOutArcs(node).Length;
            int inDegree = graph.GetInArcs(node).Length;
            int degree = outDegree + inDegree;
            
            degrees[i] = degree;
            totalDegree += degree;
            maxDegree = Math.Max(maxDegree, degree);
        }

        double avgDegree = graph.NodeCount > 0 ? (double)totalDegree / graph.NodeCount : 0;
        
        // Calculate variance
        double variance = 0;
        if (graph.NodeCount > 0)
        {
            foreach (var degree in degrees)
            {
                double diff = degree - avgDegree;
                variance += diff * diff;
            }
            variance /= graph.NodeCount;
        }

        characteristics.AverageDegree = avgDegree;
        characteristics.MaxDegree = maxDegree;
        characteristics.DegreeVariance = variance;
        characteristics.DegreeCV = avgDegree > 0 ? Math.Sqrt(variance) / avgDegree : 0;
    }

    private static void AnalyzeSupplyDistribution(long[] supply, ProblemCharacteristics characteristics)
    {
        int sourceCount = 0;
        int sinkCount = 0;
        int transshipmentCount = 0;
        long totalSupply = 0;
        long maxAbsSupply = 0;

        foreach (var s in supply)
        {
            if (s > 0)
            {
                sourceCount++;
                totalSupply += s;
            }
            else if (s < 0)
            {
                sinkCount++;
            }
            else
            {
                transshipmentCount++;
            }
            
            maxAbsSupply = Math.Max(maxAbsSupply, Math.Abs(s));
        }

        characteristics.SourceCount = sourceCount;
        characteristics.SinkCount = sinkCount;
        characteristics.TransshipmentCount = transshipmentCount;
        characteristics.TotalSupply = totalSupply;
        characteristics.MaxAbsoluteSupply = maxAbsSupply;
    }

    private static void AnalyzeCosts(long[] cost, int arcCount, ProblemCharacteristics characteristics)
    {
        if (arcCount == 0)
        {
            characteristics.CostRange = 0;
            characteristics.AverageCost = 0;
            characteristics.CostVariance = 0;
            characteristics.CostCV = 0;
            characteristics.HasUniformCosts = true;
            return;
        }

        long minCost = long.MaxValue;
        long maxCost = long.MinValue;
        long totalCost = 0;

        for (int i = 0; i < arcCount; i++)
        {
            minCost = Math.Min(minCost, cost[i]);
            maxCost = Math.Max(maxCost, cost[i]);
            totalCost += cost[i];
        }

        double avgCost = (double)totalCost / arcCount;
        
        // Calculate variance
        double variance = 0;
        for (int i = 0; i < arcCount; i++)
        {
            double diff = cost[i] - avgCost;
            variance += diff * diff;
        }
        variance /= arcCount;

        characteristics.CostRange = maxCost - minCost;
        characteristics.AverageCost = avgCost;
        characteristics.CostVariance = variance;
        characteristics.CostCV = Math.Abs(avgCost) > 0 ? Math.Sqrt(variance) / Math.Abs(avgCost) : 0;
        
        // Check uniformity
        characteristics.HasUniformCosts = characteristics.CostCV < UNIFORMITY_TOLERANCE;
    }

    private static void AnalyzeCapacities(long[] upper, long[] lower, int arcCount, ProblemCharacteristics characteristics)
    {
        if (arcCount == 0)
        {
            characteristics.CapacityRange = 0;
            characteristics.AverageCapacity = 0;
            characteristics.HasUniformCapacities = true;
            characteristics.FiniteCapacityRatio = 0;
            return;
        }

        long minCapacity = long.MaxValue;
        long maxCapacity = long.MinValue;
        long totalCapacity = 0;
        int finiteCapacityCount = 0;
        const long INF_THRESHOLD = long.MaxValue / 2; // Consider very large values as infinite

        for (int i = 0; i < arcCount; i++)
        {
            long capacity = upper[i] - lower[i];
            if (capacity < INF_THRESHOLD)
            {
                minCapacity = Math.Min(minCapacity, capacity);
                maxCapacity = Math.Max(maxCapacity, capacity);
                totalCapacity += capacity;
                finiteCapacityCount++;
            }
        }

        if (finiteCapacityCount > 0)
        {
            characteristics.CapacityRange = maxCapacity - minCapacity;
            characteristics.AverageCapacity = (double)totalCapacity / finiteCapacityCount;
            characteristics.HasUniformCapacities = characteristics.CapacityRange == 0;
        }
        else
        {
            characteristics.CapacityRange = 0;
            characteristics.AverageCapacity = 0;
            characteristics.HasUniformCapacities = true;
        }

        characteristics.FiniteCapacityRatio = (double)finiteCapacityCount / arcCount;
    }

    private static ProblemType DetectProblemType(IGraph graph, ProblemCharacteristics characteristics)
    {
        // Pure circulation: all supplies are zero
        if (characteristics.SourceCount == 0 && characteristics.SinkCount == 0)
        {
            return ProblemType.Circulation;
        }

        // Check for bipartite structure
        bool isBipartite = CheckBipartite(graph, characteristics);

        if (isBipartite)
        {
            // Assignment: bipartite with unit supplies
            if (characteristics.MaxAbsoluteSupply == 1 &&
                characteristics.SourceCount == characteristics.SinkCount)
            {
                return ProblemType.Assignment;
            }

            // Transportation: bipartite from sources to sinks
            if (characteristics.TransshipmentCount == 0)
            {
                return ProblemType.Transportation;
            }
        }

        // Transshipment: has intermediate nodes
        if (characteristics.TransshipmentCount > 0)
        {
            return ProblemType.Transshipment;
        }

        return ProblemType.General;
    }

    private static bool CheckBipartite(IGraph graph, ProblemCharacteristics characteristics)
    {
        // Simple heuristic: check if nodes can be partitioned into two sets
        // where all arcs go from one set to the other
        
        // For efficiency, we'll use a simple check:
        // If roughly half the nodes have only outgoing arcs and
        // the other half have only incoming arcs, it's likely bipartite
        
        int onlyOutgoing = 0;
        int onlyIncoming = 0;
        
        for (int i = 0; i < graph.NodeCount; i++)
        {
            var node = new Node(i);
            bool hasOutgoing = graph.GetOutArcs(node).Length > 0;
            bool hasIncoming = graph.GetInArcs(node).Length > 0;
            
            if (hasOutgoing && !hasIncoming)
            {
                onlyOutgoing++;
            }
            else if (!hasOutgoing && hasIncoming)
            {
                onlyIncoming++;
            }
        }
        
        // If most nodes are purely sources or sinks, likely bipartite
        double bipartiteRatio = (double)(onlyOutgoing + onlyIncoming) / graph.NodeCount;
        return bipartiteRatio > 0.8;
    }

    private static bool CheckForLayeredStructure(IGraph graph, ProblemCharacteristics characteristics)
    {
        // Heuristic for detecting time-expanded networks:
        // 1. Nodes can be partitioned into layers
        // 2. Most arcs go from layer i to layer i+1
        // 3. Regular pattern in node degrees
        
        // Simple check: if degree variance is low and network is sparse
        // with a regular structure, it might be layered
        
        return characteristics.DegreeCV < 0.3 && 
               characteristics.IsSparse &&
               characteristics.DetectedType == ProblemType.Transshipment;
    }
}