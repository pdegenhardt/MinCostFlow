using Xunit;
using MinCostFlow.Core.Gort;
using FluentAssertions;

namespace MinCostFlow.Tests.Gort;

public class PriorityQueueTest
{
    [Fact]
    public void TestPriorityQueueRestriction()
    {
        var queue = new PriorityQueueWithRestrictedPush<int, int>();
        
        // First push - no restriction
        queue.Push(1, 5);
        
        // These should work
        queue.Push(2, 4); // 4 >= 5-1
        queue.Push(3, 6); // 6 >= 5-1
        
        // Now highest is 6, so minimum allowed is 6-1 = 5
        queue.Push(4, 5); // Should work
        
        // This should fail
        var ex = Assert.Throws<System.InvalidOperationException>(() => queue.Push(5, 3));
        ex.Message.Should().Contain("Priority 3 is less than");
    }
    
    [Fact]
    public void TestQueueOrdering()
    {
        var queue = new PriorityQueueWithRestrictedPush<int, int>();
        
        // Push in order
        queue.Push(1, 1);
        queue.Push(2, 2);
        queue.Push(3, 3);
        
        // Pop should return highest priority first
        queue.Pop().Should().Be(3);
        queue.Pop().Should().Be(2);
        queue.Pop().Should().Be(1);
    }
}