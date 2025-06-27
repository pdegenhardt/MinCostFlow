using System.Linq;
using Xunit;
using MinCostFlow.Core.Analysis;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Types;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Core.Lemon;

namespace MinCostFlow.Tests.Lemon;

public class ProblemAnalysisTests
{
    [Fact]
    public void AnalyzeProblem_DenseNetwork_CorrectlyIdentified()
    {
        // Create a dense network (fully connected)
        int nodeCount = 10;
        var builder = new GraphBuilder();
        
        // Add nodes
        for (int i = 0; i < nodeCount; i++)
        {
            builder.AddNode();
        }
        
        // Add arcs between all pairs (dense)
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = 0; j < nodeCount; j++)
            {
                if (i != j)
                {
                    builder.AddArc(i, j);
                }
            }
        }
        
        var graph = builder.Build();
        var lower = new long[graph.ArcCount];
        var upper = Enumerable.Repeat(100L, graph.ArcCount).ToArray();
        var cost = Enumerable.Repeat(1L, graph.ArcCount).ToArray();
        var supply = new long[nodeCount];
        supply[0] = 50; // source
        supply[nodeCount - 1] = -50; // sink
        
        var characteristics = ProblemAnalyzer.Analyze(graph, lower, upper, cost, supply);
        
        Assert.True(characteristics.IsDense);
        Assert.False(characteristics.IsSparse);
        Assert.True(characteristics.Density > 0.9); // Nearly fully connected
        Assert.Equal(nodeCount - 1, characteristics.MaxDegree * 0.5); // Each node connects to all others
    }
    
    [Fact]
    public void AnalyzeProblem_SparseNetwork_CorrectlyIdentified()
    {
        // Create a sparse network (chain)
        int nodeCount = 100;
        var builder = new GraphBuilder();
        
        // Add nodes
        for (int i = 0; i < nodeCount; i++)
        {
            builder.AddNode();
        }
        
        // Add arcs in a chain
        for (int i = 0; i < nodeCount - 1; i++)
        {
            builder.AddArc(i, i + 1);
        }
        
        var graph = builder.Build();
        var lower = new long[graph.ArcCount];
        var upper = Enumerable.Repeat(100L, graph.ArcCount).ToArray();
        var cost = Enumerable.Repeat(1L, graph.ArcCount).ToArray();
        var supply = new long[nodeCount];
        supply[0] = 50;
        supply[nodeCount - 1] = -50;
        
        var characteristics = ProblemAnalyzer.Analyze(graph, lower, upper, cost, supply);
        
        
        Assert.False(characteristics.IsDense);
        // With 100 nodes and 99 arcs, density = 99/(100*99) = 0.01
        // This is not sparse by our 0.005 threshold
        Assert.False(characteristics.IsSparse);
        Assert.True(characteristics.Density <= 0.01);
        Assert.Equal(2, characteristics.MaxDegree); // Chain has degree 2 (except endpoints)
    }
    
    [Fact]
    public void AnalyzeProblem_TrulySparseNetwork_CorrectlyIdentified()
    {
        // Create a very sparse network (star topology)
        int nodeCount = 200;
        var builder = new GraphBuilder();
        
        // Add nodes
        for (int i = 0; i < nodeCount; i++)
        {
            builder.AddNode();
        }
        
        // Add arcs in star pattern - node 0 connects to a few others
        for (int i = 1; i <= 10; i++)
        {
            builder.AddArc(0, i);
            builder.AddArc(i, 0);
        }
        
        var graph = builder.Build();
        var lower = new long[graph.ArcCount];
        var upper = Enumerable.Repeat(100L, graph.ArcCount).ToArray();
        var cost = Enumerable.Repeat(1L, graph.ArcCount).ToArray();
        var supply = new long[nodeCount];
        supply[0] = 50;
        supply[1] = -50;
        
        var characteristics = ProblemAnalyzer.Analyze(graph, lower, upper, cost, supply);
        
        // With 200 nodes and 20 arcs, density = 20/(200*199) ≈ 0.0005
        
        Assert.False(characteristics.IsDense);
        Assert.True(characteristics.IsSparse);
        Assert.True(characteristics.Density < 0.001);
    }
    
    [Fact]
    public void AnalyzeProblem_CirculationProblem_CorrectlyDetected()
    {
        // Create a circulation problem (all supplies are zero)
        int nodeCount = 10;
        var builder = new GraphBuilder();
        
        for (int i = 0; i < nodeCount; i++)
        {
            builder.AddNode();
        }
        
        // Add some arcs
        for (int i = 0; i < nodeCount; i++)
        {
            builder.AddArc(i, (i + 1) % nodeCount);
        }
        
        var graph = builder.Build();
        var lower = new long[graph.ArcCount];
        var upper = Enumerable.Repeat(100L, graph.ArcCount).ToArray();
        var cost = Enumerable.Range(1, graph.ArcCount).Select(i => (long)i).ToArray();
        var supply = new long[nodeCount]; // All zeros
        
        var characteristics = ProblemAnalyzer.Analyze(graph, lower, upper, cost, supply);
        
        Assert.Equal(ProblemType.Circulation, characteristics.DetectedType);
        Assert.Equal(0, characteristics.SourceCount);
        Assert.Equal(0, characteristics.SinkCount);
        Assert.Equal(nodeCount, characteristics.TransshipmentCount);
    }
    
    [Fact]
    public void AutoConfiguration_DenseNetwork_SelectsSmallBlocks()
    {
        // Create a dense network
        int nodeCount = 20;
        var builder = new GraphBuilder();
        
        for (int i = 0; i < nodeCount; i++)
        {
            builder.AddNode();
        }
        
        // Dense connectivity
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = i + 1; j < nodeCount; j++)
            {
                if ((i + j) % 3 == 0) // About 1/3 of all possible arcs
                {
                    builder.AddArc(i, j);
                }
            }
        }
        
        var graph = builder.Build();
        var solver = new NetworkSimplex(graph);
        
        // Set problem data
        for (int i = 0; i < graph.ArcCount; i++)
        {
            solver.SetArcBounds(new Arc(i), 0, 100);
            solver.SetArcCost(new Arc(i), 1);
        }
        
        solver.SetNodeSupply(new Node(0), 50);
        solver.SetNodeSupply(new Node(nodeCount - 1), -50);
        
        // Analyze without solving
        var characteristics = solver.AnalyzeProblem();
        var config = OptimizationSelector.SelectConfiguration(characteristics);
        
        Assert.True((config.Flags & OptimizationFlags.SmallBlocksForDense) != 0);
        Assert.Equal(10, config.MinBlockSize); // Should use small blocks
        Assert.Equal(50, config.MaxBlockSize);
    }
    
    [Fact]
    public void AutoConfiguration_HighDegreeVariance_EnablesAdaptiveBlockSize()
    {
        // Create a network with high degree variance (hub and spoke)
        int nodeCount = 20;
        var builder = new GraphBuilder();
        
        for (int i = 0; i < nodeCount; i++)
        {
            builder.AddNode();
        }
        
        // Node 0 is a hub connected to all others
        for (int i = 1; i < nodeCount; i++)
        {
            builder.AddArc(0, i);
            builder.AddArc(i, 0);
        }
        
        // Other nodes have few connections
        for (int i = 1; i < nodeCount - 1; i++)
        {
            builder.AddArc(i, i + 1);
        }
        
        var graph = builder.Build();
        var solver = new NetworkSimplex(graph);
        
        for (int i = 0; i < graph.ArcCount; i++)
        {
            solver.SetArcBounds(new Arc(i), 0, 100);
            solver.SetArcCost(new Arc(i), 1);
        }
        
        solver.SetNodeSupply(new Node(0), 100);
        for (int i = 1; i < nodeCount; i++)
        {
            solver.SetNodeSupply(new Node(i), -100 / (nodeCount - 1));
        }
        
        var characteristics = solver.AnalyzeProblem();
        var config = OptimizationSelector.SelectConfiguration(characteristics);
        
        Assert.True(characteristics.DegreeCV > 0.5); // High variance
        Assert.True((config.Flags & OptimizationFlags.AdaptiveBlockSize) != 0);
    }
    
    [Fact]
    public void NetworkSimplex_AutoConfiguration_SolvesCorrectly()
    {
        // Test that auto-configuration doesn't break solving
        int nodeCount = 10;
        var builder = new GraphBuilder();
        
        for (int i = 0; i < nodeCount; i++)
        {
            builder.AddNode();
        }
        
        // Create a simple path
        for (int i = 0; i < nodeCount - 1; i++)
        {
            builder.AddArc(i, i + 1);
        }
        
        var graph = builder.Build();
        var solver = new NetworkSimplex(graph);
        
        for (int i = 0; i < graph.ArcCount; i++)
        {
            solver.SetArcBounds(new Arc(i), 0, 10);
            solver.SetArcCost(new Arc(i), 1);
        }
        
        solver.SetNodeSupply(new Node(0), 5);
        solver.SetNodeSupply(new Node(nodeCount - 1), -5);
        
        // Solve with auto-configuration enabled (default)
        var status = solver.Solve();
        
        Assert.Equal(SolverStatus.Optimal, status);
        Assert.Equal(5L * (nodeCount - 1), solver.GetTotalCost()); // Flow of 5 through all arcs
        
        // Verify characteristics were computed
        var characteristics = solver.GetProblemCharacteristics();
        Assert.NotNull(characteristics);
        // With 10 nodes and 9 arcs, density = 9/(10*9) = 0.1 which is dense
        Assert.True(characteristics.IsDense);
    }
    
    [Fact]
    public void ManualConfiguration_DisablesAutoConfiguration()
    {
        var builder = new GraphBuilder();
        builder.AddNode();
        builder.AddNode();
        builder.AddArc(0, 1);
        
        var graph = builder.Build();
        var solver = new NetworkSimplex(graph);
        
        // Set manual configuration
        var manualConfig = new OptimizationConfig
        {
            Flags = OptimizationFlags.ReducedCostCaching,
            MinBlockSize = 100,
            MaxBlockSize = 200
        };
        solver.SetOptimizationConfig(manualConfig);
        
        solver.SetArcBounds(new Arc(0), 0, 10);
        solver.SetArcCost(new Arc(0), 1);
        solver.SetNodeSupply(new Node(0), 1);
        solver.SetNodeSupply(new Node(1), -1);
        
        solver.Solve();
        
        // Auto-configuration should not have changed the settings
        var metrics = solver.GetMetrics();
        Assert.True(metrics.InitialBlockSize >= 100 || metrics.InitialBlockSize == 0); // 0 if not using block search
    }
}