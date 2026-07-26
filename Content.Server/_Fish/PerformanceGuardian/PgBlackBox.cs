using Content.Shared._Fish.PerformanceGuardian;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Ring of recent aggregates; freeze snapshot on severe incident.
/// </summary>
public sealed class PgBlackBox
{
    private PgRingBuffer<PgSamplePoint> _ring;
    private PgSamplePoint[]? _frozen;
    private bool _frozenFlag;

    public bool IsFrozen => _frozenFlag;

    public PgBlackBox(int capacity)
    {
        _ring = new PgRingBuffer<PgSamplePoint>(Math.Max(8, capacity));
    }

    public void Resize(int capacity)
    {
        if (_frozenFlag)
            return;
        _ring.EnsureCapacity(Math.Max(8, capacity));
    }

    public void Append(in PgSamplePoint sample)
    {
        if (_frozenFlag)
            return;
        _ring.Push(sample);
    }

    public void Freeze()
    {
        if (_frozenFlag)
            return;

        _frozen = new PgSamplePoint[_ring.Count];
        for (var i = 0; i < _ring.Count; i++)
            _frozen[i] = _ring.Get(i);
        _frozenFlag = true;
    }

    public void Unfreeze()
    {
        _frozenFlag = false;
        _frozen = null;
    }

    public void CopyHistory(List<PgSamplePoint> destination, int max)
    {
        destination.Clear();
        if (_frozenFlag && _frozen != null)
        {
            var n = Math.Min(max, _frozen.Length);
            for (var i = Math.Max(0, _frozen.Length - n); i < _frozen.Length; i++)
                destination.Add(_frozen[i]);
            return;
        }

        var take = Math.Min(max, _ring.Count);
        var start = _ring.Count - take;
        for (var i = start; i < _ring.Count; i++)
            destination.Add(_ring.Get(i));
    }
}
