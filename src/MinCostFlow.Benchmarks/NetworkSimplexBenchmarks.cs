using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
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
    public class NetworkSimplexBenchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddColumn(StatisticColumn.Mean);
                AddColumn(StatisticColumn.StdDev);
                AddColumn(StatisticColumn.Min);
                AddColumn(StatisticColumn.Max);
                AddColumn(StatisticColumn.Iterations);
                
                // Single job with minimal iterations for all problems
                AddJob(Job.ShortRun
                    .WithLaunchCount(1)
                    .WithWarmupCount(1)
                    .WithIterationCount(3)
                    .WithInvocationCount(1)
                    .WithUnrollFactor(1));
            }
        }

        private readonly ProblemRepository _repository = new();
        private readonly Dictionary<string, MinCostFlowProblem> _problemCache = new();
        
        // Lists of problems by category for benchmarking
        private List<MinCostFlowProblem> _smallProblems = new();
        private List<MinCostFlowProblem> _mediumProblems = new();
        private List<MinCostFlowProblem> _largeProblems = new();
        private List<MinCostFlowProblem> _generatedProblems = new();

        [GlobalSetup]
        public void Setup()
        {
            // Load all problems and cache them
            LoadAllProblems();
        }

        private void LoadAllProblems()
        {
            Console.WriteLine("Loading problems for benchmarking...");
            
            // Load embedded problems dynamically
            var allProblemsByCategory = StandardProblems.GetAllByCategory();
            
            // Categorize problems
            if (allProblemsByCategory.TryGetValue("Small", out var small))
            {
                _smallProblems = small;
                Console.WriteLine($"Loaded {_smallProblems.Count} small problems");
            }
            
            if (allProblemsByCategory.TryGetValue("Medium", out var medium))
            {
                _mediumProblems = medium;
                Console.WriteLine($"Loaded {_mediumProblems.Count} medium problems");
            }
            
            if (allProblemsByCategory.TryGetValue("Large", out var large))
            {
                _largeProblems = large;
                Console.WriteLine($"Loaded {_largeProblems.Count} large problems");
            }
            
            // Also include DIMACS problems in appropriate categories
            if (allProblemsByCategory.TryGetValue("DIMACS", out var dimacs))
            {
                foreach (var problem in dimacs)
                {
                    if (problem.NodeCount <= 1000)
                        _mediumProblems.Add(problem);
                    else
                        _largeProblems.Add(problem);
                }
            }
            
            // Load generated problems
            LoadGeneratedProblems();
            
            // Cache all problems for benchmark methods
            CacheProblemsForBenchmarks();
        }

        private void LoadGeneratedProblems()
        {
            // Generate a few standard problems for benchmarking
            Console.WriteLine("Generating standard benchmark problems...");
            
            // Add some standard transportation problems
            var transportSizes = new[] { (10, 10), (50, 50), (100, 100) };
            foreach (var (sources, sinks) in transportSizes)
            {
                var problem = _repository.GenerateTransportationProblem(
                    sources: sources,
                    sinks: sinks,
                    supply: 1000 * Math.Max(sources, sinks));
                    
                if (problem.Metadata == null)
                    problem.Metadata = new ProblemMetadata();
                problem.Metadata.Name = $"Transport_{sources}x{sinks}_Gen";
                
                _generatedProblems.Add(problem);
                
                // Add to appropriate category
                var totalNodes = sources + sinks;
                if (totalNodes <= 100)
                    _mediumProblems.Add(problem);
                else
                    _largeProblems.Add(problem);
            }
            
            // Add some circulation problems
            foreach (var size in new[] { 1000, 5000 })
            {
                var problem = _repository.GenerateCirculationProblem(
                    nodes: size,
                    density: 0.05);
                    
                if (problem.Metadata == null)
                    problem.Metadata = new ProblemMetadata();
                problem.Metadata.Name = $"Circulation_{size}_Gen";
                
                _generatedProblems.Add(problem);
                _largeProblems.Add(problem);
            }
            
            Console.WriteLine($"Generated {_generatedProblems.Count} problems");
        }

        private void CacheProblemsForBenchmarks()
        {
            // Cache all problems with their names for benchmark access
            foreach (var problem in _smallProblems.Concat(_mediumProblems).Concat(_largeProblems))
            {
                var name = problem.Metadata?.Name ?? "Unknown";
                _problemCache[name] = problem;
            }
        }


        private SolverStatus SolveProblem(MinCostFlowProblem problem)
        {
            var solver = new NetworkSimplex(problem.Graph);
            
            // Set supplies
            for (int i = 0; i < problem.NodeCount; i++)
            {
                solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
            }
            
            // Set arc data
            for (int i = 0; i < problem.ArcCount; i++)
            {
                var arc = new Arc(i);
                solver.SetArcCost(arc, problem.ArcCosts[i]);
                solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
            }
            
            var status = solver.Solve();
            
            // Don't record here - BenchmarkDotNet runs in isolation
            
            return status;
        }

        // Benchmark methods - dynamically run all loaded problems
        [Benchmark]
        [BenchmarkCategory("Small")]
        [ArgumentsSource(nameof(SmallProblemNames))]
        public SolverStatus BenchmarkSmallProblem(string problemName)
        {
            return SolveProblem(_problemCache[problemName]);
        }
        
        [Benchmark] 
        [BenchmarkCategory("Medium")]
        [ArgumentsSource(nameof(MediumProblemNames))]
        public SolverStatus BenchmarkMediumProblem(string problemName)
        {
            return SolveProblem(_problemCache[problemName]);
        }
        
        [Benchmark]
        [BenchmarkCategory("Large")]
        [ArgumentsSource(nameof(LargeProblemNames))]
        public SolverStatus BenchmarkLargeProblem(string problemName)
        {
            return SolveProblem(_problemCache[problemName]);
        }
        
        // Provide problem names for benchmarking
        public IEnumerable<string> SmallProblemNames()
        {
            foreach (var problem in _smallProblems)
            {
                yield return problem.Metadata?.Name ?? "Unknown";
            }
        }
        
        public IEnumerable<string> MediumProblemNames()
        {
            foreach (var problem in _mediumProblems)
            {
                yield return problem.Metadata?.Name ?? "Unknown";
            }
        }
        
        public IEnumerable<string> LargeProblemNames()
        {
            foreach (var problem in _largeProblems)
            {
                yield return problem.Metadata?.Name ?? "Unknown";
            }
        }

    }
}