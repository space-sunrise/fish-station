using System.Runtime.CompilerServices;

namespace Content.Shared._Fish.PerformanceGuardian;

/// <summary>
/// Array-backed category counters for the hot path. No heap growth after construction.
/// </summary>
public sealed class PgCounterBag
{
    private readonly int[] _counts = new int[(int)PgMetricCategory.Count];
    private readonly int[] _prev = new int[(int)PgMetricCategory.Count];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Increment(PgMetricCategory category)
    {
        var i = (int)category;
        if ((uint)i >= (uint)_counts.Length)
            return;
        _counts[i]++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Increment(PgMetricCategory category, int amount)
    {
        var i = (int)category;
        if ((uint)i >= (uint)_counts.Length || amount <= 0)
            return;
        _counts[i] += amount;
    }

    public int Get(PgMetricCategory category)
    {
        var i = (int)category;
        return (uint)i < (uint)_counts.Length ? _counts[i] : 0;
    }

    /// <summary>
    /// Returns per-category deltas since last snapshot and advances the baseline.
    /// </summary>
    public void TakeRates(Span<int> destination)
    {
        var n = Math.Min(destination.Length, _counts.Length);
        for (var i = 0; i < n; i++)
        {
            destination[i] = _counts[i] - _prev[i];
            _prev[i] = _counts[i];
        }
    }

    public void CopyTotals(Span<int> destination)
    {
        var n = Math.Min(destination.Length, _counts.Length);
        for (var i = 0; i < n; i++)
            destination[i] = _counts[i];
    }

    public void Reset()
    {
        Array.Clear(_counts, 0, _counts.Length);
        Array.Clear(_prev, 0, _prev.Length);
    }
}
