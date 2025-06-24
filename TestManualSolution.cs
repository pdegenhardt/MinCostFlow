using System;
using System.Collections.Generic;
using System.Linq;
using MinCostFlow.Core.IO;
using MinCostFlow.Core.Types;

class TestManualSolution
{
    static void Main()
    {
        var problemPath = "benchmarks/data/dimacs/netgen_8_10a.min";
        var problem = DimacsReader.ReadFromFile(problemPath);
        
        Console.WriteLine($"Problem: {problem.NodeCount} nodes, {problem.ArcCount} arcs");
        
        // Group arcs by cost
        var arcsByCost = new Dictionary<long, List<(int arc, int from, int to, long cap)>>();
        
        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            var source = problem.Graph.Source(arc);
            var target = problem.Graph.Target(arc);
            var cost = problem.ArcCosts[i];
            var cap = problem.ArcUpperBounds[i];
            
            if (!arcsByCost.ContainsKey(cost))
                arcsByCost[cost] = new List<(int, int, int, long)>();
            
            arcsByCost[arc, source.Id, target.Id, cap].Add((i));
        }
        
        Console.WriteLine("\nArcs by cost:");
        foreach (var kvp in arcsByCost.OrderBy(x => x.Key).Take(10))
        {
            Console.WriteLine($"Cost {kvp.Key}: {kvp.Value.Count} arcs");
        }
        
        // Find paths from supply nodes to demand nodes using low-cost arcs
        Console.WriteLine("\nSupply nodes:");
        var supplyNodes = new List<(int node, long supply)>();
        var demandNodes = new List<(int node, long demand)>();
        
        for (int i = 0; i < problem.NodeCount; i++)
        {
            if (problem.NodeSupplies[i] > 0)
                supplyNodes.Add((i, problem.NodeSupplies[i]));
            else if (problem.NodeSupplies[i] < 0)
                demandNodes.Add((i, -problem.NodeSupplies[i]));
        }
        
        Console.WriteLine($"Found {supplyNodes.Count} supply nodes, {demandNodes.Count} demand nodes");
        
        // Check connectivity using only low-cost arcs
        Console.WriteLine("\nChecking connectivity with low-cost arcs (cost < 10000):");
        
        // Build adjacency list with only low-cost arcs
        var adj = new List<List<(int to, int arc, long cost)>>();
        for (int i = 0; i < problem.NodeCount; i++)
            adj.Add(new List<(int, int, long)>());
        
        for (int i = 0; i < problem.ArcCount; i++)
        {
            if (problem.ArcCosts[i] < 10000) // Skip high-cost arcs
            {
                var arc = new Arc(i);
                var source = problem.Graph.Source(arc);
                var target = problem.Graph.Target(arc);
                adj[source.Id].Add((target.Id, i, problem.ArcCosts[i]));
            }
        }
        
        // Check if we can reach demand nodes from supply nodes
        int reachablePairs = 0;
        int unreachablePairs = 0;
        
        foreach (var (supplyNode, supply) in supplyNodes.Take(5)) // Check first 5 supply nodes
        {
            var reachable = BFS(adj, supplyNode, problem.NodeCount);
            
            int reachableDemand = 0;
            foreach (var (demandNode, demand) in demandNodes)
            {
                if (reachable[demandNode])
                    reachableDemand++;
            }
            
            Console.WriteLine($"From supply node {supplyNode}: can reach {reachableDemand}/{demandNodes.Count} demand nodes");
            
            if (reachableDemand == demandNodes.Count)
                reachablePairs++;
            else
                unreachablePairs++;
        }
        
        Console.WriteLine($"\nConnectivity summary: {reachablePairs} fully connected, {unreachablePairs} partially connected");
        
        if (unreachablePairs > 0)
        {
            Console.WriteLine("\nSome nodes are not reachable without high-cost arcs!");
            Console.WriteLine("This explains why the solution uses high-cost arcs.");
        }
    }
    
    static bool[] BFS(List<List<(int to, int arc, long cost)>> adj, int start, int n)
    {
        var visited = new bool[n];
        var queue = new Queue<int>();
        queue.Enqueue(start);
        visited[start] = true;
        
        while (queue.Count > 0)
        {
            int u = queue.Dequeue();
            foreach (var (v, arc, cost) in adj[u])
            {
                if (!visited[v])
                {
                    visited[v] = true;
                    queue.Enqueue(v);
                }
            }
        }
        
        return visited;
    }
}