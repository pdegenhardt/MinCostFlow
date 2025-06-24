using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MinCostFlow.Problems.Generators;

/// <summary>
/// Generates standard DIMACS test problems for validation.
/// </summary>
public static class ProblemGenerator
{
    /// <summary>
    /// Generate a transportation problem.
    /// </summary>
    public static void GenerateTransportationProblem(string filename, int sources, int sinks, long supply, int minCost = 1, int maxCost = 100)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        
        // Calculate supply/demand per node
        long supplyPerSource = supply / sources;
        long demandPerSink = supply / sinks;
        
        // Adjust for rounding
        long totalSupply = supplyPerSource * sources;
        long totalDemand = demandPerSink * sinks;
        if (totalSupply > totalDemand)
        {
            demandPerSink += (totalSupply - totalDemand) / sinks;
        }
        
        int nodes = sources + sinks;
        int arcs = sources * sinks;
        
        using var writer = new StreamWriter(filename);
        writer.WriteLine($"c Transportation problem: {sources} sources, {sinks} sinks");
        writer.WriteLine($"p min {nodes} {arcs}");
        
        // Write source supplies
        for (int i = 1; i <= sources; i++)
        {
            writer.WriteLine($"n {i} {supplyPerSource}");
        }
        
        // Write sink demands
        for (int i = sources + 1; i <= nodes; i++)
        {
            long demand = (i < nodes) ? demandPerSink : (totalSupply - demandPerSink * (sinks - 1));
            writer.WriteLine($"n {i} {-demand}");
        }
        
