using System;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Analysis;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;

namespace MinCostFlow.Tools.Examples
{
    /// <summary>
    /// Demonstrates the automatic optimization configuration feature.
    /// </summary>
    public class AutoConfigurationExample
    {
        public static void Run()
        {
            Console.WriteLine("=== Automatic Configuration Example ===\n");
            
            // Example 1: Dense network
            DenseNetworkExample();
            
            Console.WriteLine();
            
            // Example 2: Sparse network
            SparseNetworkExample();
            
            Console.WriteLine();
            
            // Example 3: High degree variance network
            HighVarianceNetworkExample();
        }
        
        private static void DenseNetworkExample()
        {
            Console.WriteLine("1. Dense Network Example:");
            
            // Create a small but dense network
            var builder = new GraphBuilder();
            int nodeCount = 20;
            
            for (int i = 0; i < nodeCount; i++)
            {
                builder.AddNode();
            }
            
            // Add many arcs for high density
            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = 0; j < nodeCount; j++)
                {
                    if (i != j && (i + j) % 3 != 0)
                    {
                        builder.AddArc(i, j);
                    }
                }
            }
            
            var graph = builder.Build();
            var solver = new NetworkSimplex(graph);
            
            // Set costs and capacities
            for (int i = 0; i < graph.ArcCount; i++)
            {
                solver.SetArcBounds(new Arc(i), 0, 10);
                solver.SetArcCost(new Arc(i), 1 + i % 5);
            }
            
            // Set supplies
            solver.SetNodeSupply(new Node(0), 50);
            solver.SetNodeSupply(new Node(nodeCount - 1), -50);
            
            // Enable verbose output for this example
            Environment.SetEnvironmentVariable("MCF_VERBOSE", "1");
            
            // Solve - auto-configuration will be applied
            solver.Solve();
            
            Environment.SetEnvironmentVariable("MCF_VERBOSE", "0");
            
            // Show characteristics
            var characteristics = solver.GetProblemCharacteristics();
            Console.WriteLine($"  Detected as: {characteristics.DetectedType}");
            Console.WriteLine($"  Network density: {characteristics.Density:F4}");
            Console.WriteLine($"  Configuration selected: SmallBlocksForDense");
        }
        
        private static void SparseNetworkExample()
        {
            Console.WriteLine("2. Sparse Network Example:");
            
            // Create a large sparse network (star topology)
            var builder = new GraphBuilder();
            int nodeCount = 500;
            
            for (int i = 0; i < nodeCount; i++)
            {
                builder.AddNode();
            }
            
            // Star topology - hub connects to some nodes
            for (int i = 1; i < 20; i++)
            {
                builder.AddArc(0, i);
                builder.AddArc(i, 0);
            }
            
            var graph = builder.Build();
            var solver = new NetworkSimplex(graph);
            
            // Set costs and capacities
            for (int i = 0; i < graph.ArcCount; i++)
            {
                solver.SetArcBounds(new Arc(i), 0, 100);
                solver.SetArcCost(new Arc(i), 1);
            }
            
            // Set supplies
            solver.SetNodeSupply(new Node(0), 100);
            for (int i = 1; i < 20; i++)
            {
                solver.SetNodeSupply(new Node(i), -100/19);
            }
            
            // Analyze without solving
            var characteristics = solver.AnalyzeProblem();
            var config = OptimizationSelector.SelectConfiguration(characteristics);
            
            Console.WriteLine($"  Detected as: {characteristics.DetectedType}");
            Console.WriteLine($"  Network density: {characteristics.Density:F6}");
            Console.WriteLine($"  Selected flags: {config.Flags}");
            
            // Can also manually override if desired
            // solver.SetAutoConfiguration(false);
            // solver.SetOptimizationConfig(customConfig);
        }
        
        private static void HighVarianceNetworkExample()
        {
            Console.WriteLine("3. High Degree Variance Network Example:");
            
            // Create a network with uneven connectivity
            var builder = new GraphBuilder();
            int nodeCount = 100;
            
            for (int i = 0; i < nodeCount; i++)
            {
                builder.AddNode();
            }
            
            // Node 0 is a super-hub
            for (int i = 1; i < nodeCount; i++)
            {
                builder.AddArc(0, i);
            }
            
            // Nodes 1-10 are minor hubs
            for (int i = 1; i <= 10; i++)
            {
                for (int j = i * 10; j < Math.Min((i + 1) * 10, nodeCount); j++)
                {
                    if (j != i)
                    {
                        builder.AddArc(i, j);
                    }
                }
            }
            
            var graph = builder.Build();
            var solver = new NetworkSimplex(graph);
            
            // Set costs and capacities
            for (int i = 0; i < graph.ArcCount; i++)
            {
                solver.SetArcBounds(new Arc(i), 0, 10);
                solver.SetArcCost(new Arc(i), 1 + i % 10);
            }
            
            // Set supplies
            solver.SetNodeSupply(new Node(0), 200);
            for (int i = 1; i < 11; i++)
            {
                solver.SetNodeSupply(new Node(i), -20);
            }
            
            var characteristics = solver.AnalyzeProblem();
            var config = OptimizationSelector.SelectConfiguration(characteristics);
            
            Console.WriteLine($"  Detected as: {characteristics.DetectedType}");
            Console.WriteLine($"  Degree coefficient of variation: {characteristics.DegreeCV:F2}");
            Console.WriteLine($"  Max degree: {characteristics.MaxDegree}, Avg degree: {characteristics.AverageDegree:F1}");
            Console.WriteLine($"  Selected flags: {config.Flags}");
            Console.WriteLine($"  -> AdaptiveBlockSize enabled due to high degree variance");
        }
    }
}