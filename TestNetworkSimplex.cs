using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;
using System;
using System.Diagnostics;

// Simple test to debug NetworkSimplex
var builder = new GraphBuilder();
builder.AddNodes(4);

// Add arcs
builder.AddArc(0, 2); // Arc 0
builder.AddArc(0, 3); // Arc 1
builder.AddArc(1, 2); // Arc 2
builder.AddArc(1, 3); // Arc 3

using var graph = builder.Build();
var solver = new NetworkSimplex(graph);

// Set supplies
solver.SetNodeSupply(builder.GetNode(0), 10);
solver.SetNodeSupply(builder.GetNode(1), 15);
solver.SetNodeSupply(builder.GetNode(2), -12);
solver.SetNodeSupply(builder.GetNode(3), -13);

// Set costs
solver.SetArcCost(new Arc(0), 3); // 0->2
solver.SetArcCost(new Arc(1), 5); // 0->3
solver.SetArcCost(new Arc(2), 4); // 1->2
solver.SetArcCost(new Arc(3), 2); // 1->3

// Solve with timing
var sw = Stopwatch.StartNew();
var status = solver.Solve();
sw.Stop();

Console.WriteLine($"Status: {status}");
Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");

if (status == SolverStatus.Optimal)
{
    Console.WriteLine($"Total cost: {solver.GetTotalCost()}");
    Console.WriteLine("Flows:");
    Console.WriteLine($"  0->2: {solver.GetFlow(new Arc(0))}");
    Console.WriteLine($"  0->3: {solver.GetFlow(new Arc(1))}");
    Console.WriteLine($"  1->2: {solver.GetFlow(new Arc(2))}");
    Console.WriteLine($"  1->3: {solver.GetFlow(new Arc(3))}");
}