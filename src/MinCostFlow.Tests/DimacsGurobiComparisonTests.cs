using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Problems.Sets;
using MinCostFlow.Core.Types;
using MinCostFlow.Core.Validation;
using Xunit;
using Xunit.Abstractions;

namespace MinCostFlow.Tests;

/// <summary>
/// Tests that compare Network Simplex solutions with Gurobi reference solutions on DIMACS problems.
/// </summary>
public class DimacsGurobiComparisonTests
{
    private readonly ITestOutputHelper _output;
    
    public DimacsGurobiComparisonTests(ITestOutputHelper output)
    {
        _output = output;
    }
    /// <summary>
    /// Gets Gurobi flows from the embedded solution.
    /// </summary>
    private static Dictionary<(int from, int to), long> GetGurobiFlows(SolutionLoader.Solution solution)
    {
        // The solution already has flows in endpoint format
        return solution.ArcFlowsByEndpoints;
    }
    
    /// <summary>
    /// Helper method to validate a solution using SolutionValidator.
    /// </summary>
    private static void ValidateSolution(IGraph graph, NetworkSimplex solver)
    {
        var validator = new SolutionValidator(graph, solver);
        var result = validator.Validate();
        
        Assert.True(result.IsValid, 
            result.Errors.Count > 0 ? string.Join("; ", result.Errors) : "Solution should be valid");
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }
    
    [Fact]
    public void NetgenProblem_8_08a_MatchesGurobiSolution()
    {
        NetgenProblem_8_08a_MatchesGurobiSolution_WithDetailedTiming();
    }
    
