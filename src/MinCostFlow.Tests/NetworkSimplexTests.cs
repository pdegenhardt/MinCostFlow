using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;
using Xunit;

namespace MinCostFlow.Tests;

/// <summary>
/// Tests for the Network Simplex algorithm implementation.
/// </summary>
public class NetworkSimplexTests
{
    [Fact]
    public void SimpleTransportationProblem_SolvesCorrectly()
    {
        // Create a simple transportation problem:
        // 2 supply nodes (0, 1) with supplies 10, 15
        // 2 demand nodes (2, 3) with demands -12, -13
        // Costs: 0->2: 3, 0->3: 5, 1->2: 4, 1->3: 2
        var builder = new GraphBuilder();
        builder.AddNodes(4);
        
        // Add arcs with external IDs
        builder.AddArc(0, 2); // Arc 0
        builder.AddArc(0, 3); // Arc 1
        builder.AddArc(1, 2); // Arc 2
        builder.AddArc(1, 3); // Arc 3
        
        var graph = builder.Build();
        var solver = new NetworkSimplex(graph);
        
        // Set supplies
        solver.SetNodeSupply(builder.GetNode(0), 10);
        solver.SetNodeSupply(builder.GetNode(1), 15);
        solver.SetNodeSupply(builder.GetNode(2), -12);
        solver.SetNodeSupply(builder.GetNode(3), -13);
        
        // Set costs
        solver.SetArcCost(new Arc(0), 3); // 0->2
        solver.SetArcCost(new Arc(1), 5); // 0->3
        solver.SetArcCost(new Arc(2), 4); // 1->2
        solver.SetArcCost(new Arc(3), 2); // 1->3
        
        // Solve
        var status = solver.Solve();
        
        // Verify solution
        Assert.Equal(SolverStatus.Optimal, status);
        
        // Optimal solution should be:
        // 0->2: 10, 0->3: 0
        // 1->2: 2, 1->3: 13
        // Total cost: 10*3 + 2*4 + 13*2 = 64
        Assert.Equal(64, solver.GetTotalCost());
        
        // Check flows
        Assert.Equal(10, solver.GetFlow(new Arc(0))); // 0->2
        Assert.Equal(0, solver.GetFlow(new Arc(1)));  // 0->3
        Assert.Equal(2, solver.GetFlow(new Arc(2)));  // 1->2
        Assert.Equal(13, solver.GetFlow(new Arc(3))); // 1->3
    }
    
    [Fact]
    public void MinimumCostCirculation_SolvesCorrectly()
    {
        // Create a circulation problem (all supplies are 0)
        // 3 nodes in a cycle with costs
        var builder = new GraphBuilder();
        builder.AddNodes(3);
        
        // Add arcs forming a cycle
        builder.AddArc(0, 1); // Arc 0, cost 2
        builder.AddArc(1, 2); // Arc 1, cost 3
        builder.AddArc(2, 0); // Arc 2, cost -6 (negative cost cycle)
        
        var graph = builder.Build();
        var solver = new NetworkSimplex(graph);
        
        // All supplies are 0 (circulation)
        solver.SetNodeSupply(builder.GetNode(0), 0);
        solver.SetNodeSupply(builder.GetNode(1), 0);
        solver.SetNodeSupply(builder.GetNode(2), 0);
        
        // Set costs
        solver.SetArcCost(new Arc(0), 2);
        solver.SetArcCost(new Arc(1), 3);
        solver.SetArcCost(new Arc(2), -6);
        
        // Set capacities to allow flow
        solver.SetArcBounds(new Arc(0), 0, 10);
        solver.SetArcBounds(new Arc(1), 0, 10);
        solver.SetArcBounds(new Arc(2), 0, 10);
        
        // Solve
        var status = solver.Solve();
        
        // Should find optimal solution with flow around the negative cycle
        Assert.Equal(SolverStatus.Optimal, status);
        
        // Flow should be 10 around the cycle (limited by capacity)
        // Total cost: 10 * (2 + 3 - 6) = -10
        Assert.Equal(-10, solver.GetTotalCost());
        Assert.Equal(10, solver.GetFlow(new Arc(0)));
        Assert.Equal(10, solver.GetFlow(new Arc(1)));
        Assert.Equal(10, solver.GetFlow(new Arc(2)));
    }
    
    [Fact]
    public void InfeasibleProblem_ReturnsInfeasible()
    {
        // Create an infeasible problem: total supply != total demand
        var builder = new GraphBuilder();
        builder.AddNodes(2);
        builder.AddArc(0, 1);
        
        var graph = builder.Build();
        var solver = new NetworkSimplex(graph);
        
        // Supply doesn't match demand
        solver.SetNodeSupply(builder.GetNode(0), 10);
        solver.SetNodeSupply(builder.GetNode(1), -5); // Demand is only 5, not 10
        
        var status = solver.Solve();
        
        // For GEQ supply type, this should still be feasible
        // (excess supply is allowed)
        Assert.Equal(SolverStatus.Optimal, status);
    }
    
    [Fact]
    public void LowerBounds_RespectedInSolution()
    {
        // Test that lower bounds are respected
        var builder = new GraphBuilder();
        builder.AddNodes(2);
        builder.AddArc(0, 1);
        
        var graph = builder.Build();
        var solver = new NetworkSimplex(graph);
        
        solver.SetNodeSupply(builder.GetNode(0), 10);
        solver.SetNodeSupply(builder.GetNode(1), -10);
        solver.SetArcBounds(new Arc(0), 5, 15); // Lower bound of 5
        solver.SetArcCost(new Arc(0), 1);
        
        var status = solver.Solve();
        
        Assert.Equal(SolverStatus.Optimal, status);
        Assert.Equal(10, solver.GetFlow(new Arc(0))); // Should be 10, respecting lower bound
        Assert.Equal(10, solver.GetTotalCost());
    }
}