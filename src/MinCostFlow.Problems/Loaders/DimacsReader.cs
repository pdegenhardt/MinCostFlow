using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Problems.Loaders;

/// <summary>
/// Reads minimum cost flow problems in DIMACS format.
/// DIMACS format for min cost flow:
/// c comment lines start with 'c'
/// p min NODES ARCS - problem line
/// n NODE SUPPLY - node supply/demand
/// a FROM TO LOWER UPPER COST - arc definition
/// </summary>
internal static class DimacsReader
{
    /// <summary>
    /// Reads a DIMACS minimum cost flow problem from a file.
    /// </summary>
    /// <param name="filePath">Path to the DIMACS file.</param>
    /// <returns>The parsed problem.</returns>
    public static DimacsMinCostFlowProblem ReadFromFile(string filePath)
    {
        using var reader = new StreamReader(filePath);
        return ReadFromStream(reader);
    }

    /// <summary>
    /// Reads a DIMACS minimum cost flow problem from a stream.
    /// </summary>
    /// <param name="reader">The stream reader.</param>
    /// <returns>The parsed problem.</returns>
    public static DimacsMinCostFlowProblem ReadFromStream(StreamReader reader)
    {
        var problem = new DimacsMinCostFlowProblem();
        var builder = new GraphBuilder();
        
        int nodeCount = 0;
        int arcCount = 0;
        var nodeSupplies = new Dictionary<int, long>();
        var arcs = new List<(int from, int to, long lower, long upper, long cost)>();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                continue;
            }

            switch (tokens[0])
            {
                case "c":
                    // Comment line - skip
                    break;

                case "p":
                    if (tokens.Length != 4 || tokens[1] != "min")
                    {
                        throw new FormatException($"Invalid problem line: {line}");
                    }

                    nodeCount = int.Parse(tokens[2], CultureInfo.InvariantCulture);
                    arcCount = int.Parse(tokens[3], CultureInfo.InvariantCulture);
                    
                    // Initialize nodes with their IDs
                    for (int i = 0; i < nodeCount; i++)
                    {
                        builder.AddNode(i);
                    }
                    break;

                case "n":
                    if (tokens.Length != 3)
                    {
                        throw new FormatException($"Invalid node line: {line}");
                    }

                    int nodeId = int.Parse(tokens[1], CultureInfo.InvariantCulture) - 1; // DIMACS uses 1-based indexing
                    long supply = long.Parse(tokens[2], CultureInfo.InvariantCulture);
                    nodeSupplies[nodeId] = supply;
                    break;

                case "a":
                    if (tokens.Length != 6)
                    {
                        throw new FormatException($"Invalid arc line: {line}");
                    }

                    int from = int.Parse(tokens[1], CultureInfo.InvariantCulture) - 1; // DIMACS uses 1-based indexing
                    int to = int.Parse(tokens[2], CultureInfo.InvariantCulture) - 1;
                    long lower = long.Parse(tokens[3], CultureInfo.InvariantCulture);
                    long upper = long.Parse(tokens[4], CultureInfo.InvariantCulture);
                    long cost = long.Parse(tokens[5], CultureInfo.InvariantCulture);
                    
                    arcs.Add((from, to, lower, upper, cost));
                    break;

                default:
                    // Unknown line type - skip
                    break;
            }
        }

        // Add arcs to the graph
        foreach (var (from, to, _, _, _) in arcs)
        {
            builder.AddArc(from, to);
        }

        var graph = builder.Build();
        
        // Create problem instance
        problem.Graph = graph;
        problem.NodeCount = nodeCount;
        problem.ArcCount = arcCount;
        problem.NodeSupplies = new long[nodeCount];
        problem.ArcLowerBounds = new long[arcCount];
        problem.ArcUpperBounds = new long[arcCount];
        problem.ArcCosts = new long[arcCount];

        // Set node supplies
        foreach (var (nodeId, supply) in nodeSupplies)
        {
            problem.NodeSupplies[nodeId] = supply;
        }

        // Set arc data
        for (int i = 0; i < arcs.Count; i++)
        {
            var (_, _, lower, upper, cost) = arcs[i];
            problem.ArcLowerBounds[i] = lower;
            problem.ArcUpperBounds[i] = upper;
            problem.ArcCosts[i] = cost;
        }

        return problem;
    }
}