        // Write arcs from each source to each sink
        for (int s = 1; s <= sources; s++)
        {
            for (int t = sources + 1; t <= nodes; t++)
            {
                int cost = random.Next(minCost, maxCost + 1);
                long capacity = Math.Min(supplyPerSource, Math.Abs(demandPerSink) * 2);
                writer.WriteLine($"a {s} {t} 0 {capacity} {cost}");
            }
        }
    }
    
    /// <summary>
    /// Generate a circulation problem with negative cycles.
    /// </summary>
    public static void GenerateCirculationProblem(string filename, int nodes, double density, int minCost = -50, int maxCost = 100)
    {
        var random = new Random(42);
        int targetArcs = (int)(nodes * (nodes - 1) * density);
        
        // Create a connected graph first
        var arcs = new List<(int from, int to, int cost)>();
        
        // Create a cycle to ensure connectivity
        for (int i = 1; i < nodes; i++)
        {
            arcs.Add((i, i + 1, random.Next(minCost, maxCost + 1)));
        }
        arcs.Add((nodes, 1, random.Next(minCost, maxCost + 1))); // Close the cycle
        
        // Add random arcs
        var existingArcs = new HashSet<(int, int)>(arcs.Select(a => (a.from, a.to)));
        
        while (arcs.Count < targetArcs)
        {
            int from = random.Next(1, nodes + 1);
            int to = random.Next(1, nodes + 1);
            
            if (from != to && !existingArcs.Contains((from, to)))
            {
                arcs.Add((from, to, random.Next(minCost, maxCost + 1)));
                existingArcs.Add((from, to));
            }
        }
        
        using var writer = new StreamWriter(filename);
        writer.WriteLine($"c Circulation problem: {nodes} nodes, density={density:F2}");
        writer.WriteLine($"p min {nodes} {arcs.Count}");
        
        // No supplies for circulation problem
        
        // Write arcs
        foreach (var (from, to, cost) in arcs)
        {
            long capacity = random.Next(10, 101);
            writer.WriteLine($"a {from} {to} 0 {capacity} {cost}");
        }
    }
    
    /// <summary>
    /// Generate a grid network problem.
    /// </summary>
    public static void GenerateGridProblem(string filename, int rows, int cols, int sourceRow, int sourceCol, int sinkRow, int sinkCol, long supply)
    {
        var random = new Random(42);
        int nodes = rows * cols;
        var arcs = new List<(int from, int to, int cost, long cap)>();
        
        // Helper to convert grid position to node ID
        int NodeId(int r, int c) => r * cols + c + 1;
        
        // Create grid edges
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int current = NodeId(r, c);
                
                // Right edge
                if (c < cols - 1)
                {
                    int right = NodeId(r, c + 1);
                    int cost = random.Next(1, 11);
                    arcs.Add((current, right, cost, supply));
                    arcs.Add((right, current, cost, supply)); // Bidirectional
                }
                
                // Down edge
                if (r < rows - 1)
                {
                    int down = NodeId(r + 1, c);
                    int cost = random.Next(1, 11);
                    arcs.Add((current, down, cost, supply));
                    arcs.Add((down, current, cost, supply)); // Bidirectional
                }
            }
        }
        
        using var writer = new StreamWriter(filename);
        writer.WriteLine($"c Grid problem: {rows}x{cols} grid");
        writer.WriteLine($"p min {nodes} {arcs.Count}");
        
        // Write supplies
        int sourceNode = NodeId(sourceRow, sourceCol);
        int sinkNode = NodeId(sinkRow, sinkCol);
        
        writer.WriteLine($"n {sourceNode} {supply}");
        writer.WriteLine($"n {sinkNode} {-supply}");
        
        // Write arcs
        foreach (var (from, to, cost, cap) in arcs)
        {
            writer.WriteLine($"a {from} {to} 0 {cap} {cost}");
        }
    }
    
    /// <summary>
    /// Generate a simple path problem for quick testing.
    /// </summary>
    public static void GeneratePathProblem(string filename, int nodes, long supply)
    {
        var random = new Random(42);
        
        using var writer = new StreamWriter(filename);
        writer.WriteLine($"c Path problem: {nodes} nodes");
        writer.WriteLine($"p min {nodes} {nodes - 1}");
        
        // Supply at first node, demand at last
        writer.WriteLine($"n 1 {supply}");
        writer.WriteLine($"n {nodes} {-supply}");
        
        // Create path from 1 to nodes
        for (int i = 1; i < nodes; i++)
        {
            int cost = random.Next(1, 11);
            writer.WriteLine($"a {i} {i + 1} 0 {supply} {cost}");
        }
    }
    
    /// <summary>
    /// Generate an assignment problem (bipartite matching with costs).
    /// </summary>
    public static void GenerateAssignmentProblem(string filename, int workers, int tasks, int minCost = 1, int maxCost = 100, int? seed = 42)
    {
        if (workers != tasks)
        {
            throw new ArgumentException("Assignment problem must be balanced (workers == tasks)");
        }
        
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        int nodes = workers + tasks;
        int arcs = workers * tasks;
        
        using var writer = new StreamWriter(filename);
        writer.WriteLine($"c Bipartite assignment problem - {workers}x{tasks}");
        writer.WriteLine($"c {workers} workers (nodes 1-{workers}) to {tasks} tasks (nodes {workers + 1}-{nodes})");
        writer.WriteLine($"c Cost matrix generated with random costs in range [{minCost}, {maxCost}]");
        writer.WriteLine($"p min {nodes} {arcs}");
        
        // Write supplies: each worker has supply of 1
        for (int i = 1; i <= workers; i++)
        {
            writer.WriteLine($"n {i} 1");
        }
        
        // Write demands: each task has demand of 1
        for (int i = workers + 1; i <= nodes; i++)
        {
            writer.WriteLine($"n {i} -1");
        }
        
        // Write arcs from each worker to each task
        for (int w = 1; w <= workers; w++)
        {
            for (int t = workers + 1; t <= nodes; t++)
            {
                int cost = random.Next(minCost, maxCost + 1);
                // Lower bound 0, upper bound 1 (assignment constraint)
                writer.WriteLine($"a {w} {t} 0 1 {cost}");
            }
        }
    }
}