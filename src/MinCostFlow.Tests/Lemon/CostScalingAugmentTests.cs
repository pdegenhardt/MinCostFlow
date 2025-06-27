using Xunit;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Types;
using MinCostFlow.Core.Lemon.Graphs;

namespace MinCostFlow.Tests.Lemon;

public class CostScalingAugmentTests
{
    [Fact]
    public void SimplePath_AugmentMethod_FindsOptimalFlow()
    {
        // Arrange
        var graph = new GraphBuilder()
            .AddNodes(3)
            .AddArc(0, 1)
            .AddArc(1, 2)
            .Build();
        
        var solver = new CostScaling(graph);
        solver.SetNodeSupply(new Node(0), 10);
        solver.SetNodeSupply(new Node(2), -10);
        solver.SetArcCost(new Arc(0), 5);
        solver.SetArcCost(new Arc(1), 3);
        solver.SetArcBounds(new Arc(0), 0, 15);
        solver.SetArcBounds(new Arc(1), 0, 15);
        
        // Act
        var status = solver.Solve(CostScaling.Method.Augment);
        
        // Assert
        Assert.Equal(SolverStatus.Optimal, status);
        Assert.Equal(10, solver.GetFlow(new Arc(0)));
        Assert.Equal(10, solver.GetFlow(new Arc(1)));
        Assert.Equal(80, solver.GetTotalCost()); // 10*5 + 10*3
    }
    
    [Fact]
    public void PartialAugmentMethod_FindsOptimalFlow()
    {
        // Arrange
        var graph = new GraphBuilder()
            .AddNodes(3)
            .AddArc(0, 1)
            .AddArc(1, 2)
            .Build();
        
        var solver = new CostScaling(graph);
        solver.SetNodeSupply(new Node(0), 10);
        solver.SetNodeSupply(new Node(2), -10);
        solver.SetArcCost(new Arc(0), 5);
        solver.SetArcCost(new Arc(1), 3);
        solver.SetArcBounds(new Arc(0), 0, 15);
        solver.SetArcBounds(new Arc(1), 0, 15);
        
        // Act
        var status = solver.Solve(CostScaling.Method.PartialAugment);
        
        // Assert
        Assert.Equal(SolverStatus.Optimal, status);
        Assert.Equal(10, solver.GetFlow(new Arc(0)));
        Assert.Equal(10, solver.GetFlow(new Arc(1)));
        Assert.Equal(80, solver.GetTotalCost()); // 10*5 + 10*3
    }
    
    [Fact]
    public void DiamondGraph_AugmentMethod_SameResultAsPush()
    {
        // Arrange
        var graph = new GraphBuilder()
            .AddNodes(4)
            .AddArc(0, 1) // Arc 0
            .AddArc(0, 2) // Arc 1
            .AddArc(1, 3) // Arc 2
            .AddArc(2, 3) // Arc 3
            .Build();
        
        // Solve with Push method
        var solverPush = new CostScaling(graph);
        solverPush.SetNodeSupply(new Node(0), 20);
        solverPush.SetNodeSupply(new Node(3), -20);
        solverPush.SetArcCost(new Arc(0), 4);
        solverPush.SetArcCost(new Arc(1), 2);
        solverPush.SetArcCost(new Arc(2), 1);
        solverPush.SetArcCost(new Arc(3), 3);
        solverPush.SetArcBounds(new Arc(0), 0, 15);
        solverPush.SetArcBounds(new Arc(1), 0, 15);
        solverPush.SetArcBounds(new Arc(2), 0, 15);
        solverPush.SetArcBounds(new Arc(3), 0, 15);
        
        var statusPush = solverPush.Solve(CostScaling.Method.Push);
        
        // Solve with Augment method
        var solverAugment = new CostScaling(graph);
        solverAugment.SetNodeSupply(new Node(0), 20);
        solverAugment.SetNodeSupply(new Node(3), -20);
        solverAugment.SetArcCost(new Arc(0), 4);
        solverAugment.SetArcCost(new Arc(1), 2);
        solverAugment.SetArcCost(new Arc(2), 1);
        solverAugment.SetArcCost(new Arc(3), 3);
        solverAugment.SetArcBounds(new Arc(0), 0, 15);
        solverAugment.SetArcBounds(new Arc(1), 0, 15);
        solverAugment.SetArcBounds(new Arc(2), 0, 15);
        solverAugment.SetArcBounds(new Arc(3), 0, 15);
        
        var statusAugment = solverAugment.Solve(CostScaling.Method.Augment);
        
        // Assert
        Assert.Equal(SolverStatus.Optimal, statusPush);
        Assert.Equal(SolverStatus.Optimal, statusAugment);
        Assert.Equal(solverPush.GetTotalCost(), solverAugment.GetTotalCost());
    }
    
    [Fact]
    public void MultipleSupplyDemand_PartialAugmentMethod()
    {
        // Arrange
        var graph = new GraphBuilder()
            .AddNodes(5)
            .AddArc(0, 2) // Arc 0
            .AddArc(1, 2) // Arc 1
            .AddArc(2, 3) // Arc 2
            .AddArc(2, 4) // Arc 3
            .Build();
        
        var solver = new CostScaling(graph);
        solver.SetNodeSupply(new Node(0), 8);
        solver.SetNodeSupply(new Node(1), 7);
        solver.SetNodeSupply(new Node(3), -9);
        solver.SetNodeSupply(new Node(4), -6);
        
        solver.SetArcCost(new Arc(0), 2);
        solver.SetArcCost(new Arc(1), 3);
        solver.SetArcCost(new Arc(2), 4);
        solver.SetArcCost(new Arc(3), 5);
        
        solver.SetArcBounds(new Arc(0), 0, 10);
        solver.SetArcBounds(new Arc(1), 0, 10);
        solver.SetArcBounds(new Arc(2), 0, 10);
        solver.SetArcBounds(new Arc(3), 0, 10);
        
        // Act
        var status = solver.Solve(CostScaling.Method.PartialAugment);
        
        // Assert
        Assert.Equal(SolverStatus.Optimal, status);
        Assert.Equal(8, solver.GetFlow(new Arc(0)));
        Assert.Equal(7, solver.GetFlow(new Arc(1)));
        Assert.Equal(9, solver.GetFlow(new Arc(2)));
        Assert.Equal(6, solver.GetFlow(new Arc(3)));
        Assert.Equal(8*2 + 7*3 + 9*4 + 6*5, solver.GetTotalCost());
    }
}