using System;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;

// Debug simple 2-node problem
var builder = new GraphBuilder();
builder.AddNodes(2);
builder.AddArc(0, 1); // Arc 0

using var graph = builder.Build();
var solver = new NetworkSimplexFixed(graph);

// Supply at node 0, demand at node 1
solver.SetNodeSupply(builder.GetNode(0), 5);
solver.SetNodeSupply(builder.GetNode(1), -5);
solver.SetArcCost(new Arc(0), 1);

Console.WriteLine("Solving 2-node problem...");
var status = solver.Solve();
Console.WriteLine($"Status: {status}");

if (status == SolverStatus.Optimal)
{
    Console.WriteLine($"Flow: {solver.GetFlow(new Arc(0))}");
    Console.WriteLine($"Total cost: {solver.GetTotalCost()}");
}
else
{
    Console.WriteLine("Failed to find optimal solution");
}