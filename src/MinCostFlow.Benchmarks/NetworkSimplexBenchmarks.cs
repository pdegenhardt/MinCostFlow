using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.IO;
using MinCostFlow.Core.Types;

namespace MinCostFlow.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80)]
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
            }
        }

        private readonly List<BenchmarkProblem> _problems = new();

        public class BenchmarkProblem
        {
            public string Name { get; set; } = null!;
            public IGraph Graph { get; set; } = null!;
            public long[] Supplies { get; set; } = null!;
            public long[] Costs { get; set; } = null!;
            public long[] LowerBounds { get; set; } = null!;
            public long[] UpperBounds { get; set; } = null!;
            public int NodeCount { get; set; }
            public int ArcCount { get; set; }
            public string? FilePath { get; set; }
        }

        [GlobalSetup]
        public void Setup()
        {
            // First, add generated problems of various sizes
            AddGeneratedProblems();

            // Then, add DIMACS problems if available
            AddDimacsProblemFiles();
        }

        private void AddGeneratedProblems()
        {
            // Generate transportation problems of various sizes
            var sizes = new[] { 100, 500, 1000, 5000, 10000 };
            
            foreach (var size in sizes)
            {
                // Create balanced sources/sinks for reasonable arc count
                int sources = (int)Math.Sqrt(size);
                int sinks = sources;
                var problem = GenerateTransportationProblem(sources, sinks);
                problem.Name = $"Transport_{size}";
                _problems.Add(problem);
            }

            // Generate circulation problems
            foreach (var size in new[] { 1000, 5000, 10000 })
            {
                var problem = GenerateCirculationProblem(size);
                problem.Name = $"Circulation_{size}";
                _problems.Add(problem);
            }
            
            // Add a true 10,000 node sparse transport problem
            var largeProblem = GenerateSparseTransportProblem(5000, 5000);
            largeProblem.Name = "Transport_10000_sparse";
            _problems.Add(largeProblem);
            
            // Add a simple valid 10,000 node problem for testing
            var simpleLarge = GenerateSimpleLargeProblem(10000);
            simpleLarge.Name = "Simple_10000";
            _problems.Add(simpleLarge);
        }

        private void AddDimacsProblemFiles()
        {
            var benchmarkDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dimacs");
            if (!Directory.Exists(benchmarkDir))
            {
                Console.WriteLine($"DIMACS benchmark directory not found: {benchmarkDir}");
                return;
            }

            var dimacsFiles = Directory.GetFiles(benchmarkDir, "*.min", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(benchmarkDir, "*.dmx", SearchOption.AllDirectories))
                .ToList();

            foreach (var file in dimacsFiles)
            {
                try
                {
                    var dimacsProblem = DimacsReader.ReadFromFile(file);
                    var problem = new BenchmarkProblem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Graph = dimacsProblem.Graph,
                        Supplies = dimacsProblem.NodeSupplies,
                        Costs = dimacsProblem.ArcCosts,
                        LowerBounds = dimacsProblem.ArcLowerBounds,
                        UpperBounds = dimacsProblem.ArcUpperBounds,
                        NodeCount = dimacsProblem.NodeCount,
                        ArcCount = dimacsProblem.ArcCount,
                        FilePath = file
                    };
                    _problems.Add(problem);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load DIMACS file {file}: {ex.Message}");
                }
            }
        }

        private BenchmarkProblem GenerateTransportationProblem(int sources, int sinks)
        {
            var builder = new GraphBuilder();
            
            // Add source nodes
            for (int i = 0; i < sources; i++)
            {
                builder.AddNode(i);
            }
            
            // Add sink nodes
            for (int i = 0; i < sinks; i++)
            {
                builder.AddNode(sources + i);
            }

            var random = new Random(42); // Fixed seed for reproducibility

            // Connect every source to every sink
            for (int i = 0; i < sources; i++)
            {
                for (int j = 0; j < sinks; j++)
                {
                    builder.AddArc(i, sources + j);
                }
            }

            var graph = builder.Build();
            var totalNodes = sources + sinks;
            var totalArcs = sources * sinks;

            // Generate supplies and demands
            var supplies = new long[totalNodes];
            long totalSupply = 0;
            
            // Sources have positive supply
            for (int i = 0; i < sources; i++)
            {
                supplies[i] = random.Next(10, 100);
                totalSupply += supplies[i];
            }
            
            // Distribute demand evenly among sinks
            long demandPerSink = totalSupply / sinks;
            long remainder = totalSupply % sinks;
            
            for (int i = 0; i < sinks; i++)
            {
                supplies[sources + i] = -demandPerSink;
                if (i < remainder)
                    supplies[sources + i]--;
            }
            
            // Verify supply balance
            long checkSum = 0;
            for (int i = 0; i < totalNodes; i++)
            {
                checkSum += supplies[i];
            }
            if (checkSum != 0)
            {
                throw new InvalidOperationException($"Supply not balanced: {checkSum}");
            }

            // Generate costs and capacities
            var costs = new long[totalArcs];
            var lowerBounds = new long[totalArcs];
            var upperBounds = new long[totalArcs];
            
            for (int i = 0; i < totalArcs; i++)
            {
                costs[i] = random.Next(1, 100);
                lowerBounds[i] = 0;
                upperBounds[i] = random.Next(50, 200);
            }

            return new BenchmarkProblem
            {
                Graph = graph,
                Supplies = supplies,
                Costs = costs,
                LowerBounds = lowerBounds,
                UpperBounds = upperBounds,
                NodeCount = totalNodes,
                ArcCount = totalArcs
            };
        }

        private BenchmarkProblem GenerateSparseTransportProblem(int sources, int sinks)
        {
            var builder = new GraphBuilder();
            
            // Add source nodes
            for (int i = 0; i < sources; i++)
            {
                builder.AddNode(i);
            }
            
            // Add sink nodes
            for (int i = 0; i < sinks; i++)
            {
                builder.AddNode(sources + i);
            }

            var random = new Random(42);
            int arcCount = 0;
            
            // Create sparse connections - each source connects to ~20 sinks
            // Ensure every sink is connected to at least one source
            var sinkConnections = new HashSet<int>[sinks];
            for (int i = 0; i < sinks; i++)
            {
                sinkConnections[i] = new HashSet<int>();
            }
            
            int connectionsPerSource = Math.Min(20, sinks);
            for (int i = 0; i < sources; i++)
            {
                var selectedSinks = new HashSet<int>();
                
                // First, connect to a sink that has no connections yet (if any)
                for (int j = 0; j < sinks && selectedSinks.Count < connectionsPerSource; j++)
                {
                    if (sinkConnections[j].Count == 0)
                    {
                        selectedSinks.Add(j);
                        sinkConnections[j].Add(i);
                    }
                }
                
                // Then add random connections
                while (selectedSinks.Count < connectionsPerSource)
                {
                    int j = random.Next(sinks);
                    if (selectedSinks.Add(j))
                    {
                        sinkConnections[j].Add(i);
                    }
                }
                
                foreach (var j in selectedSinks)
                {
                    builder.AddArc(i, sources + j);
                    arcCount++;
                }
            }

            var graph = builder.Build();
            var totalNodes = sources + sinks;

            // Generate supplies and demands
            var supplies = new long[totalNodes];
            long totalSupply = 0;
            
            // Sources have positive supply
            for (int i = 0; i < sources; i++)
            {
                supplies[i] = random.Next(10, 100);
                totalSupply += supplies[i];
            }
            
            // Distribute demand evenly among sinks
            long demandPerSink = totalSupply / sinks;
            long remainder = totalSupply % sinks;
            
            for (int i = 0; i < sinks; i++)
            {
                supplies[sources + i] = -demandPerSink;
                if (i < remainder)
                    supplies[sources + i]--;
            }

            // Generate costs and capacities
            var costs = new long[arcCount];
            var lowerBounds = new long[arcCount];
            var upperBounds = new long[arcCount];
            
            for (int i = 0; i < arcCount; i++)
            {
                costs[i] = random.Next(1, 100);
                lowerBounds[i] = 0;
                // Ensure capacities are large enough to accommodate flow
                upperBounds[i] = random.Next(1000, 5000);
            }

            return new BenchmarkProblem
            {
                Graph = graph,
                Supplies = supplies,
                Costs = costs,
                LowerBounds = lowerBounds,
                UpperBounds = upperBounds,
                NodeCount = totalNodes,
                ArcCount = arcCount
            };
        }

        private BenchmarkProblem GenerateCirculationProblem(int nodeCount)
        {
            var builder = new GraphBuilder();
            
            // Add nodes
            for (int i = 0; i < nodeCount; i++)
            {
                builder.AddNode(i);
            }

            var random = new Random(42);
            
            // Create a connected graph with ~3 arcs per node
            int targetArcCount = nodeCount * 3;
            int arcCount = 0;
            
            // First, create a cycle to ensure connectivity
            for (int i = 0; i < nodeCount; i++)
            {
                builder.AddArc(i, (i + 1) % nodeCount);
                arcCount++;
            }
            
            // Add random arcs (avoid duplicates)
            var existingArcs = new HashSet<(int, int)>();
            for (int i = 0; i < nodeCount; i++)
            {
                existingArcs.Add((i, (i + 1) % nodeCount));
            }
            
            int attempts = 0;
            while (arcCount < targetArcCount && attempts < targetArcCount * 10)
            {
                int from = random.Next(nodeCount);
                int to = random.Next(nodeCount);
                attempts++;
                
                if (from != to && existingArcs.Add((from, to)))
                {
                    builder.AddArc(from, to);
                    arcCount++;
                }
            }

            var graph = builder.Build();

            // Circulation has zero supplies
            var supplies = new long[nodeCount];
            
            // Generate costs (including negative costs for interesting cycles)
            var costs = new long[arcCount];
            var lowerBounds = new long[arcCount];
            var upperBounds = new long[arcCount];
            
            for (int i = 0; i < arcCount; i++)
            {
                costs[i] = random.Next(-20, 100);
                lowerBounds[i] = 0;
                upperBounds[i] = random.Next(10, 100);
            }

            return new BenchmarkProblem
            {
                Graph = graph,
                Supplies = supplies,
                Costs = costs,
                LowerBounds = lowerBounds,
                UpperBounds = upperBounds,
                NodeCount = nodeCount,
                ArcCount = arcCount
            };
        }

        private BenchmarkProblem GenerateSimpleLargeProblem(int nodeCount)
        {
            var builder = new GraphBuilder();
            
            // Create a simple path graph with some additional arcs
            for (int i = 0; i < nodeCount; i++)
            {
                builder.AddNode(i);
            }
            
            // Create a path
            for (int i = 0; i < nodeCount - 1; i++)
            {
                builder.AddArc(i, i + 1);
            }
            
            // Add some backward arcs for cycles
            var random = new Random(42);
            int additionalArcs = nodeCount / 2;
            for (int i = 0; i < additionalArcs; i++)
            {
                int from = random.Next(nodeCount / 2, nodeCount);
                int to = random.Next(0, from);
                builder.AddArc(from, to);
            }
            
            var graph = builder.Build();
            int arcCount = nodeCount - 1 + additionalArcs;
            
            // Simple supply/demand: first node supplies, last node demands
            var supplies = new long[nodeCount];
            supplies[0] = 1000;
            supplies[nodeCount - 1] = -1000;
            
            // Simple costs and large capacities
            var costs = new long[arcCount];
            var lowerBounds = new long[arcCount];
            var upperBounds = new long[arcCount];
            
            for (int i = 0; i < arcCount; i++)
            {
                costs[i] = i < nodeCount - 1 ? 1 : 2; // Forward arcs cheaper
                lowerBounds[i] = 0;
                upperBounds[i] = 10000; // Large capacity
            }
            
            return new BenchmarkProblem
            {
                Graph = graph,
                Supplies = supplies,
                Costs = costs,
                LowerBounds = lowerBounds,
                UpperBounds = upperBounds,
                NodeCount = nodeCount,
                ArcCount = arcCount
            };
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetProblems))]
        public SolverStatus SolveNetworkSimplex(BenchmarkProblem problem)
        {
            var solver = new NetworkSimplex(problem.Graph);
            
            // Set supplies
            for (int i = 0; i < problem.NodeCount; i++)
            {
                solver.SetNodeSupply(new Node(i), problem.Supplies[i]);
            }
            
            // Set arc data
            for (int i = 0; i < problem.ArcCount; i++)
            {
                var arc = new Arc(i);
                solver.SetArcCost(arc, problem.Costs[i]);
                solver.SetArcBounds(arc, problem.LowerBounds[i], problem.UpperBounds[i]);
            }
            
            return solver.Solve();
        }

        public IEnumerable<BenchmarkProblem> GetProblems()
        {
            return _problems;
        }

        [Benchmark]
        public void ValidatePerformanceTargets()
        {
            // Run the 10,000 node transport problem specifically
            var problem = _problems.FirstOrDefault(p => p.Name == "Transport_10000");
            if (problem == null)
            {
                throw new InvalidOperationException("10,000 node transport problem not found");
            }

            var solver = new NetworkSimplex(problem.Graph);
            
            // Set supplies
            for (int i = 0; i < problem.NodeCount; i++)
            {
                solver.SetNodeSupply(new Node(i), problem.Supplies[i]);
            }
            
            // Set arc data
            for (int i = 0; i < problem.ArcCount; i++)
            {
                var arc = new Arc(i);
                solver.SetArcCost(arc, problem.Costs[i]);
                solver.SetArcBounds(arc, problem.LowerBounds[i], problem.UpperBounds[i]);
            }
            
            var sw = Stopwatch.StartNew();
            var status = solver.Solve();
            sw.Stop();
            
            if (status != SolverStatus.Optimal)
            {
                throw new InvalidOperationException($"Solver returned {status} for transport problem");
            }
            
            Console.WriteLine($"10,000 node problem solved in {sw.ElapsedMilliseconds}ms");
            
            if (sw.ElapsedMilliseconds > 1000)
            {
                Console.WriteLine("WARNING: Performance target not met (> 1s for 10,000 nodes)");
            }
        }
    }
}