    [Fact]
    public void NetgenProblem_8_08a_TimingAnalysis()
    {
        // Load problem from embedded resources
        var problem = StandardProblems.Dimacs.Netgen8_08a;
        var graph = problem.Graph;
        
        _output.WriteLine($"Problem statistics:");
        _output.WriteLine($"  Nodes: {problem.NodeCount:N0}");
        _output.WriteLine($"  Arcs: {problem.ArcCount:N0}");
        
        // Run multiple iterations to get timing statistics
        const int iterations = 10;
        var solveTimings = new List<double>();
        
        _output.WriteLine($"\nRunning {iterations} iterations for timing analysis:");
        
        for (int iter = 0; iter < iterations; iter++)
        {
            var solver = new NetworkSimplex(graph);
            
            // Set node supplies
            for (int i = 0; i < problem.NodeCount; i++)
            {
                if (problem.NodeSupplies[i] != 0)
                {
                    solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
                }
            }
            
            // Set arc bounds and costs
            for (int i = 0; i < problem.ArcCount; i++)
            {
                solver.SetArcBounds(new Arc(i), problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
                solver.SetArcCost(new Arc(i), problem.ArcCosts[i]);
            }
            
            var sw = Stopwatch.StartNew();
            var status = solver.Solve();
            sw.Stop();
            
            Assert.Equal(SolverStatus.Optimal, status);
            
            solveTimings.Add(sw.Elapsed.TotalMilliseconds);
            _output.WriteLine($"  Iteration {iter + 1}: {sw.ElapsedMilliseconds:N0} ms");
        }
        
        _output.WriteLine($"\nTiming Statistics:");
        _output.WriteLine($"  Average solve time: {solveTimings.Average():F2} ms");
        _output.WriteLine($"  Min solve time: {solveTimings.Min():F2} ms");
        _output.WriteLine($"  Max solve time: {solveTimings.Max():F2} ms");
        _output.WriteLine($"  Std deviation: {Math.Sqrt(solveTimings.Select(x => Math.Pow(x - solveTimings.Average(), 2)).Average()):F2} ms");
    }
    
    [Fact]
    public void NetgenProblem_8_13a_LargeScale_MatchesGurobiSolution()
    {
        // Measure reading time
        var totalStopwatch = Stopwatch.StartNew();
        var readStopwatch = Stopwatch.StartNew();
        
        // Load problem from embedded resources
        var problem = StandardProblems.Dimacs.Netgen8_13a;
        var graph = problem.Graph;
        readStopwatch.Stop();
        
        // Get Gurobi solution
        var gurobiSolution = StandardProblems.Solutions.Netgen8_13a;
        Assert.NotNull(gurobiSolution);
        Assert.Equal("Gurobi", gurobiSolution.Source);
        
        _output.WriteLine($"Large-scale problem statistics:");
        _output.WriteLine($"  Nodes: {problem.NodeCount:N0}");
        _output.WriteLine($"  Arcs: {problem.ArcCount:N0}");
        _output.WriteLine($"  Read time: {readStopwatch.ElapsedMilliseconds:N0} ms");
        
        // Measure setup time
        var setupStopwatch = Stopwatch.StartNew();
        
        // Create and configure solver
        var solver = new NetworkSimplex(graph);
        
        // Set node supplies
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (problem.NodeSupplies[i] != 0)
            {
                solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
            }
        }
        
        // Set arc bounds and costs
        for (int i = 0; i < problem.ArcCount; i++)
        {
            solver.SetArcBounds(new Arc(i), problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
            solver.SetArcCost(new Arc(i), problem.ArcCosts[i]);
        }
        setupStopwatch.Stop();
        
        _output.WriteLine($"  Setup time: {setupStopwatch.ElapsedMilliseconds:N0} ms");
        
        // Check supply balance
        long totalSupply = 0;
        int supplyNodes = 0;
        int demandNodes = 0;
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (problem.NodeSupplies[i] > 0)
            {
                supplyNodes++;
                totalSupply += problem.NodeSupplies[i];
            }
            else if (problem.NodeSupplies[i] < 0)
            {
                demandNodes++;
                totalSupply += problem.NodeSupplies[i];
            }
        }
        _output.WriteLine($"  Supply nodes: {supplyNodes}");
        _output.WriteLine($"  Demand nodes: {demandNodes}");
        _output.WriteLine($"  Total supply balance: {totalSupply}");
        
        // Measure solve time
        var solveStopwatch = Stopwatch.StartNew();
        var status = solver.Solve();
        solveStopwatch.Stop();
        
        _output.WriteLine($"  Solve time: {solveStopwatch.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Solver status: {status}");
        
        Assert.Equal(SolverStatus.Optimal, status);
        
        // Validate the solution
        ValidateSolution(graph, solver);
        
        // Get Gurobi flows from solution
        var gurobiFlows = GetGurobiFlows(gurobiSolution);
        
        // Compare flows (only check non-zero flows due to size)
        var ourFlows = new Dictionary<(int from, int to), long>();
        int nonZeroFlowCount = 0;
        for (int arcId = 0; arcId < graph.ArcCount; arcId++)
        {
            var arc = new Arc(arcId);
            var flow = solver.GetFlow(arc);
            if (flow > 0)
            {
                nonZeroFlowCount++;
                var from = graph.Source(arc).Id;
                var to = graph.Target(arc).Id;
                ourFlows[(from, to)] = flow;
            }
        }
        
        _output.WriteLine($"  Non-zero flows: {nonZeroFlowCount:N0}");
        
        // Verify all Gurobi flows are present in our solution
        foreach (var (arc, gurobiFlow) in gurobiFlows)
        {
            Assert.True(ourFlows.TryGetValue(arc, out var ourFlow), 
                $"Arc {arc.from + 1} -> {arc.to + 1} with flow {gurobiFlow} not found in our solution");
            Assert.Equal(gurobiFlow, ourFlow);
        }
        
        // Verify we don't have extra flows that Gurobi doesn't have
        foreach (var (arc, ourFlow) in ourFlows)
        {
            Assert.True(gurobiFlows.TryGetValue(arc, out var gurobiFlow), 
                $"Our solution has flow {ourFlow} on arc {arc.from + 1} -> {arc.to + 1} but Gurobi doesn't");
            Assert.Equal(gurobiFlow, ourFlow);
        }
        
        // Verify objective values match
        var ourObjective = solver.GetTotalCost();
        
        totalStopwatch.Stop();
        
        _output.WriteLine($"  Objective value: {ourObjective:N0}");
        _output.WriteLine($"  Total time: {totalStopwatch.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Performance: {(double)problem.ArcCount / solveStopwatch.ElapsedMilliseconds:N0} arcs/ms");
    }
    
