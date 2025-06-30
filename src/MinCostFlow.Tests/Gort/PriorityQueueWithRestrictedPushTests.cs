using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class PriorityQueueWithRestrictedPushTests
{
    [Fact]
    public void EmptyQueue_ShouldBehaveCorrectly()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<string, int>();

        // Assert
        queue.IsEmpty().Should().BeTrue();
        queue.Count.Should().Be(0);
        Assert.Throws<InvalidOperationException>(() => queue.Pop());
    }

    [Fact]
    public void Clear_ShouldEmptyQueue()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        queue.Push("A", 5);
        queue.Push("B", 5);
        queue.Push("C", 6);

        // Act
        queue.Clear();

        // Assert
        queue.IsEmpty().Should().BeTrue();
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void Push_SingleElement_ShouldWork()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<string, int>();

        // Act
        queue.Push("A", 5);

        // Assert
        queue.IsEmpty().Should().BeFalse();
        queue.Count.Should().Be(1);
        queue.Pop().Should().Be("A");
        queue.IsEmpty().Should().BeTrue();
    }

    [Fact]
    public void Push_WithinRestriction_ShouldWork()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        queue.Push("A", 5);

        // Act - Push with priority 4 (which is highest_priority - 1)
        queue.Push("B", 4);
        queue.Push("C", 5);
        queue.Push("D", 6);

        // Assert
        queue.Count.Should().Be(4);
    }

    [Fact]
    public void Push_ViolatingRestriction_ShouldThrow()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        queue.Push("A", 5);

        // Act & Assert - Try to push with priority 3 (less than highest_priority - 1)
        Assert.Throws<InvalidOperationException>(() => queue.Push("B", 3));
    }

    [Fact]
    public void Pop_ShouldReturnHighestPriority_LIFO()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        
        // Push elements with same priority
        queue.Push("A", 5);
        queue.Push("B", 5);
        queue.Push("C", 5);

        // Act & Assert - Should return in LIFO order
        queue.Pop().Should().Be("C");
        queue.Pop().Should().Be("B");
        queue.Pop().Should().Be("A");
    }

    [Fact]
    public void Pop_MixedPriorities_ShouldReturnHighestFirst()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        
        queue.Push("A", 5);
        queue.Push("B", 6);
        queue.Push("C", 5);
        queue.Push("D", 6);

        // Act & Assert - Should return priority 6 elements first (LIFO within priority)
        queue.Pop().Should().Be("D");
        queue.Pop().Should().Be("B");
        queue.Pop().Should().Be("C");
        queue.Pop().Should().Be("A");
    }

    [Fact]
    public void EvenOddQueues_ShouldWorkCorrectly()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        
        // Test basic even/odd separation
        queue.Push("A", 4);     // even queue
        queue.Push("B", 5);     // odd queue
        
        // Act & Assert
        queue.Pop().Should().Be("B"); // Priority 5 (odd) is higher
        queue.Pop().Should().Be("A"); // Priority 4 (even)
        
        // Test LIFO within same priority
        queue.Push("C", 6);     // even queue
        queue.Push("D", 6);     // even queue
        queue.Push("E", 7);     // odd queue
        
        queue.Pop().Should().Be("E"); // Priority 7 (odd)
        queue.Pop().Should().Be("D"); // Priority 6 (even), most recent
        queue.Pop().Should().Be("C"); // Priority 6 (even), first
    }

    [Fact]
    public void ComplexSequence_ShouldMaintainInvariants()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        var operations = new List<(string op, string? elem, int? priority)>
        {
            ("push", "A", 10),
            ("push", "B", 10),
            ("push", "C", 11),
            ("push", "D", 10),
            ("pop", null, null),  // Should pop C (priority 11)
            ("push", "E", 11),
            ("push", "F", 10),
            ("pop", null, null),  // Should pop E (priority 11)
            ("pop", null, null),  // Should pop F (priority 10, most recent)
        };

        var results = new List<string>();

        // Act
        foreach (var (op, elem, priority) in operations)
        {
            if (op == "push")
            {
                queue.Push(elem!, priority!.Value);
            }
            else if (op == "pop")
            {
                results.Add(queue.Pop());
            }
        }

        // Assert
        results.Should().Equal("C", "E", "F");
        queue.Count.Should().Be(3);
    }

    [Fact]
    public void DifferentTypes_ShouldWork()
    {
        // Test with uint priority
        var uintQueue = new PriorityQueueWithRestrictedPush<string, uint>();
        uintQueue.Push("A", 5u);
        uintQueue.Push("B", 6u);
        uintQueue.Pop().Should().Be("B");

        // Test with short priority
        var shortQueue = new PriorityQueueWithRestrictedPush<string, short>();
        shortQueue.Push("A", (short)5);
        shortQueue.Push("B", (short)6);
        shortQueue.Pop().Should().Be("B");

        // Test with ushort priority
        var ushortQueue = new PriorityQueueWithRestrictedPush<string, ushort>();
        ushortQueue.Push("A", (ushort)5);
        ushortQueue.Push("B", (ushort)6);
        ushortQueue.Pop().Should().Be("B");
    }

    [Fact]
    public void StressTest_LargeNumberOfElements()
    {
        // Arrange
        var queue = new PriorityQueueWithRestrictedPush<int, int>();
        const int numElements = 10000;
        var random = new Random(42);

        // Act - Push elements with priorities within restriction
        int currentMax = 0;
        var elementPriorities = new Dictionary<int, int>();
        
        for (int i = 0; i < numElements; i++)
        {
            int priority = currentMax + random.Next(-1, 2); // -1, 0, or 1 relative to current max
            if (priority < currentMax - 1) priority = currentMax - 1;
            if (priority > currentMax) currentMax = priority;
            
            elementPriorities[i] = priority;
            queue.Push(i, priority);
        }

        // Assert
        queue.Count.Should().Be(numElements);
        
        // Pop all elements - they should come out in non-increasing priority order
        var poppedElements = new List<int>();
        while (!queue.IsEmpty())
        {
            poppedElements.Add(queue.Pop());
        }
        
        poppedElements.Count.Should().Be(numElements);
        
        // Verify that elements came out in non-increasing priority order
        for (int i = 1; i < poppedElements.Count; i++)
        {
            elementPriorities[poppedElements[i]].Should().BeLessThanOrEqualTo(elementPriorities[poppedElements[i-1]]);
        }
    }
}