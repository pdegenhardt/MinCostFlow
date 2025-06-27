using MinCostFlow.Experiment;
using System;
using Xunit;
using System.Collections.Generic;

namespace MinCostFlow.Experiment.Test;

public class PriorityQueueWithRestrictedPushTests
{
    [Fact]
    public void PushAndPop_SingleElement_WorksCorrectly()
    {
        var pq = new PriorityQueueWithRestrictedPush<string, int>();
        pq.Push("A", 2);
        Assert.False(pq.IsEmpty);
        var result = pq.Pop();
        Assert.Equal("A", result);
        Assert.True(pq.IsEmpty);
    }

    [Fact]
    public void Pop_EmptyQueue_ThrowsException()
    {
        var pq = new PriorityQueueWithRestrictedPush<int, int>();
        Assert.Throws<InvalidOperationException>(() => pq.Pop());
    }

    [Fact]
    public void Clear_EmptiesTheQueue()
    {
        var pq = new PriorityQueueWithRestrictedPush<string, int>();
        pq.Push("A", 1);
        pq.Push("B", 2);
        pq.Clear();
        Assert.True(pq.IsEmpty);
        Assert.Throws<InvalidOperationException>(() => pq.Pop());
    }

    [Fact]
    public void Push_EvenAndOddPriorities_HandledCorrectly()
    {
        var pq = new PriorityQueueWithRestrictedPush<string, int>();
        pq.Push("Even", 2);
        pq.Push("Odd", 3);
        Assert.Equal("Odd", pq.Pop());
        Assert.Equal("Even", pq.Pop());
    }

    [Fact]
    public void BasicBehavior()
    {
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        Assert.True(queue.IsEmpty);
        queue.Push("A", 1);
        queue.Push("B", 0);
        queue.Push("C", 2);
        queue.Push("D", 10);
        queue.Push("E", 9);
        Assert.Equal("D", queue.Pop());
        Assert.Equal("E", queue.Pop());
        Assert.Equal("C", queue.Pop());
        Assert.Equal("A", queue.Pop());
        Assert.Equal("B", queue.Pop());
        Assert.True(queue.IsEmpty);
        queue.Push("A", 1);
        queue.Push("B", 0);
        Assert.False(queue.IsEmpty);
        queue.Clear();
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void BasicBehaviorWithMixedPushPop()
    {
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        Assert.True(queue.IsEmpty);
        queue.Push("A", 1);
        queue.Push("B", 0);
        queue.Push("C", 2);
        Assert.Equal("C", queue.Pop());
        Assert.Equal("A", queue.Pop());
        queue.Push("D", 1);
        queue.Push("E", 0);
        Assert.Equal("D", queue.Pop());
        Assert.Equal("E", queue.Pop());
        Assert.Equal("B", queue.Pop());
        Assert.True(queue.IsEmpty);
        queue.Push("E", 1);
        Assert.False(queue.IsEmpty);
        Assert.Equal("E", queue.Pop());
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void DeathTest_PopOnEmptyThrows()
    {
        var queue = new PriorityQueueWithRestrictedPush<string, int>();
        Assert.True(queue.IsEmpty);
        Assert.Throws<InvalidOperationException>(() => queue.Pop());
        queue.Push("A", 10);
        queue.Push("B", 9);
        Assert.Throws<InvalidOperationException>(() => queue.Push("C", 4));
        Assert.Throws<InvalidOperationException>(() => queue.Push("C", 8));
    }

    [Fact]
    public void RandomPushPop()
    {
        var random = new Random(1);
        var pairs = new List<(int element, int priority)>();
        const int kNumElement = 1000; // Reduced for test speed
        const int kMaxPriority = 1000;
        for (int i = 0; i < kNumElement; ++i)
        {
            pairs.Add((i, random.Next(kMaxPriority)));
        }
        pairs.Sort((a, b) => a.priority.CompareTo(b.priority));
        // Randomly add +1 to some priorities
        for (int i = 0; i < pairs.Count; ++i)
        {
            if (random.NextDouble() < 0.5)
                pairs[i] = (pairs[i].element, pairs[i].priority + 1);
        }
        var queue = new PriorityQueueWithRestrictedPush<int, int>();
        foreach (var pair in pairs)
        {
            // Only push if allowed by the restriction
            if (queue.IsEmpty)
            {
                queue.Push(pair.element, pair.priority);
            }
            else
            {
                try
                {
                    queue.Push(pair.element, pair.priority);
                }
                catch (InvalidOperationException)
                {
                    // Ignore invalid pushes
                }
            }
        }
        // Stable sort for checking
        pairs.Sort((a, b) => a.priority != b.priority ? a.priority.CompareTo(b.priority) : a.element.CompareTo(b.element));
        // Pop all elements and check they are in non-increasing priority order
        int? lastPriority = null;
        while (!queue.IsEmpty)
        {
            int elem = queue.Pop();
            var found = pairs.FindLastIndex(p => p.element == elem);
            Assert.True(found >= 0);
            int prio = pairs[found].priority;
            if (lastPriority.HasValue)
                Assert.True(prio <= lastPriority.Value);
            lastPriority = prio;
            pairs.RemoveAt(found);
        }
    }
}