    [Fact]
    public void NetgenProblem_8_13a_LargeScale_TimingAnalysis()
    {
        // Load problem from embedded resources
        var problem = StandardProblems.Dimacs.Netgen8_13a;
        var graph = problem.Graph;
        
        _output.WriteLine($"Large-scale problem statistics:");
        _output.WriteLine($"  Nodes: {problem.NodeCount:N0}");
        _output.WriteLine($"  Arcs: {problem.ArcCount:N0}");
        
        // Run multiple iterations to get timing statistics
        const int iterations = 5; // Fewer iterations for large problem
        var solveTimings = new List<double>();
        
        _output.WriteLine($"\nRunning {iterations} iterations for timing analysis:");
        
        for (int iter = 0; iter < iterations; iter++)
        {
            var solver = new NetworkSimplex(graph);
            
            // Set node supplies
            for (int i = 0; i < problem.NodeCount; i++)
            {
                if (problem.NodeSupplies[i] != 0)
                {
                    solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
                }
            }
            
            // Set arc bounds and costs
            for (int i = 0; i < problem.ArcCount; i++)
            {
                solver.SetArcBounds(new Arc(i), problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
                solver.SetArcCost(new Arc(i), problem.ArcCosts[i]);
            }
            
            var sw = Stopwatch.StartNew();
            var status = solver.Solve();
            sw.Stop();
            
            Assert.Equal(SolverStatus.Optimal, status);
            
            solveTimings.Add(sw.Elapsed.TotalMilliseconds);
            _output.WriteLine($"  Iteration {iter + 1}: {sw.ElapsedMilliseconds:N0} ms");
        }
        
        _output.WriteLine($"\nTiming Statistics:");
        _output.WriteLine($"  Average solve time: {solveTimings.Average():F2} ms");
        _output.WriteLine($"  Min solve time: {solveTimings.Min():F2} ms");
        _output.WriteLine($"  Max solve time: {solveTimings.Max():F2} ms");
        _output.WriteLine($"  Std deviation: {Math.Sqrt(solveTimings.Select(x => Math.Pow(x - solveTimings.Average(), 2)).Average()):F2} ms");
        _output.WriteLine($"  Average performance: {(double)problem.ArcCount / solveTimings.Average():F0} arcs/ms");
    }
    
    [Fact]
    public void NetgenProblem_8_14a_VeryLargeScale_Performance()
    {
        // Measure reading time
        var totalStopwatch = Stopwatch.StartNew();
        var readStopwatch = Stopwatch.StartNew();
        
        // Load problem from embedded resources
        var problem = StandardProblems.Dimacs.Netgen8_14a;
        var graph = problem.Graph;
        readStopwatch.Stop();
        
        _output.WriteLine($"Very large-scale problem statistics (netgen_8_14a):");
        _output.WriteLine($"  Nodes: {problem.NodeCount:N0}");
        _output.WriteLine($"  Arcs: {problem.ArcCount:N0}");
        _output.WriteLine($"  Read time: {readStopwatch.ElapsedMilliseconds:N0} ms");
        
        // Measure setup time
        var setupStopwatch = Stopwatch.StartNew();
        
        // Create and configure solver
        var solver = new NetworkSimplex(graph);
        
        // Set node supplies
        int supplyNodes = 0;
        int demandNodes = 0;
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (problem.NodeSupplies[i] != 0)
            {
                solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
                if (problem.NodeSupplies[i] > 0) supplyNodes++;
                else demandNodes++;
            }
        }
        
        // Set arc bounds and costs
        for (int i = 0; i < problem.ArcCount; i++)
        {
            solver.SetArcBounds(new Arc(i), problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
            solver.SetArcCost(new Arc(i), problem.ArcCosts[i]);
        }
        setupStopwatch.Stop();
        
        _output.WriteLine($"  Supply nodes: {supplyNodes}");
        _output.WriteLine($"  Demand nodes: {demandNodes}");
        _output.WriteLine($"  Setup time: {setupStopwatch.ElapsedMilliseconds:N0} ms");
        
        // Measure solve time
        var solveStopwatch = Stopwatch.StartNew();
        var status = solver.Solve();
        solveStopwatch.Stop();
        
        _output.WriteLine($"  Solve time: {solveStopwatch.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Solver status: {status}");
        
        Assert.Equal(SolverStatus.Optimal, status);
        
        // Validate the solution
        ValidateSolution(graph, solver);
        
        // Get objective value
        var objective = solver.GetTotalCost();
        
        totalStopwatch.Stop();
        
        _output.WriteLine($"  Objective value: {objective:N0}");
        _output.WriteLine($"  Total time: {totalStopwatch.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Performance: {(double)problem.ArcCount / solveStopwatch.ElapsedMilliseconds:F0} arcs/ms");
    }
    
    [Fact]
    public void NetgenProblem_8_15a_ExtraLargeScale_Performance()
    {
        // Measure reading time
        var totalStopwatch = Stopwatch.StartNew();
        var readStopwatch = Stopwatch.StartNew();
        
        // Load problem from embedded resources
        var problem = StandardProblems.Dimacs.Netgen8_15a;
        var graph = problem.Graph;
        readStopwatch.Stop();
        
        _output.WriteLine($"Extra large-scale problem statistics (netgen_8_15a):");
        _output.WriteLine($"  Nodes: {problem.NodeCount:N0}");
        _output.WriteLine($"  Arcs: {problem.ArcCount:N0}");
        _output.WriteLine($"  Read time: {readStopwatch.ElapsedMilliseconds:N0} ms");
        
        // Measure setup time
        var setupStopwatch = Stopwatch.StartNew();
        
        // Create and configure solver
        var solver = new NetworkSimplex(graph);
        
        // Set node supplies
        int supplyNodes = 0;
        int demandNodes = 0;
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (problem.NodeSupplies[i] != 0)
            {
                solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
                if (problem.NodeSupplies[i] > 0) supplyNodes++;
                else demandNodes++;
            }
        }
        
        // Set arc bounds and costs
        for (int i = 0; i < problem.ArcCount; i++)
        {
            solver.SetArcBounds(new Arc(i), problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
            solver.SetArcCost(new Arc(i), problem.ArcCosts[i]);
        }
        setupStopwatch.Stop();
        
        _output.WriteLine($"  Supply nodes: {supplyNodes}");
        _output.WriteLine($"  Demand nodes: {demandNodes}");
        _output.WriteLine($"  Setup time: {setupStopwatch.ElapsedMilliseconds:N0} ms");
        
        // Measure solve time
        var solveStopwatch = Stopwatch.StartNew();
        var status = solver.Solve();
        solveStopwatch.Stop();
        
        _output.WriteLine($"  Solve time: {solveStopwatch.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Solver status: {status}");
        
        Assert.Equal(SolverStatus.Optimal, status);
        
        // Validate the solution
        ValidateSolution(graph, solver);
        
        // Get objective value
        var objective = solver.GetTotalCost();
        
        totalStopwatch.Stop();
        
        _output.WriteLine($"  Objective value: {objective:N0}");
        _output.WriteLine($"  Total time: {totalStopwatch.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Performance: {(double)problem.ArcCount / solveStopwatch.ElapsedMilliseconds:F0} arcs/ms");
    }
    
    private void NetgenProblem_8_08a_MatchesGurobiSolution_WithDetailedTiming()
    {
        // Measure reading time
        var totalStopwatch = Stopwatch.StartNew();
        var readStopwatch = Stopwatch.StartNew();
        
        // Load problem from embedded resources
        var problem = StandardProblems.Dimacs.Netgen8_08a;
        var graph = problem.Graph;
        readStopwatch.Stop();
        
        // Get Gurobi solution
        var gurobiSolution = StandardProblems.Solutions.Netgen8_08a;
        Assert.NotNull(gurobiSolution);
        Assert.Equal("Gurobi", gurobiSolution.Source);
        
        _output.WriteLine($"Problem statistics:");
        _output.WriteLine($"  Nodes: {problem.NodeCount:N0}");
        _output.WriteLine($"  Arcs: {problem.ArcCount:N0}");
        _output.WriteLine($"  Read time: {readStopwatch.ElapsedMilliseconds:N0} ms");
        
        // Measure setup time
        var setupStopwatch = Stopwatch.StartNew();
        
        // Create and configure solver
        var solver = new NetworkSimplex(graph);
        
        // Set node supplies
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (problem.NodeSupplies[i] != 0)
            {
                solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
            }
        }
        
        // Set arc bounds and costs
        for (int i = 0; i < problem.ArcCount; i++)
        {
            solver.SetArcBounds(new Arc(i), problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
            solver.SetArcCost(new Arc(i), problem.ArcCosts[i]);
        }
        setupStopwatch.Stop();
        
        _output.WriteLine($"  Setup time: {setupStopwatch.ElapsedMilliseconds:N0} ms");
        
        // Check supply balance
        long totalSupply = 0;
        int supplyNodes = 0;
        int demandNodes = 0;
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (problem.NodeSupplies[i] > 0)
            {
                supplyNodes++;
                totalSupply += problem.NodeSupplies[i];
            }
            else if (problem.NodeSupplies[i] < 0)
            {
                demandNodes++;
                totalSupply += problem.NodeSupplies[i];
            }
        }
        _output.WriteLine($"  Supply nodes: {supplyNodes}");
        _output.WriteLine($"  Demand nodes: {demandNodes}");
        _output.WriteLine($"  Total supply balance: {totalSupply}");
        
        // Measure solve time
        var solveStopwatch = Stopwatch.StartNew();
        var status = solver.Solve();
        solveStopwatch.Stop();
        
        _output.WriteLine($"  Solve time: {solveStopwatch.ElapsedMilliseconds:N0} ms");
        _output.WriteLine($"  Solver status: {status}");
        
        Assert.Equal(SolverStatus.Optimal, status);
        
        // Validate the solution
        ValidateSolution(graph, solver);
        
        // Get Gurobi flows from solution
        var gurobiFlows = GetGurobiFlows(gurobiSolution);
        
        // Compare flows
        var ourFlows = new Dictionary<(int from, int to), long>();
        for (int arcId = 0; arcId < graph.ArcCount; arcId++)
        {
            var arc = new Arc(arcId);
            var flow = solver.GetFlow(arc);
            if (flow > 0)
            {
                var from = graph.Source(arc).Id;
                var to = graph.Target(arc).Id;
                ourFlows[(from, to)] = flow;
            }
        }
        
        // Verify all Gurobi flows are present in our solution
        foreach (var (arc, gurobiFlow) in gurobiFlows)
        {
            Assert.True(ourFlows.TryGetValue(arc, out var ourFlow), 
                $"Arc {arc.from + 1} -> {arc.to + 1} with flow {gurobiFlow} not found in our solution");
            Assert.Equal(gurobiFlow, ourFlow);
        }
        
        // Verify we don't have extra flows that Gurobi doesn't have
        foreach (var (arc, ourFlow) in ourFlows)
        {
            Assert.True(gurobiFlows.TryGetValue(arc, out var gurobiFlow), 
                $"Our solution has flow {ourFlow} on arc {arc.from + 1} -> {arc.to + 1} but Gurobi doesn't");
            Assert.Equal(gurobiFlow, ourFlow);
        }
        
        // Verify objective values match
        var ourObjective = solver.GetTotalCost();
        
        totalStopwatch.Stop();
        
        _output.WriteLine($"  Objective value: {ourObjective:N0}");
        _output.WriteLine($"  Gurobi objective: {gurobiSolution.OptimalCost:N0}");
        _output.WriteLine($"  Total time: {totalStopwatch.ElapsedMilliseconds:N0} ms");
        
        // Verify objective values match exactly
        Assert.Equal(gurobiSolution.OptimalCost, ourObjective);
    }
}