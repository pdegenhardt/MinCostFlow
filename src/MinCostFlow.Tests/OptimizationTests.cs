using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;
using MinCostFlow.Core.Utils;
using System;
using Xunit;

namespace MinCostFlow.Tests;

public class OptimizationTests
{
    [Fact]
    public void OptimizedPivot_ProducesSameResults()
    {
        // Create a test problem
        var builder = new GraphBuilder();
        builder.AddNodes(5);
        
        // Add arcs
        builder.AddArc(0, 1); // Arc 0
        builder.AddArc(0, 2); // Arc 1
        builder.AddArc(1, 3); // Arc 2
        builder.AddArc(2, 3); // Arc 3
        builder.AddArc(2, 4); // Arc 4
        builder.AddArc(3, 4); // Arc 5
        
        var graph = builder.Build();
        
        // Solve with baseline
        var baselineSolver = new NetworkSimplex(graph);
        SetupProblem(baselineSolver, builder);
        var baselineStatus = baselineSolver.Solve();
        var baselineCost = baselineSolver.GetTotalCost();
        var baselineFlows = new long[6];
        for (int i = 0; i < 6; i++)
        {
            baselineFlows[i] = baselineSolver.GetFlow(new Arc(i));
        }
        
        // Solve with optimized pivot
        var optimizedSolver = new NetworkSimplex(graph);
        SetupProblem(optimizedSolver, builder);
        optimizedSolver.EnableOptimizedPivot(true);
        var optimizedStatus = optimizedSolver.Solve();
        var optimizedCost = optimizedSolver.GetTotalCost();
        
        // Compare results
        Assert.Equal(baselineStatus, optimizedStatus);
        Assert.Equal(baselineCost, optimizedCost);
        
        // Compare flows
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(baselineFlows[i], optimizedSolver.GetFlow(new Arc(i)));
        }
    }
    
    [Fact]
    public void OptimizedPotentialUpdate_ProducesSameResults()
    {
        // Create a larger test problem
        var builder = new GraphBuilder();
        builder.AddNodes(10);
        
        // Create a more complex graph
        var random = new Random(42);
        int arcCount = 0;
        for (int i = 0; i < 9; i++)
        {
            for (int j = i + 1; j < 10; j++)
            {
                if (random.NextDouble() < 0.4)
                {
                    builder.AddArc(i, j);
                    arcCount++;
                }
            }
        }
        
        var graph = builder.Build();
        
        // Solve with baseline
        var baselineSolver = new NetworkSimplex(graph);
        SetupLargerProblem(baselineSolver, builder, arcCount);
        var baselineStatus = baselineSolver.Solve();
        
        // Skip test if problem is infeasible
        if (baselineStatus != SolverStatus.Optimal)
        {
            Assert.Equal(SolverStatus.Infeasible, baselineStatus);
            return;
        }
        
        var baselineCost = baselineSolver.GetTotalCost();
        
        // Solve with optimized potential update
        var optimizedSolver = new NetworkSimplex(graph);
        SetupLargerProblem(optimizedSolver, builder, arcCount);
        optimizedSolver.EnableOptimizedPotentialUpdate(true);
        var optimizedStatus = optimizedSolver.Solve();
        
        // Compare results
        Assert.Equal(baselineStatus, optimizedStatus);
        if (optimizedStatus == SolverStatus.Optimal)
        {
            var optimizedCost = optimizedSolver.GetTotalCost();
            Assert.Equal(baselineCost, optimizedCost);
        }
    }
    
    [Fact]
    public void AllOptimizations_ProduceCorrectResults()
    {
        // Test with different pivot rules
        foreach (var pivotRule in new[] { PivotRule.BlockSearch, PivotRule.FirstEligible, PivotRule.BestEligible })
        {
            var builder = new GraphBuilder();
            builder.AddNodes(8);
            
            // Create a grid-like graph
            builder.AddArc(0, 1); 
            builder.AddArc(1, 2);
            builder.AddArc(2, 3);
            builder.AddArc(0, 4);
            builder.AddArc(1, 5);
            builder.AddArc(2, 6);
            builder.AddArc(3, 7);
            builder.AddArc(4, 5);
            builder.AddArc(5, 6);
            builder.AddArc(6, 7);
            
            var graph = builder.Build();
            
            // Solve with all optimizations
            var solver = new NetworkSimplex(graph);
            var memoryPool = new SolverMemoryPool();
            memoryPool.PreAllocate(graph.NodeCount, graph.ArcCount);
            
            SetupGridProblem(solver, builder);
            solver.SetPivotRule(pivotRule);
            solver.EnableOptimizedPivot(true);
            solver.EnableOptimizedPotentialUpdate(true);
            solver.SetMemoryPool(memoryPool);
            
            var status = solver.Solve();
            
            // Verify solution is optimal
            Assert.Equal(SolverStatus.Optimal, status);
            
            // Verify flow conservation
            for (int i = 0; i < 8; i++)
            {
                long flowIn = 0, flowOut = 0;
                
                // Calculate flow balance
                int arcId = 0;
                for (var arc = graph.FirstArc(); arc.IsValid; arc = graph.NextArc(arc))
                {
                    if (graph.Target(arc).Id == i)
                        flowIn += solver.GetFlow(new Arc(arcId));
                    if (graph.Source(arc).Id == i)
                        flowOut += solver.GetFlow(new Arc(arcId));
                    arcId++;
                }
                
                long expectedSupply = (i == 0) ? 100 : (i == 7) ? -100 : 0;
                long actualSupply = flowOut - flowIn;
                Assert.Equal(expectedSupply, actualSupply);
            }
            
            memoryPool.Dispose();
        }
    }
    
    private static void SetupProblem(NetworkSimplex solver, GraphBuilder builder)
    {
        // Set supplies
        solver.SetNodeSupply(builder.GetNode(0), 50);
        solver.SetNodeSupply(builder.GetNode(1), 20);
        solver.SetNodeSupply(builder.GetNode(2), -10);
        solver.SetNodeSupply(builder.GetNode(3), -30);
        solver.SetNodeSupply(builder.GetNode(4), -30);
        
        // Set costs
        solver.SetArcCost(new Arc(0), 10);
        solver.SetArcCost(new Arc(1), 20);
        solver.SetArcCost(new Arc(2), 30);
        solver.SetArcCost(new Arc(3), 15);
        solver.SetArcCost(new Arc(4), 25);
        solver.SetArcCost(new Arc(5), 35);
        
        // Set capacities
        for (int i = 0; i < 6; i++)
        {
            solver.SetArcBounds(new Arc(i), 0, 100);
        }
    }
    
    private static void SetupLargerProblem(NetworkSimplex solver, GraphBuilder builder, int arcCount)
    {
        var random = new Random(123);
        
        // Set random costs and capacities
        for (int i = 0; i < arcCount; i++)
        {
            solver.SetArcCost(new Arc(i), random.Next(1, 50));
            solver.SetArcBounds(new Arc(i), 0, random.Next(50, 200));
        }
        
        // Create a balanced supply/demand
        long totalSupply = 0;
        for (int i = 0; i < 5; i++)
        {
            long supply = random.Next(10, 50);
            solver.SetNodeSupply(builder.GetNode(i), supply);
            totalSupply += supply;
        }
        
        long remainingDemand = totalSupply;
        for (int i = 5; i < 9; i++)
        {
            long demand = totalSupply / 5;
            solver.SetNodeSupply(builder.GetNode(i), -demand);
            remainingDemand -= demand;
        }
        // Set the last node to balance exactly
        solver.SetNodeSupply(builder.GetNode(9), -remainingDemand);
    }
    
    private static void SetupGridProblem(NetworkSimplex solver, GraphBuilder builder)
    {
        // Set costs based on arc position
        for (int i = 0; i < 10; i++)
        {
            solver.SetArcCost(new Arc(i), (i + 1) * 10);
            solver.SetArcBounds(new Arc(i), 0, 100);
        }
        
        // Set supplies
        solver.SetNodeSupply(builder.GetNode(0), 100);
        solver.SetNodeSupply(builder.GetNode(7), -100);
        for (int i = 1; i < 7; i++)
        {
            solver.SetNodeSupply(builder.GetNode(i), 0);
        }
    }
}