using System.Diagnostics;
using System.Numerics;

namespace MinCostFlow.Experiment;

/// <summary>
/// Priority queue implementation with restricted push operations.
/// The priority type must be an integer. The queue allows retrieval of the element
/// with highest priority but only allows pushes with a priority greater or equal
/// to the highest priority in the queue minus one.
/// </summary>
public class PriorityQueueWithRestrictedPush<TElement, TPriority>
    where TPriority : INumber<TPriority>
{
    private readonly List<KeyValuePair<TElement, TPriority>> _evenQueue = [];
    private readonly List<KeyValuePair<TElement, TPriority>> _oddQueue = [];

    public bool IsEmpty => _evenQueue.Count == 0 && _oddQueue.Count == 0;

    public void Clear()
    {
        _evenQueue.Clear();
        _oddQueue.Clear();
    }

    public void Push(TElement element, TPriority priority)
    {
        if (_evenQueue.Count > 0 && priority < _evenQueue[^1].Value - TPriority.One)
        {
            throw new InvalidOperationException($"Cannot push with priority {priority}, must be >= {_evenQueue[^1].Value} - 1.");
        }
        if (_oddQueue.Count > 0 && priority < _oddQueue[^1].Value - TPriority.One)
        {
            throw new InvalidOperationException($"Cannot push with priority {priority}, must be >= {_oddQueue[^1].Value} -1.");
        }

        if (TPriority.IsOddInteger(priority))
        {
            // Odd queue
            if (_oddQueue.Count > 0)
            {
                var lastPriority = _oddQueue[^1].Value;
                if (priority < lastPriority)
                {
                    throw new InvalidOperationException($"Cannot push with priority {priority}, must be >= {_oddQueue[^1].Value} - 1.");
                }
            }
            _oddQueue.Add(new KeyValuePair<TElement, TPriority>(element, priority));
        }
        else
        {
            // Even queue
            if (_evenQueue.Count > 0)
            {
                var lastPriority = _evenQueue[^1].Value;
                if (priority < lastPriority - TPriority.One)
                {
                    throw new InvalidOperationException($"Cannot push with priority {priority}, must be >= {_evenQueue[^1].Value} - 1.");
                }
            }
            _evenQueue.Add(new KeyValuePair<TElement, TPriority>(element, priority));
        }
    }

    public TElement Pop()
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Cannot pop from an empty priority queue.");
        }

        if (_evenQueue.Count == 0)
        {
            return PopBack(_oddQueue);
        }

        if (_oddQueue.Count == 0)
        {
            return PopBack(_evenQueue);
        }

        return _oddQueue[^1].Value > _evenQueue[^1].Value ? PopBack(_oddQueue) : PopBack(_evenQueue);
    }

    private static TElement PopBack(List<KeyValuePair<TElement, TPriority>> queue)
    {
        if (queue.Count == 0)
        {
            throw new InvalidOperationException("Cannot pop from an empty queue.");
        }

        var element = queue[^1].Key;
        queue.RemoveAt(queue.Count - 1);
        return element;
    }
}
