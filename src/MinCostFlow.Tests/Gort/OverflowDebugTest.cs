using System;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class OverflowDebugTest
{
    private readonly ITestOutputHelper _output;

    public OverflowDebugTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestMaxFlowSumInitialization()
    {
        // Test with int flow and long sum
        var graph = new ReverseArcListGraph();
        graph.AddNode(0);
        graph.AddNode(1);
        var arc = graph.AddArc(0, 1);
        
        var maxFlow = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 1);
        
        // Get MaxFlowSum value via reflection
        var maxFlowSumField = typeof(GenericMaxFlow<ReverseArcListGraph, int, long>)
            .GetField("MaxFlowSum", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var maxFlowSum = maxFlowSumField?.GetValue(maxFlow);
        _output.WriteLine($"MaxFlowSum value: {maxFlowSum}");
        _output.WriteLine($"long.MaxValue: {long.MaxValue}");
        
        // Test with simple flow
        maxFlow.SetArcCapacity(arc, 100);
        var solved = maxFlow.Solve();
        _output.WriteLine($"Solved with capacity 100: {solved}, status: {maxFlow.status}, flow: {maxFlow.GetOptimalFlow()}");
        
        // Test with int.MaxValue
        var maxFlow2 = new GenericMaxFlow<ReverseArcListGraph, int, long>(graph, 0, 1);
        maxFlow2.SetArcCapacity(arc, int.MaxValue);
        var solved2 = maxFlow2.Solve();
        _output.WriteLine($"Solved with capacity int.MaxValue: {solved2}, status: {maxFlow2.status}, flow: {maxFlow2.GetOptimalFlow()}");
        _output.WriteLine($"int.MaxValue: {int.MaxValue}");
    }
}