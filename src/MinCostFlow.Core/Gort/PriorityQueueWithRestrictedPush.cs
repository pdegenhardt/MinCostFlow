using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// A specialized priority queue where elements can only be pushed with priority ≥ (highest_priority - 1).
/// All operations are O(1). Elements with the same priority are retrieved in LIFO order.
/// Uses two internal queues for even/odd priorities.
/// </summary>
/// <typeparam name="TElement">The type of elements in the queue</typeparam>
/// <typeparam name="TPriority">The priority type (must be an integer type)</typeparam>
public class PriorityQueueWithRestrictedPush<TElement, TPriority> 
    where TPriority : struct, IComparable<TPriority>
{
    private readonly List<(TElement element, TPriority priority)> _evenQueue;
    private readonly List<(TElement element, TPriority priority)> _oddQueue;
    private readonly Func<TPriority, int> _toInt;

    /// <summary>
    /// Creates a new priority queue with restricted push.
    /// </summary>
    public PriorityQueueWithRestrictedPush()
    {
        _evenQueue = new List<(TElement, TPriority)>();
        _oddQueue = new List<(TElement, TPriority)>();

        // Set up conversion functions based on the priority type
        if (typeof(TPriority) == typeof(int))
        {
            _toInt = p => (int)(object)p!;
        }
        else if (typeof(TPriority) == typeof(uint))
        {
            _toInt = p => (int)(uint)(object)p!;
        }
        else if (typeof(TPriority) == typeof(short))
        {
            _toInt = p => (short)(object)p!;
        }
        else if (typeof(TPriority) == typeof(ushort))
        {
            _toInt = p => (ushort)(object)p!;
        }
        else
        {
            throw new NotSupportedException($"Priority type {typeof(TPriority)} is not supported. Use int, uint, short, or ushort.");
        }
    }

    /// <summary>
    /// Returns true if the queue is empty.
    /// </summary>
    public bool IsEmpty() => _evenQueue.Count == 0 && _oddQueue.Count == 0;

    /// <summary>
    /// Removes all elements from the queue.
    /// </summary>
    public void Clear()
    {
        _evenQueue.Clear();
        _oddQueue.Clear();
    }

    /// <summary>
    /// Adds an element with the given priority.
    /// Priority must be ≥ (highest_priority - 1).
    /// </summary>
    public void Push(TElement element, TPriority priority)
    {
        int priorityInt = _toInt(priority);
        
        // Validate restriction against actual queue contents (following OR-Tools pattern)
        if (_evenQueue.Count > 0)
        {
            int evenBackPriority = _toInt(_evenQueue[_evenQueue.Count - 1].priority);
            if (priorityInt < evenBackPriority - 1)
            {
                throw new InvalidOperationException(
                    $"Priority {priorityInt} is less than (even_queue highest_priority - 1) = {evenBackPriority - 1}. " +
                    $"Elements can only be pushed with priority ≥ {evenBackPriority - 1}.");
            }
        }
        
        if (_oddQueue.Count > 0)
        {
            int oddBackPriority = _toInt(_oddQueue[_oddQueue.Count - 1].priority);
            if (priorityInt < oddBackPriority - 1)
            {
                throw new InvalidOperationException(
                    $"Priority {priorityInt} is less than (odd_queue highest_priority - 1) = {oddBackPriority - 1}. " +
                    $"Elements can only be pushed with priority ≥ {oddBackPriority - 1}.");
            }
        }

        // Add to appropriate queue based on parity (following OR-Tools logic)
        if ((priorityInt & 1) == 1) // Odd priority
        {
            // Additional check: priority must be >= last odd priority (OR-Tools line 562)
            if (_oddQueue.Count > 0)
            {
                int lastOddPriority = _toInt(_oddQueue[_oddQueue.Count - 1].priority);
                if (priorityInt < lastOddPriority)
                {
                    throw new InvalidOperationException(
                        $"Priority {priorityInt} is less than last odd priority {lastOddPriority}. " +
                        $"Queue order would be violated.");
                }
            }
            _oddQueue.Add((element, priority));
        }
        else // Even priority
        {
            // Additional check: priority must be >= last even priority (OR-Tools line 565)
            if (_evenQueue.Count > 0)
            {
                int lastEvenPriority = _toInt(_evenQueue[_evenQueue.Count - 1].priority);
                if (priorityInt < lastEvenPriority)
                {
                    throw new InvalidOperationException(
                        $"Priority {priorityInt} is less than last even priority {lastEvenPriority}. " +
                        $"Queue order would be violated.");
                }
            }
            _evenQueue.Add((element, priority));
        }
    }

    /// <summary>
    /// Removes and returns the element with the highest priority.
    /// </summary>
    public TElement Pop()
    {
        if (IsEmpty())
        {
            throw new InvalidOperationException("Cannot pop from an empty queue.");
        }

        // Follow OR-Tools logic exactly (lines 571-580)
        if (_evenQueue.Count == 0)
        {
            return PopBack(_oddQueue);
        }
        
        if (_oddQueue.Count == 0)
        {
            return PopBack(_evenQueue);
        }
        
        // Both queues have elements, compare priorities at the back
        var evenBackPriority = _evenQueue[_evenQueue.Count - 1].priority;
        var oddBackPriority = _oddQueue[_oddQueue.Count - 1].priority;
        
        if (oddBackPriority.CompareTo(evenBackPriority) > 0)
        {
            return PopBack(_oddQueue);
        }
        else
        {
            return PopBack(_evenQueue);
        }
    }

    /// <summary>
    /// Helper function to pop the last element from a queue.
    /// </summary>
    private TElement PopBack(List<(TElement element, TPriority priority)> queue)
    {
        if (queue.Count == 0)
        {
            throw new InvalidOperationException("Cannot pop from empty queue.");
        }
        
        var element = queue[queue.Count - 1].element;
        queue.RemoveAt(queue.Count - 1);
        return element;
    }

    /// <summary>
    /// Returns the number of elements in the queue.
    /// </summary>
    public int Count => _evenQueue.Count + _oddQueue.Count;
}