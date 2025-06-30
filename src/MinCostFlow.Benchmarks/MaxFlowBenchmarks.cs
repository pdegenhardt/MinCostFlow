using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using MinCostFlow.Core.Gort;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinCostFlow.Benchmarks;

/// <summary>
/// Performance benchmarks for the Generic Max Flow algorithm implementation.
/// Tests various graph configurations as specified in the max flow spec section 9.7.
/// </summary>
[Config(typeof(Config))]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
public class MaxFlowBenchmarks
{
    public class Config : ManualConfig
    {
        public Config()
        {
            AddLogger(DefaultConfig.Instance.GetLoggers().ToArray());
            AddExporter(DefaultConfig.Instance.GetExporters().ToArray());
            AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
        }
    }

    private readonly List<BenchmarkInstance> _instances = new();

    public MaxFlowBenchmarks()
    {
        // Initialize benchmark instances
        InitializeBenchmarks();
    }

    [GlobalSetup]
    public void Setup()
    {
        // Pre-build all graphs
        foreach (var instance in _instances)
        {
            instance.Prepare();
        }
    }

    #region Full Assignment Benchmarks

    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    [Arguments(2000)]
    public long FullAssignment(int size)
    {
        var instance = GetInstance("FullAssignment", size);
        return instance.Solve();
    }

    #endregion

    #region Partial Assignment Benchmarks

    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    [Arguments(5000)]
    public long PartialAssignment(int size)
    {
        var instance = GetInstance("PartialAssignment", size);
        return instance.Solve();
    }

    #endregion

    #region Random Flow Benchmarks

    [Benchmark]
    [Arguments(100, 0.1)]
    [Arguments(500, 0.1)]
    [Arguments(100, 0.5)]
    [Arguments(500, 0.5)]
    public long RandomFlow(int size, double density)
    {
        var instance = GetInstance($"RandomFlow_{density:F1}", size);
        return instance.Solve();
    }

    #endregion

    #region Grid Flow Benchmarks

    [Benchmark]
    [Arguments(10)]  // 10x10 grid = 100 nodes
    [Arguments(20)]  // 20x20 grid = 400 nodes
    [Arguments(30)]  // 30x30 grid = 900 nodes
    [Arguments(50)]  // 50x50 grid = 2500 nodes
    public long GridFlow(int gridSize)
    {
        var instance = GetInstance("GridFlow", gridSize * gridSize);
        return instance.Solve();
    }

    #endregion

    private BenchmarkInstance GetInstance(string type, int size)
    {
        var key = $"{type}_{size}";
        return _instances.Find(i => i.Key == key) 
            ?? throw new InvalidOperationException($"Instance {key} not found");
    }

    private void InitializeBenchmarks()
    {
        // Full Assignment: O(n²) arcs
        foreach (var size in new[] { 100, 500, 1000, 2000 })
        {
            _instances.Add(new FullAssignmentInstance(size));
        }

        // Partial Assignment: O(n) arcs
        foreach (var size in new[] { 100, 500, 1000, 5000 })
        {
            _instances.Add(new PartialAssignmentInstance(size));
        }

        // Random Flows: Variable density
        foreach (var size in new[] { 100, 500 })
        {
            foreach (var density in new[] { 0.1, 0.5 })
            {
                _instances.Add(new RandomFlowInstance(size, density));
            }
        }

        // Grid Flows
        foreach (var gridSize in new[] { 10, 20, 30, 50 })
        {
            _instances.Add(new GridFlowInstance(gridSize));
        }
    }

    #region Benchmark Instance Classes

    private abstract class BenchmarkInstance
    {
        public abstract string Key { get; }
        protected ReverseArcListGraph? Graph { get; set; }
        protected GenericMaxFlow<ReverseArcListGraph, int, long>? MaxFlow { get; set; }

        public abstract void Prepare();

        public long Solve()
        {
            if (MaxFlow == null)
                throw new InvalidOperationException("Instance not prepared");

            MaxFlow.Solve();
            return MaxFlow.GetOptimalFlow();
        }
    }

    private class FullAssignmentInstance : BenchmarkInstance
    {
        private readonly int _size;

        public FullAssignmentInstance(int size)
        {
            _size = size;
        }

        public override string Key => $"FullAssignment_{_size}";

