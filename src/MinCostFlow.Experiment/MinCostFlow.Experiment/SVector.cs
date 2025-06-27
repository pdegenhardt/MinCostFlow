using System.Diagnostics;

namespace MinCostFlow.Experiment;

// Simplified SVector implementation for signed indices
public class SVector<TIndex, T>
    where TIndex : struct, IComparable<TIndex>
{
    private List<T> _data;
    private int _size;
    private int _capacity;

    public SVector()
    {
        _data = [];
        _size = 0;
        _capacity = 0;
    }

    public T this[TIndex index]
    {
        get
        {
            var idx = Convert.ToInt32(index);
            if (idx < -_size || idx >= _size)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {idx} is out of range for SVector of size {_size}.");
            return _data[_size + idx];
        }
        set
        {
            var idx = Convert.ToInt32(index);
            if (idx < -_size || idx >= _size)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {idx} is out of range for SVector of size {_size}.");
            _data[_size + idx] = value;
        }
    }

    public void Grow(T left, T right)
    {
        if (_size == _capacity)
        {
            Reserve(NewCapacity(1));
        }
        _data.Insert(0, left);
        _data.Add(right);
        _size++;
    }

    public void Reserve(int newCapacity)
    {
        if (newCapacity <= _capacity) return;

        var newData = new List<T>(2 * newCapacity);
        for (int i = 0; i < newCapacity - _size; i++)
        {
            newData.Add(default(T));
        }

        // Copy existing data
        for (int i = -_size; i < _size; i++)
        {
            if (_size > 0 && Math.Abs(i) < _size)
            {
                newData[newCapacity + i] = _data[_size + i];
            }
        }

        for (int i = 0; i < newCapacity - _size; i++)
        {
            newData.Add(default(T));
        }

        _data = newData;
        _capacity = newCapacity;
    }

    public void Resize(int newSize)
    {
        Reserve(newSize);
        // Adjust data list size
        while (_data.Count < 2 * newSize)
        {
            _data.Add(default(T));
        }
        while (_data.Count > 2 * newSize)
        {
            _data.RemoveAt(_data.Count - 1);
        }
        _size = newSize;
    }

    public void Clear()
    {
        Resize(0);
    }

    public int Size => _size;
    public int Capacity => _capacity;

    private int NewCapacity(int delta)
    {
        int candidate = (int)(1.3 * _capacity);
        if (candidate > _capacity + delta)
        {
            return candidate;
        }
        return _capacity + delta;
    }
}
