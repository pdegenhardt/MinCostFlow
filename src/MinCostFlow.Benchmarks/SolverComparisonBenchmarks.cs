using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using MinCostFlow.Benchmarks.Solvers;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;
using MinCostFlow.Problems;
using MinCostFlow.Problems.Models;
using MinCostFlow.Problems.Sets;

namespace MinCostFlow.Benchmarks
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class SolverComparisonBenchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddColumn(StatisticColumn.Mean);
                AddColumn(StatisticColumn.StdDev);
                AddColumn(StatisticColumn.Min);
                AddColumn(StatisticColumn.Max);
                // Remove RatioColumn as it's not in this version of BenchmarkDotNet
                AddColumn(StatisticColumn.Iterations);
                
                // Configure job for comparison
                AddJob(Job.ShortRun
                    .WithLaunchCount(1)
                    .WithWarmupCount(2)
                    .WithIterationCount(5)
                    .WithInvocationCount(1)
                    .WithUnrollFactor(1));
            }
        }

        public enum SolverType
        {
            NetworkSimplex,
            OrTools
        }

        [ParamsSource(nameof(GetProblemSolverPairs))]
        public (string ProblemName, MinCostFlowProblem Problem, SolverType Solver) TestCase { get; set; }

        private readonly Dictionary<string, MinCostFlowProblem> _problemCache = new();
        private readonly ProblemRepository _repository = new();

        [GlobalSetup]
        public void Setup()
        {
            LoadAllProblems();
        }

        private void LoadAllProblems()
        {
            // Load small problems
            _problemCache["Small_Path5"] = StandardProblems.Small.Path5Node;
            _problemCache["Small_Path10"] = StandardProblems.Small.Path10Node;
            _problemCache["Small_Grid2x2"] = StandardProblems.Small.Grid2x2;
            _problemCache["Small_Diamond"] = StandardProblems.Small.DiamondGraph;
            
            // Load DIMACS problems
            _problemCache["DIMACS_Netgen8_08a"] = StandardProblems.Dimacs.Netgen8_08a;
            _problemCache["DIMACS_Netgen8_10a"] = StandardProblems.Dimacs.Netgen8_10a;
            _problemCache["DIMACS_Netgen8_13a"] = StandardProblems.Dimacs.Netgen8_13a;
            _problemCache["DIMACS_Netgen8_14a"] = StandardProblems.Dimacs.Netgen8_14a;
            _problemCache["DIMACS_Netgen8_15a"] = StandardProblems.Dimacs.Netgen8_15a;
            
            // Generate transportation problems
            var transportSizes = new[] { 100, 500, 1000, 5000, 10000 };
            foreach (var size in transportSizes)
            {
                int sources = (int)Math.Sqrt(size);
                int sinks = sources;
                var problem = _repository.GenerateTransportationProblem(sources, sinks, 1000);
                _problemCache[$"Transport_{size}"] = problem;
            }
            
            // Generate circulation problems
            var circulationSizes = new[] { 1000, 5000, 6000 };
            foreach (var size in circulationSizes)
            {
                var problem = _repository.GenerateCirculationProblem(size, 0.05);
                _problemCache[$"Circulation_{size}"] = problem;
            }
            
            // Generate path and grid problems
            _problemCache["Path_10000"] = _repository.GeneratePathProblem(10000, 1000);
            _problemCache["Grid_100x100"] = _repository.GenerateGridProblem(100, 100, 0, 0, 99, 99, 1000);
        }

        public IEnumerable<(string, MinCostFlowProblem, SolverType)> GetProblemSolverPairs()
        {
            // Small problems - test both solvers
            var smallProblems = new[] { "Small_Path5", "Small_Path10", "Small_Grid2x2", "Small_Diamond" };
            foreach (var problemName in smallProblems)
            {
                if (_problemCache.TryGetValue(problemName, out var problem))
                {
                    yield return (problemName, problem, SolverType.NetworkSimplex);
                    yield return (problemName, problem, SolverType.OrTools);
                }
            }
            
            // Medium problems
            var mediumProblems = new[] { "Transport_100", "Transport_500", "Transport_1000", 
                                         "Circulation_1000", "DIMACS_Netgen8_08a", "DIMACS_Netgen8_10a" };
            foreach (var problemName in mediumProblems)
            {
                if (_problemCache.TryGetValue(problemName, out var problem))
                {
                    yield return (problemName, problem, SolverType.NetworkSimplex);
                    yield return (problemName, problem, SolverType.OrTools);
                }
            }
            
            // Large problems
            var largeProblems = new[] { "Transport_5000", "Transport_10000", "Circulation_5000", 
                                       "Path_10000", "Grid_100x100", "DIMACS_Netgen8_13a" };
            foreach (var problemName in largeProblems)
            {
                if (_problemCache.TryGetValue(problemName, out var problem))
                {
                    yield return (problemName, problem, SolverType.NetworkSimplex);
                    yield return (problemName, problem, SolverType.OrTools);
                }
            }
        }

        [Benchmark]
        public (SolverStatus status, long cost) SolveProblem()
        {
            var (_, problem, solverType) = TestCase;
            
            if (solverType == SolverType.NetworkSimplex)
            {
                var solver = new NetworkSimplex(problem.Graph);
                
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
                
                var status = solver.Solve();
                
                if (status == SolverStatus.Optimal)
                {
                    return (status, solver.GetTotalCost());
                }
                
                return (status, 0);
            }
            else // SolverType.OrTools
            {
                var solver = new OrToolsSolver(problem.Graph);
                
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
                
                var status = solver.Solve();
                
                if (status == SolverStatus.Optimal)
                {
                    return (status, solver.GetTotalCost());
                }
                
                return (status, 0);
            }
        }
    }
}