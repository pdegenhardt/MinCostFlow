using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace MinCostFlow.Tools;

/// <summary>
/// Reads and writes DIMACS solution files.
/// </summary>
public static class SolutionReader
{
    public class Solution
    {
        public long OptimalValue { get; set; }
        public List<(int from, int to, long flow)> Flows { get; } = new();
    }
    
    /// <summary>
    /// Read a DIMACS solution file.
    /// </summary>
    public static Solution? ReadSolution(string filename)
    {
        if (!File.Exists(filename))
            return null;
            
        var solution = new Solution();
        
        foreach (var line in File.ReadAllLines(filename))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                continue;
                
            switch (tokens[0])
            {
                case "s":
                    if (tokens.Length >= 2)
                    {
                        solution.OptimalValue = long.Parse(tokens[1], CultureInfo.InvariantCulture);
                    }
                    break;
                    
                case "f":
                    if (tokens.Length >= 4)
                    {
                        int from = int.Parse(tokens[1], CultureInfo.InvariantCulture);
                        int to = int.Parse(tokens[2], CultureInfo.InvariantCulture);
                        long flow = long.Parse(tokens[3], CultureInfo.InvariantCulture);
                        solution.Flows.Add((from, to, flow));
                    }
                    break;
                    
                case "#":
                    // Handle simple format: # Known solution: VALUE
                    if (line.Contains("Known solution:"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length >= 2 && long.TryParse(parts[1].Trim(), out var value))
                        {
                            solution.OptimalValue = value;
                        }
                    }
                    break;
            }
        }
        
        return solution;
    }
    
    /// <summary>
    /// Write a DIMACS solution file.
    /// </summary>
    public static void WriteSolution(string filename, long optimalValue, 
        List<(int from, int to, long flow)> flows, string problemName)
    {
        using var writer = new StreamWriter(filename);
        
        writer.WriteLine($"c Solution file for {problemName}");
        writer.WriteLine("c");
        writer.WriteLine("c Optimal solution");
        writer.WriteLine($"s {optimalValue}");
        writer.WriteLine("c");
        writer.WriteLine("c Non-zero flows (SRC DST FLOW)");
        
        foreach (var (from, to, flow) in flows)
        {
            // Convert 0-based to 1-based indexing for DIMACS
            writer.WriteLine($"f {from + 1} {to + 1} {flow}");
        }
        
        writer.WriteLine("c");
        writer.WriteLine("c End of file");
    }
}