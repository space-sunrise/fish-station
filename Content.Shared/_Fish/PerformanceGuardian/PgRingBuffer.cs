using System.Runtime.CompilerServices;

namespace Content.Shared._Fish.PerformanceGuardian;

/// <summary>
/// Fixed-capacity ring buffer. No allocations after construction; no LINQ.
/// </summary>
public struct PgRingBuffer<T>
{
    private T[] _items;
    private int _head;
    private int _count;

    public PgRingBuffer(int capacity)
    {
        if (capacity < 1)
            capacity = 1;

        _items = new T[capacity];
        _head = 0;
        _count = 0;
    }

    public readonly int Capacity => _items.Length;
    public readonly int Count => _count;
    public readonly bool IsEmpty => _count == 0;
    public readonly bool IsFull => _count == _items.Length;

    public void EnsureCapacity(int capacity)
    {
        if (_items != null && _items.Length >= capacity)
            return;

        var next = new T[Math.Max(1, capacity)];
        if (_items != null && _count > 0)
        {
            for (var i = 0; i < _count; i++)
                next[i] = Get(i);
        }

        _items = next;
        _head = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(in T value)
    {
        var cap = _items.Length;
        var idx = (_head + _count) % cap;
        if (_count == cap)
        {
            _items[_head] = value;
            _head = (_head + 1) % cap;
        }
        else
        {
            _items[idx] = value;
            _count++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T Get(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return _items[(_head + index) % _items.Length];
    }

    public readonly T Latest()
    {
        if (_count == 0)
            throw new InvalidOperationException("Ring buffer is empty.");

        return Get(_count - 1);
    }

    public bool TryLatest(out T value)
    {
        if (_count == 0)
        {
            value = default!;
            return false;
        }

        value = Get(_count - 1);
        return true;
    }

    public void Clear()
    {
        if (_items == null)
            return;

        Array.Clear(_items, 0, _items.Length);
        _head = 0;
        _count = 0;
    }

    /// <summary>
    /// Copies chronological order into <paramref name="destination"/> (oldest first).
    /// Returns written count.
    /// </summary>
    public readonly int CopyTo(Span<T> destination)
    {
        var n = Math.Min(_count, destination.Length);
        for (var i = 0; i < n; i++)
            destination[i] = Get(i);
        return n;
    }
}
