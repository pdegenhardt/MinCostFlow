using MinCostFlow.Core;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Core.Lemon.Types;
using MinCostFlow.Problems.Models;
using Xunit;
using Xunit.Abstractions;

namespace MinCostFlow.Tests.Lemon;

/// <summary>
/// Tests for the CostScaling algorithm implementation.
/// </summary>
public class CostScalingTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void SimplePathProblem_FindsOptimalSolution()
    {
        // Create a simple path: 0 -> 1 -> 2
        var builder = new GraphBuilder();
        builder.AddNodes(3);
        builder.AddArc(0, 1);  // Arc 0
        builder.AddArc(1, 2);  // Arc 1
        var graph = builder.Build();
        
        var solver = new CostScaling(graph);
        
        // Set supplies: 10 units at node 0, -10 at node 2
        solver.SetNodeSupply(new Node(0), 10);
        solver.SetNodeSupply(new Node(2), -10);
        
        // Set costs and capacities
        solver.SetArcCost(new Arc(0), 5);
        solver.SetArcBounds(new Arc(0), 0, 15);
        
        solver.SetArcCost(new Arc(1), 3);
        solver.SetArcBounds(new Arc(1), 0, 15);
        
        // Solve - use Push method for now since others aren't implemented
        var status = solver.Solve(CostScaling.Method.Push);
        
        Assert.Equal(SolverStatus.Optimal, status);
        
        // Check flows
        Assert.Equal(10, solver.GetFlow(new Arc(0)));
        Assert.Equal(10, solver.GetFlow(new Arc(1)));
        
        // Check total cost: 10 * 5 + 10 * 3 = 80
        Assert.Equal(80, solver.GetTotalCost());
        
        _output.WriteLine($"Status: {status}");
        _output.WriteLine($"Flow on arc 0: {solver.GetFlow(new Arc(0))}");
        _output.WriteLine($"Flow on arc 1: {solver.GetFlow(new Arc(1))}");
        _output.WriteLine($"Total cost: {solver.GetTotalCost()}");
    }
    
    [Fact]
    public void DiamondProblem_FindsOptimalSolution()
    {
        // Create diamond graph:
        //     1
        //   /   \
        // 0       3
        //   \   /
        //     2
        var builder = new GraphBuilder();
        builder.AddNodes(4);
        builder.AddArc(0, 1);  // Arc 0 (upper path)
        builder.AddArc(0, 2);  // Arc 1 (lower path)
        builder.AddArc(1, 3);  // Arc 2
        builder.AddArc(2, 3);  // Arc 3
        var graph = builder.Build();
        
        var solver = new CostScaling(graph);
        
        // Set supplies: 20 units at node 0, -20 at node 3
        solver.SetNodeSupply(new Node(0), 20);
        solver.SetNodeSupply(new Node(3), -20);
        
        // Upper path: more expensive but higher capacity
        solver.SetArcCost(new Arc(0), 10);
        solver.SetArcBounds(new Arc(0), 0, 15);
        solver.SetArcCost(new Arc(2), 5);
        solver.SetArcBounds(new Arc(2), 0, 15);
        
        // Lower path: cheaper but lower capacity
        solver.SetArcCost(new Arc(1), 3);
        solver.SetArcBounds(new Arc(1), 0, 10);
        solver.SetArcCost(new Arc(3), 2);
        solver.SetArcBounds(new Arc(3), 0, 10);
        
        var status = solver.Solve(CostScaling.Method.Push);
        
        Assert.Equal(SolverStatus.Optimal, status);
        
        // Should use lower path to capacity, then upper path
        Assert.Equal(10, solver.GetFlow(new Arc(1)));  // Lower path at capacity
        Assert.Equal(10, solver.GetFlow(new Arc(3)));
        Assert.Equal(10, solver.GetFlow(new Arc(0)));  // Upper path for remainder
        Assert.Equal(10, solver.GetFlow(new Arc(2)));
        
        // Total cost: 10*3 + 10*2 + 10*10 + 10*5 = 30 + 20 + 100 + 50 = 200
        Assert.Equal(200, solver.GetTotalCost());
    }
    
   
    private static void SetupSolver<T>(T solver, MinCostFlowProblem problem) 
        where T : IMinCostFlowSolver
    {
        // Set node supplies
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (solver is NetworkSimplex ns)
            {
                ns.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
            }
            else if (solver is CostScaling cs)
            {
                cs.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
            }
        }
        
        // Set arc data
        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            if (solver is NetworkSimplex ns)
            {
                ns.SetArcCost(arc, problem.ArcCosts[i]);
                ns.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
            }
            else if (solver is CostScaling cs)
            {
                cs.SetArcCost(arc, problem.ArcCosts[i]);
                cs.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
            }
        }
    }
}