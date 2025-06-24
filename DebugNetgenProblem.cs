using System;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.IO;
using MinCostFlow.Core.Types;

namespace MinCostFlow.Debug
{
    class DebugNetgenProblem
    {
        static void Main(string[] args)
        {
            var problemPath = "benchmarks/data/dimacs/netgen_8_10a.min";
            var solutionPath = "benchmarks/data/dimacs/netgen_8_10a.sol";
            
            Console.WriteLine("=== NETGEN 8_10a Debug ===");
            Console.WriteLine($"Expected optimal: 55944");
            Console.WriteLine();
            
            // Read problem
            var problem = DimacsReader.ReadFromFile(problemPath);
            Console.WriteLine($"Nodes: {problem.NodeCount}");
            Console.WriteLine($"Arcs: {problem.ArcCount}");
            Console.WriteLine();
            
            // Check supplies
            long totalSupply = 0;
            int supplyNodes = 0;
            int demandNodes = 0;
            for (int i = 0; i < problem.NodeCount; i++)
            {
                if (problem.NodeSupplies[i] > 0)
                {
                    supplyNodes++;
                    totalSupply += problem.NodeSupplies[i];
                }
                else if (problem.NodeSupplies[i] < 0)
                {
                    demandNodes++;
                    totalSupply += problem.NodeSupplies[i];
                }
            }
            Console.WriteLine($"Supply nodes: {supplyNodes}");
            Console.WriteLine($"Demand nodes: {demandNodes}");
            Console.WriteLine($"Total supply balance: {totalSupply}");
            Console.WriteLine();
            
            // Check arc bounds
            int arcsWithLowerBounds = 0;
            long totalLowerBoundCost = 0;
            for (int i = 0; i < problem.ArcCount; i++)
            {
                if (problem.ArcLowerBounds[i] > 0)
                {
                    arcsWithLowerBounds++;
                    totalLowerBoundCost += problem.ArcLowerBounds[i] * problem.ArcCosts[i];
                }
            }
            Console.WriteLine($"Arcs with lower bounds > 0: {arcsWithLowerBounds}");
            Console.WriteLine($"Total lower bound cost contribution: {totalLowerBoundCost}");
            Console.WriteLine();
            
            // Create solver
            var solver = new NetworkSimplex(problem.Graph);
            
            // Set up problem
            for (int i = 0; i < problem.NodeCount; i++)
            {
                solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
            }
            
            for (int i = 0; i < problem.ArcCount; i++)
            {
                var arc = new Arc(i);
                solver.SetArcCost(arc, problem.ArcCosts[i]);
                solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
            }
            
            // Solve
            Console.WriteLine("Solving...");
            var status = solver.Solve();
            Console.WriteLine($"Status: {status}");
            
            if (status == SolverStatus.Optimal)
            {
                var totalCost = solver.GetTotalCost();
                Console.WriteLine($"Total cost: {totalCost}");
                Console.WriteLine($"Expected:  55944");
                Console.WriteLine($"Difference: {totalCost - 55944}");
                Console.WriteLine();
                
                // Calculate total cost manually
                long manualCost = 0;
                for (int i = 0; i < problem.ArcCount; i++)
                {
                    var arc = new Arc(i);
                    var flow = solver.GetFlow(arc);
                    if (flow > 0)
                    {
                        manualCost += flow * problem.ArcCosts[i];
                    }
                }
                Console.WriteLine($"Manual cost calculation: {manualCost}");
                
                // Show some arc flows
                Console.WriteLine("\nFirst 10 arcs with flow > 0:");
                int count = 0;
                for (int i = 0; i < problem.ArcCount && count < 10; i++)
                {
                    var arc = new Arc(i);
                    var flow = solver.GetFlow(arc);
                    if (flow > 0)
                    {
                        var source = problem.Graph.Source(arc);
                        var target = problem.Graph.Target(arc);
                        Console.WriteLine($"  Arc {i}: {source.Id + 1} -> {target.Id + 1}, " +
                                        $"flow = {flow}, cost = {problem.ArcCosts[i]}, " +
                                        $"lower = {problem.ArcLowerBounds[i]}, " +
                                        $"contribution = {flow * problem.ArcCosts[i]}");
                        count++;
                    }
                }
            }
        }
    }
}