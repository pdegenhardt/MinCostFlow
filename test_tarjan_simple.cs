using System;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;

// Simple test of TarjanEnhancedOrTools
var graph = new ReverseArcGraph();
var nodes = new[] { graph.AddNode(), graph.AddNode() };

var arc = graph.AddArc(nodes[0], nodes[1]);

var solver = new TarjanEnhancedOrTools(graph);

// Set supplies
solver.SetNodeSupply(nodes[0], 1);
solver.SetNodeSupply(nodes[1], -1);

// Set arc capacity and cost
solver.SetArcCapacity(arc, 2);
solver.SetArcCost(arc, 3);

Console.WriteLine("Starting solve...");
var status = solver.Solve();

Console.WriteLine($"Status: {status}");
if (status == SolverStatus.Optimal)
{
    Console.WriteLine($"Total cost: {solver.GetTotalCost()}");
    Console.WriteLine($"Flow on arc: {solver.GetFlow(arc)}");
}