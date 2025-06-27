using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Benchmarks;

/// <summary>
/// Performance benchmarks for Gort data structures.
/// Verifies theoretical specifications from docs/spec/graph.md and docs/spec/svector.md.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(Config))]
public class GortGraphBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddColumn(StatisticColumn.Mean);
            AddColumn(StatisticColumn.StdDev);
            AddColumn(StatisticColumn.Min);
            AddColumn(StatisticColumn.Max);
            WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));
            
            // Use ShortRun for quick results
            AddJob(Job.ShortRun
                .WithLaunchCount(1)
                .WithWarmupCount(3)
                .WithIterationCount(5));
        }
    }
    private const int SmallNodes = 1000;
    private const int SmallArcs = 5000;
    private const int MediumNodes = 10000;
    private const int MediumArcs = 50000;
    private const int LargeNodes = 100000;
    private const int LargeArcs = 500000;

    private readonly Random _random = new Random(42);
    private List<(int tail, int head)> _smallGraph = new();
    private List<(int tail, int head)> _mediumGraph = new();
    private List<(int tail, int head)> _largeGraph = new();

    [GlobalSetup]
    public void Setup()
    {
        _smallGraph = GenerateRandomGraph(SmallNodes, SmallArcs);
        _mediumGraph = GenerateRandomGraph(MediumNodes, MediumArcs);
        _largeGraph = GenerateRandomGraph(LargeNodes, LargeArcs);
    }

    private List<(int tail, int head)> GenerateRandomGraph(int nodes, int arcs)
    {
        var result = new List<(int, int)>(arcs);
        for (int i = 0; i < arcs; i++)
        {
            int tail = _random.Next(nodes);
            int head = _random.Next(nodes);
            result.Add((tail, head));
        }
        return result;
    }

    #region Construction Benchmarks

    [Benchmark]
    public ListGraph ConstructListGraph_Small()
    {
        var graph = new ListGraph();
        foreach (var (tail, head) in _smallGraph)
        {
            graph.AddArc(tail, head);
        }
        return graph;
    }

    [Benchmark]
    public StaticGraph ConstructStaticGraph_Small()
    {
        var graph = new StaticGraph();
        foreach (var (tail, head) in _smallGraph)
        {
            graph.AddArc(tail, head);
        }
        graph.Build();
        return graph;
    }

    [Benchmark]
    public ReverseArcListGraph ConstructReverseArcListGraph_Small()
    {
        var graph = new ReverseArcListGraph();
        foreach (var (tail, head) in _smallGraph)
        {
            graph.AddArc(tail, head);
        }
        return graph;
    }

    [Benchmark]
    public ReverseArcStaticGraph ConstructReverseArcStaticGraph_Small()
    {
        var graph = new ReverseArcStaticGraph();
        foreach (var (tail, head) in _smallGraph)
        {
            graph.AddArc(tail, head);
        }
        graph.Build();
        return graph;
    }

    #endregion

    #region Iteration Benchmarks

    private ListGraph? _listGraph;
    private StaticGraph? _staticGraph;
    private ReverseArcListGraph? _reverseArcListGraph;
    private ReverseArcStaticGraph? _reverseArcStaticGraph;

    [GlobalSetup(Target = nameof(IterateOutgoingArcs_ListGraph))]
    public void SetupListGraph()
    {
        Setup();
        _listGraph = new ListGraph();
        foreach (var (tail, head) in _mediumGraph)
        {
            _listGraph.AddArc(tail, head);
        }
    }

    [GlobalSetup(Target = nameof(IterateOutgoingArcs_StaticGraph))]
    public void SetupStaticGraph()
    {
        Setup();
        _staticGraph = new StaticGraph();
        foreach (var (tail, head) in _mediumGraph)
        {
            _staticGraph.AddArc(tail, head);
        }
        _staticGraph.Build();
    }

    [GlobalSetup(Target = nameof(IterateOutgoingArcs_ReverseArcListGraph))]
    public void SetupReverseArcListGraph()
    {
        Setup();
        _reverseArcListGraph = new ReverseArcListGraph();
        foreach (var (tail, head) in _mediumGraph)
        {
            _reverseArcListGraph.AddArc(tail, head);
        }
    }

    [GlobalSetup(Target = nameof(IterateOutgoingArcs_ReverseArcStaticGraph))]
    public void SetupReverseArcStaticGraph()
    {
        Setup();
        _reverseArcStaticGraph = new ReverseArcStaticGraph();
        foreach (var (tail, head) in _mediumGraph)
        {
            _reverseArcStaticGraph.AddArc(tail, head);
        }
        _reverseArcStaticGraph.Build();
    }

    [Benchmark]
    public int IterateOutgoingArcs_ListGraph()
    {
        int sum = 0;
        for (int node = 0; node < 100; node++)
        {
            foreach (var arc in _listGraph!.OutgoingArcs(node))
            {
                sum += _listGraph.Head(arc);
            }
        }
        return sum;
    }

    [Benchmark]
    public int IterateOutgoingArcs_StaticGraph()
    {
        int sum = 0;
        for (int node = 0; node < 100; node++)
        {
            foreach (var arc in _staticGraph!.OutgoingArcs(node))
            {
                sum += _staticGraph.Head(arc);
            }
        }
        return sum;
    }

    [Benchmark]
    public int IterateOutgoingArcs_ReverseArcListGraph()
    {
        int sum = 0;
        for (int node = 0; node < 100; node++)
        {
            foreach (var arc in _reverseArcListGraph!.OutgoingArcs(node))
            {
                sum += _reverseArcListGraph.Head(arc);
            }
        }
        return sum;
    }

    [Benchmark]
    public int IterateOutgoingArcs_ReverseArcStaticGraph()
    {
        int sum = 0;
        for (int node = 0; node < 100; node++)
        {
            foreach (var arc in _reverseArcStaticGraph!.OutgoingArcs(node))
            {
                sum += _reverseArcStaticGraph.Head(arc);
            }
        }
        return sum;
    }

    #endregion

    #region Memory Usage Benchmarks

    [Benchmark]
    public StaticGraph MemoryUsage_StaticGraph_Large()
    {
        var graph = new StaticGraph();
        graph.Reserve(LargeNodes, LargeArcs);
        foreach (var (tail, head) in _largeGraph)
        {
            graph.AddArc(tail, head);
        }
        graph.Build();
        return graph;
    }

    [Benchmark]
    public ReverseArcStaticGraph MemoryUsage_ReverseArcStaticGraph_Large()
    {
        var graph = new ReverseArcStaticGraph();
        graph.Reserve(LargeNodes, LargeArcs);
        foreach (var (tail, head) in _largeGraph)
        {
            graph.AddArc(tail, head);
        }
        graph.Build();
        return graph;
    }

    #endregion

    #region Complete Graph Benchmarks

    [Params(100)]
    public int CompleteGraphSize { get; set; }

    [Benchmark]
    public int CompleteGraph_IterateAllArcs()
    {
        var graph = new CompleteGraph(CompleteGraphSize);
        int sum = 0;
        foreach (var arc in graph.AllForwardArcs())
        {
            sum += graph.Head(arc) + graph.Tail(arc);
        }
        return sum;
    }

    [Benchmark]
    public int CompleteBipartiteGraph_IterateAllArcs()
    {
        var graph = new CompleteBipartiteGraph(CompleteGraphSize / 2, CompleteGraphSize / 2);
        int sum = 0;
        foreach (var arc in graph.AllForwardArcs())
        {
            sum += graph.Head(arc) + graph.Tail(arc);
        }
        return sum;
    }

    #endregion

    #region SVector Benchmarks

    [Params(10000)]
    public int SVectorSize { get; set; }

    private SVector<int>? _svector;
    private int[]? _indices;

    [GlobalSetup(Target = nameof(SVector_Construction))]
    public void SetupSVectorConstruction()
    {
        // Nothing to setup for construction benchmark
    }

    [Benchmark]
    [BenchmarkCategory("SVector")]
    public SVector<int> SVector_Construction()
    {
        var sv = new SVector<int>();
        sv.Resize(SVectorSize);
        return sv;
    }

    [GlobalSetup(Targets = new[] { nameof(SVector_IndexedAccess), nameof(SVector_NegativeIndexAccess) })]
    public void SetupSVectorAccess()
    {
        _svector = new SVector<int>();
        _svector.Resize(SVectorSize);
        _indices = new int[100];
        var random = new Random(42);
        
        // Fill with test data
        for (int i = -SVectorSize/2; i < SVectorSize/2; i++)
        {
            _svector[i] = i * 2;
        }
        
        // Generate random indices for access
        for (int i = 0; i < _indices.Length; i++)
        {
            _indices[i] = random.Next(-SVectorSize/2, SVectorSize/2);
        }
    }

    [Benchmark]
    [BenchmarkCategory("SVector")]
    public int SVector_IndexedAccess()
    {
        int sum = 0;
        for (int i = 0; i < _indices!.Length; i++)
        {
            sum += _svector![_indices[i]];
        }
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("SVector")]
    public int SVector_NegativeIndexAccess()
    {
        int sum = 0;
        for (int i = -50; i < 50; i++)
        {
            sum += _svector![i];
        }
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("SVector")]
    public SVector<int> SVector_Resize()
    {
        var sv = new SVector<int>();
        sv.Resize(SVectorSize);
        sv.Resize(SVectorSize * 2);
        return sv;
    }

    #endregion

    #region Theoretical Memory Verification

    [Params(10000)]
    public int TheoreticalTestSize { get; set; }

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public ListGraph Memory_ListGraph()
    {
        // Theory: 2n + 4m integers
        var graph = new ListGraph();
        graph.Reserve(TheoreticalTestSize, TheoreticalTestSize * 5);
        for (int i = 0; i < TheoreticalTestSize; i++)
        {
            graph.AddNode(i);
        }
        for (int i = 0; i < TheoreticalTestSize * 5; i++)
        {
            graph.AddArc(i % TheoreticalTestSize, (i + 1) % TheoreticalTestSize);
        }
        return graph;
    }

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public StaticGraph Memory_StaticGraph()
    {
        // Theory: (n+1) + 2m integers after Build()
        var graph = new StaticGraph();
        graph.Reserve(TheoreticalTestSize, TheoreticalTestSize * 5);
        for (int i = 0; i < TheoreticalTestSize; i++)
        {
            graph.AddNode(i);
        }
        for (int i = 0; i < TheoreticalTestSize * 5; i++)
        {
            graph.AddArc(i % TheoreticalTestSize, (i + 1) % TheoreticalTestSize);
        }
        graph.Build();
        return graph;
    }

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public ReverseArcStaticGraph Memory_ReverseArcStaticGraph()
    {
        // Theory: 2(n+1) + 3m integers after Build()
        var graph = new ReverseArcStaticGraph();
        graph.Reserve(TheoreticalTestSize, TheoreticalTestSize * 5);
        for (int i = 0; i < TheoreticalTestSize; i++)
        {
            graph.AddNode(i);
        }
        for (int i = 0; i < TheoreticalTestSize * 5; i++)
        {
            graph.AddArc(i % TheoreticalTestSize, (i + 1) % TheoreticalTestSize);
        }
        graph.Build();
        return graph;
    }

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public CompleteGraph Memory_CompleteGraph()
    {
        // Theory: O(1) memory
        return new CompleteGraph(TheoreticalTestSize);
    }

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public CompleteBipartiteGraph Memory_CompleteBipartiteGraph()
    {
        // Theory: O(1) memory
        return new CompleteBipartiteGraph(TheoreticalTestSize / 2, TheoreticalTestSize / 2);
    }

    #endregion

    #region Performance Characteristics Verification

    private StaticGraph? _perfStaticGraph;
    private ReverseArcStaticGraph? _perfReverseGraph;

    [GlobalSetup(Target = nameof(Performance_CacheEfficiency_StaticGraph))]
    public void SetupPerfStaticGraph()
    {
        _perfStaticGraph = new StaticGraph();
        // Create a graph with good locality
        for (int i = 0; i < 10000; i++)
        {
            _perfStaticGraph.AddNode(i);
        }
        for (int i = 0; i < 10000; i++)
        {
            // Add arcs with good locality
            for (int j = 0; j < 5; j++)
            {
                _perfStaticGraph.AddArc(i, (i + j + 1) % 10000);
            }
        }
        _perfStaticGraph.Build();
    }

    [Benchmark]
    [BenchmarkCategory("Performance")]
    public long Performance_CacheEfficiency_StaticGraph()
    {
        // Measure cache-efficient iteration
        long sum = 0;
        foreach (var node in _perfStaticGraph!.AllNodes())
        {
            foreach (var arc in _perfStaticGraph.OutgoingArcs(node))
            {
                sum += _perfStaticGraph.Head(arc);
            }
        }
        return sum;
    }

    [GlobalSetup(Target = nameof(Performance_ReverseArcAccess))]
    public void SetupPerfReverseGraph()
    {
        _perfReverseGraph = new ReverseArcStaticGraph();
        for (int i = 0; i < 1000; i++)
        {
            _perfReverseGraph.AddNode(i);
        }
        for (int i = 0; i < 5000; i++)
        {
            _perfReverseGraph.AddArc(i % 1000, (i * 7) % 1000);
        }
        _perfReverseGraph.Build();
    }

    [Benchmark]
    [BenchmarkCategory("Performance")]
    public int Performance_ReverseArcAccess()
    {
        // Test O(1) reverse arc access
        int sum = 0;
        for (int i = 1; i <= 1000; i++)
        {
            var forward = _perfReverseGraph!.Head(i);
            var reverse = _perfReverseGraph.Head(-i);
            sum += forward + reverse;
        }
        return sum;
    }

    #endregion
}