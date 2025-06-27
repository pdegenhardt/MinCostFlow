using System;
using System.Linq;
using MinCostFlow.Benchmarks.Solvers;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Core.Lemon.Types;
using MinCostFlow.Core.Lemon.Validation;
using MinCostFlow.Problems;
using MinCostFlow.Problems.Models;
using MinCostFlow.Problems.Sets;
using Xunit;
using Xunit.Abstractions;

namespace MinCostFlow.Tests.Lemon;

/// <summary>
/// Tests that compare NetworkSimplex and OR-Tools solvers to ensure they produce
/// the same optimal solutions.
/// </summary>
public class SolverComparisonTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private readonly ProblemRepository _repository = new();

    [Theory]
    [InlineData("Small_Path5")]
    [InlineData("Small_Path10")]
    [InlineData("Small_Grid2x2")]
    [InlineData("Small_Diamond")]
    [InlineData("Small_Assignment3x3")]
    [InlineData("Small_Transport2x3")]
    public void SmallProblems_BothSolversAgreeOnOptimalCost(string problemType)
    {
        var smallProblems = StandardProblems.GetByCategory("Small").ToList();
        MinCostFlowProblem problem = problemType switch
        {
            "Small_Path5" => smallProblems.First(p => p.Metadata?.Name == "path_5node"),
            "Small_Path10" => smallProblems.First(p => p.Metadata?.Name == "path_10node"),
            "Small_Grid2x2" => smallProblems.First(p => p.Metadata?.Name == "grid_2x2"),
            "Small_Diamond" => smallProblems.First(p => p.Metadata?.Name == "diamond_graph"),
            "Small_Assignment3x3" => smallProblems.First(p => p.Metadata?.Name == "assignment_3x3"),
            "Small_Transport2x3" => smallProblems.First(p => p.Metadata?.Name == "transport_2x3"),
            _ => throw new ArgumentException($"Unknown problem type: {problemType}")
        };

        CompareSolvers(problem, problemType);
    }

    [Theory]
    [InlineData("DIMACS_Netgen8_08a")]
    [InlineData("DIMACS_Netgen8_10a")]
    public void DimacsProblems_BothSolversAgreeOnOptimalCost(string problemType)
    {
        var dimacsProblems = StandardProblems.GetByCategory("DIMACS").ToList();
        MinCostFlowProblem problem = problemType switch
        {
            "DIMACS_Netgen8_08a" => dimacsProblems.First(p => p.Metadata?.Name == "netgen_8_08a"),
            "DIMACS_Netgen8_10a" => dimacsProblems.First(p => p.Metadata?.Name == "netgen_8_10a"),
            _ => throw new ArgumentException($"Unknown problem type: {problemType}")
        };

        CompareSolvers(problem, problemType);
    }

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(500, 22, 22)]
    [InlineData(1000, 32, 32)]
    public void GeneratedTransportationProblems_BothSolversAgree(int size, int sources, int sinks)
    {
        var problem = _repository.GenerateTransportationProblem(sources, sinks, 1000);
        var problemName = $"Transport_{sources}x{sinks}";
        CompareSolvers(problem, problemName);
    }

    [Theory]
    [InlineData(100, 0.1)]
    [InlineData(500, 0.05)]
    [InlineData(1000, 0.02)]
    public void GeneratedCirculationProblems_BothSolversAgree(int nodes, double density)
    {
        var problem = _repository.GenerateCirculationProblem(nodes, density);
        var problemName = $"Circulation_{nodes}_d{density}";
        CompareSolvers(problem, problemName);
    }

    [Theory]
    [InlineData(100, 1000)]
    [InlineData(500, 5000)]
    [InlineData(1000, 10000)]
    public void GeneratedPathProblems_BothSolversAgree(int nodes, long supply)
    {
        var problem = _repository.GeneratePathProblem(nodes, supply);
        var problemName = $"Path_{nodes}";
        CompareSolvers(problem, problemName);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(20, 20)]
    [InlineData(30, 30)]
    public void GeneratedGridProblems_BothSolversAgree(int rows, int cols)
    {
        var problem = _repository.GenerateGridProblem(rows, cols, 0, 0, rows - 1, cols - 1, 1000);
        var problemName = $"Grid_{rows}x{cols}";
        CompareSolvers(problem, problemName);
    }

    private void CompareSolvers(MinCostFlowProblem problem, string problemName)
    {
        _output.WriteLine($"\nComparing solvers on {problemName}:");
        _output.WriteLine($"  Nodes: {problem.NodeCount}");
        _output.WriteLine($"  Arcs: {problem.ArcCount}");

        // Solve with NetworkSimplex
        var nsSolver = new NetworkSimplex(problem.Graph);
        ConfigureNetworkSimplex(nsSolver, problem);
        var nsStatus = nsSolver.Solve();
        var nsCost = nsStatus == SolverStatus.Optimal ? nsSolver.GetTotalCost() : 0;

        _output.WriteLine($"  NetworkSimplex: Status={nsStatus}, Cost={nsCost}");

        // Solve with OR-Tools
        var orSolver = new OrToolsSolver(problem.Graph);
        ConfigureOrTools(orSolver, problem);
        var orStatus = orSolver.Solve();
        var orCost = orStatus == SolverStatus.Optimal ? orSolver.GetTotalCost() : 0;

        _output.WriteLine($"  OR-Tools: Status={orStatus}, Cost={orCost}");

        // Verify both found optimal solutions
        Assert.Equal(SolverStatus.Optimal, nsStatus);
        Assert.Equal(SolverStatus.Optimal, orStatus);

        // Verify costs match
        Assert.Equal(nsCost, orCost);

        // Validate NetworkSimplex solution
        ValidateNetworkSimplexSolution(problem.Graph, nsSolver, "NetworkSimplex");

        // Compare flow values (they might differ due to alternate optimal solutions)
        CompareFlows(problem, nsSolver, orSolver);
    }

    private static void ConfigureNetworkSimplex(NetworkSimplex solver, MinCostFlowProblem problem)
    {
        // Set supplies
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (problem.NodeSupplies[i] != 0)
            {
                solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
            }
        }

        // Set arc data
        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            solver.SetArcCost(arc, problem.ArcCosts[i]);
            solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
        }
    }

    private static void ConfigureOrTools(OrToolsSolver solver, MinCostFlowProblem problem)
    {
        // Set supplies
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (problem.NodeSupplies[i] != 0)
            {
                solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
            }
        }

        // Set arc data
        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            solver.SetArcCost(arc, problem.ArcCosts[i]);
            solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
        }
    }

    private void ValidateNetworkSimplexSolution(IGraph graph, NetworkSimplex solver, string solverName)
    {
        var validator = new SolutionValidator(graph, solver);
        var result = validator.Validate();

        Assert.True(result.IsValid, 
            $"{solverName} solution validation failed: {string.Join("; ", result.Errors)}");
        Assert.Empty(result.Errors);
        
        if (result.Warnings.Count != 0)
        {
            _output.WriteLine($"  {solverName} warnings: {string.Join("; ", result.Warnings)}");
        }
    }

    private void CompareFlows(MinCostFlowProblem problem, NetworkSimplex solver1, OrToolsSolver solver2)
    {
        var totalFlow1 = 0L;
        var totalFlow2 = 0L;
        var differentFlows = 0;

        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            var flow1 = solver1.GetFlow(arc);
            var flow2 = solver2.GetFlow(arc);

            totalFlow1 += flow1;
            totalFlow2 += flow2;

            if (flow1 != flow2)
            {
                differentFlows++;
            }
        }

        _output.WriteLine($"  Total flow comparison: NS={totalFlow1}, OR={totalFlow2}");
        _output.WriteLine($"  Arcs with different flows: {differentFlows}/{problem.ArcCount} " +
                        $"({100.0 * differentFlows / problem.ArcCount:F1}%)");

        // In case of alternate optimal solutions, flows might differ but totals should be similar
        if (differentFlows > 0)
        {
            _output.WriteLine("  Note: Different flows detected (likely alternate optimal solutions)");
        }
    }

    [Fact]
    public void LowerBoundsHandling_BothSolversAgree()
    {
        // Create a simple problem with lower bounds
        var builder = new GraphBuilder();
        
        // 4 nodes: source (0), intermediate (1,2), sink (3)
        builder.AddNode(); // 0
        builder.AddNode(); // 1
        builder.AddNode(); // 2
        builder.AddNode(); // 3
        
        // Arcs with lower bounds
        builder.AddArc(0, 1); // Arc 0: lower=5, upper=10, cost=1
        builder.AddArc(0, 2); // Arc 1: lower=3, upper=8, cost=2
        builder.AddArc(1, 3); // Arc 2: lower=2, upper=15, cost=1
        builder.AddArc(2, 3); // Arc 3: lower=4, upper=12, cost=1
        
        var graph = builder.Build();
        
        // Test both solvers
        var nsSolver = new NetworkSimplex(graph);
        var orSolver = new OrToolsSolver(graph);
        
        // Set supplies
        nsSolver.SetNodeSupply(new Node(0), 10); // source
        nsSolver.SetNodeSupply(new Node(3), -10); // sink
        orSolver.SetNodeSupply(new Node(0), 10);
        orSolver.SetNodeSupply(new Node(3), -10);
        
        // Set bounds and costs
        nsSolver.SetArcBounds(new Arc(0), 5, 10);
        nsSolver.SetArcCost(new Arc(0), 1);
        nsSolver.SetArcBounds(new Arc(1), 3, 8);
        nsSolver.SetArcCost(new Arc(1), 2);
        nsSolver.SetArcBounds(new Arc(2), 2, 15);
        nsSolver.SetArcCost(new Arc(2), 1);
        nsSolver.SetArcBounds(new Arc(3), 4, 12);
        nsSolver.SetArcCost(new Arc(3), 1);
        
        orSolver.SetArcBounds(new Arc(0), 5, 10);
        orSolver.SetArcCost(new Arc(0), 1);
        orSolver.SetArcBounds(new Arc(1), 3, 8);
        orSolver.SetArcCost(new Arc(1), 2);
        orSolver.SetArcBounds(new Arc(2), 2, 15);
        orSolver.SetArcCost(new Arc(2), 1);
        orSolver.SetArcBounds(new Arc(3), 4, 12);
        orSolver.SetArcCost(new Arc(3), 1);
        
        // Solve
        var nsStatus = nsSolver.Solve();
        var orStatus = orSolver.Solve();
        
        Assert.Equal(SolverStatus.Optimal, nsStatus);
        Assert.Equal(SolverStatus.Optimal, orStatus);
        
        // Compare costs
        var nsCost = nsSolver.GetTotalCost();
        var orCost = orSolver.GetTotalCost();
        
        _output.WriteLine($"Lower bounds test:");
        _output.WriteLine($"  NetworkSimplex cost: {nsCost}");
        _output.WriteLine($"  OR-Tools cost: {orCost}");
        
        Assert.Equal(nsCost, orCost);
        
        // Verify flows respect lower bounds
        for (int i = 0; i < 4; i++)
        {
            var arc = new Arc(i);
            var nsFlow = nsSolver.GetFlow(arc);
            var orFlow = orSolver.GetFlow(arc);
            var (lower, upper) = orSolver.GetArcBounds(arc);
            
            _output.WriteLine($"  Arc {i}: NS={nsFlow}, OR={orFlow}, bounds=[{lower},{upper}]");
            
            Assert.True(nsFlow >= lower, $"NS flow {nsFlow} violates lower bound {lower}");
            Assert.True(nsFlow <= upper, $"NS flow {nsFlow} violates upper bound {upper}");
            Assert.True(orFlow >= lower, $"OR flow {orFlow} violates lower bound {lower}");
            Assert.True(orFlow <= upper, $"OR flow {orFlow} violates upper bound {upper}");
        }
    }
}