        public override void Prepare()
        {
            // Create complete bipartite graph
            // Left nodes: 0 to size-1
            // Right nodes: size to 2*size-1
            // Source: 2*size
            // Sink: 2*size+1

            Graph = new ReverseArcListGraph();
            
            int leftStart = 0;
            int rightStart = _size;
            int source = 2 * _size;
            int sink = 2 * _size + 1;

            // Add nodes
            for (int i = 0; i <= sink; i++)
            {
                Graph.AddNode(i);
            }

            // Reserve space
            int numArcs = _size + _size * _size + _size; // source->left + left->right + right->sink
            Graph.ReserveArcs(numArcs);

            MaxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(Graph, source, sink);

            // Add source -> left arcs
            for (int i = 0; i < _size; i++)
            {
                var arc = Graph.AddArc(source, leftStart + i);
                MaxFlow.SetArcCapacity(arc, 100);
            }

            // Add left -> right arcs (complete bipartite)
            var random = new Random(42); // Fixed seed for reproducibility
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    var arc = Graph.AddArc(leftStart + i, rightStart + j);
                    MaxFlow.SetArcCapacity(arc, random.Next(1, 10));
                }
            }

            // Add right -> sink arcs
            for (int j = 0; j < _size; j++)
            {
                var arc = Graph.AddArc(rightStart + j, sink);
                MaxFlow.SetArcCapacity(arc, 100);
            }
        }
    }

    private class PartialAssignmentInstance : BenchmarkInstance
    {
        private readonly int _size;

        public PartialAssignmentInstance(int size)
        {
            _size = size;
        }

        public override string Key => $"PartialAssignment_{_size}";

        public override void Prepare()
        {
            // Create sparse bipartite graph where each left node connects to 5 right nodes
            Graph = new ReverseArcListGraph();
            
            int leftStart = 0;
            int rightStart = _size;
            int source = 2 * _size;
            int sink = 2 * _size + 1;

            // Add nodes
            for (int i = 0; i <= sink; i++)
            {
                Graph.AddNode(i);
            }

            MaxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(Graph, source, sink);

            // Add source -> left arcs
            for (int i = 0; i < _size; i++)
            {
                var arc = Graph.AddArc(source, leftStart + i);
                MaxFlow.SetArcCapacity(arc, 50);
            }

            // Add left -> right arcs (each left connects to 5 random right nodes)
            var random = new Random(42);
            for (int i = 0; i < _size; i++)
            {
                var rightNodes = new HashSet<int>();
                while (rightNodes.Count < Math.Min(5, _size))
                {
                    rightNodes.Add(random.Next(_size));
                }

                foreach (var j in rightNodes)
                {
                    var arc = Graph.AddArc(leftStart + i, rightStart + j);
                    MaxFlow.SetArcCapacity(arc, random.Next(5, 20));
                }
            }

            // Add right -> sink arcs
            for (int j = 0; j < _size; j++)
            {
                var arc = Graph.AddArc(rightStart + j, sink);
                MaxFlow.SetArcCapacity(arc, 50);
            }
        }
    }

    private class RandomFlowInstance : BenchmarkInstance
    {
        private readonly int _size;
        private readonly double _density;

        public RandomFlowInstance(int size, double density)
        {
            _size = size;
            _density = density;
        }

        public override string Key => $"RandomFlow_{_density:F1}_{_size}";

        public override void Prepare()
        {
            // Create random directed graph with given density
            Graph = new ReverseArcListGraph();
            
            int source = 0;
            int sink = _size - 1;

            // Add nodes
            for (int i = 0; i < _size; i++)
            {
                Graph.AddNode(i);
            }

            MaxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(Graph, source, sink);

            // Add random arcs based on density
            var random = new Random(42);
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    if (i != j && random.NextDouble() < _density)
                    {
                        var arc = Graph.AddArc(i, j);
                        MaxFlow.SetArcCapacity(arc, random.Next(1, 100));
                    }
                }
            }
        }
    }

    private class GridFlowInstance : BenchmarkInstance
    {
        private readonly int _gridSize;

        public GridFlowInstance(int gridSize)
        {
            _gridSize = gridSize;
        }

        public override string Key => $"GridFlow_{_gridSize * _gridSize}";

        public override void Prepare()
        {
            // Create grid graph where flow goes from top-left to bottom-right
            Graph = new ReverseArcListGraph();
            
            int numNodes = _gridSize * _gridSize;
            int source = 0; // Top-left
            int sink = numNodes - 1; // Bottom-right

            // Add nodes
            for (int i = 0; i < numNodes; i++)
            {
                Graph.AddNode(i);
            }

            MaxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(Graph, source, sink);

            // Add grid edges
            var random = new Random(42);
            for (int row = 0; row < _gridSize; row++)
            {
                for (int col = 0; col < _gridSize; col++)
                {
                    int node = row * _gridSize + col;

                    // Right edge
                    if (col < _gridSize - 1)
                    {
                        var arc = Graph.AddArc(node, node + 1);
                        MaxFlow.SetArcCapacity(arc, random.Next(10, 50));
                    }

                    // Down edge
                    if (row < _gridSize - 1)
                    {
                        var arc = Graph.AddArc(node, node + _gridSize);
                        MaxFlow.SetArcCapacity(arc, random.Next(10, 50));
                    }
                }
            }
        }
    }

    #endregion
}