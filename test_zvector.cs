using System;
using MinCostFlow.Core.DataStructures;

Console.WriteLine("Testing ZVector...");

// Test creating ZVector for 1 arc
using var zv = ZVector<long>.ForArcs(1);
Console.WriteLine($"ZVector range: [{zv.MinIndex}, {zv.MaxIndex}]");

// Test accessing indices
try 
{
    Console.WriteLine("Setting zv[0] = 10");
    zv[0] = 10;
    Console.WriteLine($"zv[0] = {zv[0]}");
    
    Console.WriteLine("Setting zv[-1] = 20");
    zv[-1] = 20;
    Console.WriteLine($"zv[-1] = {zv[-1]}");
    
    Console.WriteLine("Success!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}