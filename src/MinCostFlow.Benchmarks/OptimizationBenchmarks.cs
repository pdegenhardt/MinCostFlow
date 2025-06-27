using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Core.Lemon.Types;
using MinCostFlow.Core.Utils;
using System;

namespace MinCostFlow.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class OptimizationBenchmarks
{
    private CompactDigraph _smallGraph = null!;
    private CompactDigraph _mediumGraph = null!;
    private CompactDigraph _largeGraph = null!;
    private SolverMemoryPool _memoryPool = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        // Small graph: 100 nodes, 300 arcs
        _smallGraph = GenerateTransportationProblem(10, 10, 42);
        
        // Medium graph: 1000 nodes, 3000 arcs
        _mediumGraph = GenerateTransportationProblem(30, 34, 43);
        
        // Large graph: 10000 nodes, 15000 arcs
        _largeGraph = GenerateTransportationProblem(100, 100, 44);
        
        // Initialize memory pool
        _memoryPool = new SolverMemoryPool();
        _memoryPool.PreAllocate(_largeGraph.NodeCount, _largeGraph.ArcCount);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _memoryPool?.Dispose();
    }
    
    // Baseline benchmarks
    [Benchmark(Baseline = true)]
    [Arguments("Small")]
    [Arguments("Medium")]
    [Arguments("Large")]
    public SolverStatus SolveBaseline(string size)
    {
        var graph = GetGraph(size);
        var solver = new NetworkSimplex(graph);
        SetupProblem(solver, graph);
        return solver.Solve();
    }
    
    // Optimized pivot only
    [Benchmark]
    [Arguments("Small")]
    [Arguments("Medium")]
    [Arguments("Large")]
    public SolverStatus SolveOptimizedPivot(string size)
    {
        var graph = GetGraph(size);
        var solver = new NetworkSimplex(graph);
        solver.EnableOptimizedPivot(true);
        SetupProblem(solver, graph);
        return solver.Solve();
    }
    
    // Optimized potential update only - removed as it was ineffective
    // [Benchmark]
    // [Arguments("Small")]
    // [Arguments("Medium")]
    // [Arguments("Large")]
    // public SolverStatus SolveOptimizedPotentialUpdate(string size)
    // {
    //     var graph = GetGraph(size);
    //     var solver = new NetworkSimplex(graph);
    //     solver.EnableOptimizedPotentialUpdate(true);
    //     SetupProblem(solver, graph);
    //     return solver.Solve();
    // }
    
    // Both optimizations
    [Benchmark]
    [Arguments("Small")]
    [Arguments("Medium")]
    [Arguments("Large")]
    public SolverStatus SolveFullyOptimized(string size)
    {
        var graph = GetGraph(size);
        var solver = new NetworkSimplex(graph);
        solver.EnableOptimizedPivot(true);
        // solver.EnableOptimizedPotentialUpdate(true); // Removed - ineffective
        SetupProblem(solver, graph);
        return solver.Solve();
    }
    
    // With memory pool
    [Benchmark]
    [Arguments("Small")]
    [Arguments("Medium")]
    [Arguments("Large")]
    public SolverStatus SolveWithMemoryPool(string size)
    {
        var graph = GetGraph(size);
        var solver = new NetworkSimplex(graph);
        solver.EnableOptimizedPivot(true);
        // solver.EnableOptimizedPotentialUpdate(true); // Removed - ineffective
        solver.SetMemoryPool(_memoryPool);
        SetupProblem(solver, graph);
        return solver.Solve();
    }
    
    // Different pivot rules
    [Benchmark]
    [Arguments("Medium", PivotRule.BlockSearch)]
    [Arguments("Medium", PivotRule.FirstEligible)]
    [Arguments("Medium", PivotRule.BestEligible)]
    public SolverStatus SolvePivotRuleComparison(string size, PivotRule pivotRule)
    {
        var graph = GetGraph(size);
        var solver = new NetworkSimplex(graph);
        solver.EnableOptimizedPivot(true);
        // solver.EnableOptimizedPotentialUpdate(true); // Removed - ineffective
        solver.SetPivotRule(pivotRule);
        SetupProblem(solver, graph);
        return solver.Solve();
    }
    
    private CompactDigraph GetGraph(string size) => size switch
    {
        "Small" => _smallGraph,
        "Medium" => _mediumGraph,
        "Large" => _largeGraph,
        _ => throw new ArgumentException($"Unknown size: {size}")
    };
    
    private static CompactDigraph GenerateTransportationProblem(int suppliers, int customers, int seed)
    {
        var random = new Random(seed);
        var builder = new GraphBuilder();
        
        // Add nodes
        builder.AddNodes(suppliers + customers);
        
        // Add arcs with random costs
        for (int i = 0; i < suppliers; i++)
        {
            for (int j = 0; j < customers; j++)
            {
                if (random.NextDouble() < 0.3) // 30% density
                {
                    builder.AddArc(i, suppliers + j);
                }
            }
        }
        
        return builder.Build();
    }
    
    private static void SetupProblem(NetworkSimplex solver, CompactDigraph graph)
    {
        var random = new Random(12345);
        var builder = new GraphBuilder();
        
        // Create nodes to get proper Node references
        for (int i = 0; i < graph.NodeCount; i++)
        {
            builder.AddNode();
        }
        
        // Set random costs
        int arcId = 0;
        for (var arc = graph.FirstArc(); arc.IsValid; arc = graph.NextArc(arc))
        {
            solver.SetArcCost(new Arc(arcId), random.Next(1, 100));
            solver.SetArcBounds(new Arc(arcId), 0, random.Next(10, 1000));
            arcId++;
        }
        
        // Set supplies to create a balanced problem
        long totalSupply = 0;
        int halfNodes = graph.NodeCount / 2;
        
        for (int i = 0; i < halfNodes; i++)
        {
            long supply = random.Next(10, 100);
            solver.SetNodeSupply(builder.GetNode(i), supply);
            totalSupply += supply;
        }
        
        // Distribute demand
        for (int i = halfNodes; i < graph.NodeCount - 1; i++)
        {
            long demand = totalSupply / (graph.NodeCount - halfNodes);
            solver.SetNodeSupply(builder.GetNode(i), -demand);
        }
        
        // Balance on last node
        solver.SetNodeSupply(builder.GetNode(graph.NodeCount - 1), 
            -(totalSupply - (totalSupply / (graph.NodeCount - halfNodes)) * (graph.NodeCount - halfNodes - 1)));
    }
}