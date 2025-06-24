using System;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.IO;
using MinCostFlow.Core.Types;

class TestNetworkSimplexOptimality
{
    static void Main()
    {
        var problemPath = "benchmarks/data/dimacs/netgen_8_10a.min";
        var problem = DimacsReader.ReadFromFile(problemPath);
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
        var status = solver.Solve();
        Console.WriteLine($"Status: {status}");
        
        if (status == SolverStatus.Optimal)
        {
            Console.WriteLine($"Total cost: {solver.GetTotalCost()}");
            
            // Check optimality conditions
            Console.WriteLine("\nChecking optimality conditions:");
            
            // Get node potentials
            var potentials = new long[problem.NodeCount];
            for (int i = 0; i < problem.NodeCount; i++)
            {
                potentials[i] = solver.GetPotential(new Node(i));
            }
            
            // Check reduced costs
            int violations = 0;
            for (int i = 0; i < problem.ArcCount; i++)
            {
                var arc = new Arc(i);
                var source = problem.Graph.Source(arc);
                var target = problem.Graph.Target(arc);
                var flow = solver.GetFlow(arc);
                
                // Reduced cost = cost + potential[source] - potential[target]
                long reducedCost = problem.ArcCosts[i] + potentials[source.Id] - potentials[target.Id];
                
                // Check optimality conditions:
                // If flow > lower, then reduced cost <= 0
                // If flow < upper, then reduced cost >= 0
                bool violation = false;
                if (flow > problem.ArcLowerBounds[i] && reducedCost > 0)
                {
                    Console.WriteLine($"Violation on arc {i}: flow > lower but reduced cost = {reducedCost} > 0");
                    violation = true;
                }
                if (flow < problem.ArcUpperBounds[i] && reducedCost < 0)
                {
                    Console.WriteLine($"Violation on arc {i}: flow < upper but reduced cost = {reducedCost} < 0");
                    violation = true;
                }
                
                if (violation)
                {
                    violations++;
                    if (violations >= 10) break; // Limit output
                }
            }
            
            Console.WriteLine($"\nTotal violations: {violations}");
            
            // Check if we can improve the solution manually
            Console.WriteLine("\nLooking for improvement opportunities:");
            long potentialImprovement = 0;
            int improvableArcs = 0;
            
            for (int i = 0; i < problem.ArcCount; i++)
            {
                var arc = new Arc(i);
                var source = problem.Graph.Source(arc);
                var target = problem.Graph.Target(arc);
                var flow = solver.GetFlow(arc);
                
                long reducedCost = problem.ArcCosts[i] + potentials[source.Id] - potentials[target.Id];
                
                // Can we increase flow?
                if (reducedCost < 0 && flow < problem.ArcUpperBounds[i])
                {
                    long delta = Math.Min(problem.ArcUpperBounds[i] - flow, 100); // Limit delta for display
                    long improvement = -reducedCost * delta;
                    potentialImprovement += improvement;
                    improvableArcs++;
                    
                    if (improvableArcs <= 5)
                    {
                        Console.WriteLine($"Arc {i}: can increase flow by {delta}, improvement = {improvement}");
                    }
                }
                
                // Can we decrease flow?
                if (reducedCost > 0 && flow > problem.ArcLowerBounds[i])
                {
                    long delta = Math.Min(flow - problem.ArcLowerBounds[i], 100); // Limit delta
                    long improvement = reducedCost * delta;
                    potentialImprovement += improvement;
                    improvableArcs++;
                    
                    if (improvableArcs <= 5)
                    {
                        Console.WriteLine($"Arc {i}: can decrease flow by {delta}, improvement = {improvement}");
                    }
                }
            }
            
            Console.WriteLine($"\nTotal improvable arcs: {improvableArcs}");
            Console.WriteLine($"Potential improvement (approximate): {potentialImprovement}");
        }
    }
}