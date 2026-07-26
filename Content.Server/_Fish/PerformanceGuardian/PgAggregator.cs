using Content.Shared._Fish.PerformanceGuardian;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Merges samples into fixed rolling windows and Welford stats.
/// </summary>
public sealed class PgAggregator
{
    public PgRingBuffer<PgSamplePoint> Window10s;
    public PgRingBuffer<PgSamplePoint> Window1m;
    public PgRingBuffer<PgSamplePoint> Window5m;

    public PgWelfordAccumulator TickMs;
    public PgWelfordAccumulator AtmosActive;
    public PgWelfordAccumulator AwakeBodies;
    public PgWelfordAccumulator EventRate;

    public int[] LastRates = new int[(int)PgMetricCategory.Count];
    public float LastEventRatePerSec;

    private int _cursor;

    public PgAggregator()
    {
        Window10s = new PgRingBuffer<PgSamplePoint>(12);
        Window1m = new PgRingBuffer<PgSamplePoint>(60);
        Window5m = new PgRingBuffer<PgSamplePoint>(60);
    }

    public void PushSample(in PgSamplePoint sample, ReadOnlySpan<int> rates)
    {
        Window10s.Push(sample);
        Window1m.Push(sample);

        // 5m window stores ~every 5th sample when sampling at 1Hz
        if ((_cursor++ % 5) == 0)
            Window5m.Push(sample);

        TickMs.Add(sample.TickMs);
        AtmosActive.Add(sample.AtmosActiveTiles);
        AwakeBodies.Add(sample.AwakeBodies);

        var n = Math.Min(LastRates.Length, rates.Length);
        for (var i = 0; i < n; i++)
            LastRates[i] = rates[i];

        LastEventRatePerSec = sample.EventRatePerSec;
        EventRate.Add(LastEventRatePerSec);
    }

    /// <summary>
    /// Pearson-like correlation over the last N overlapping points (bounded).
    /// </summary>
    public float CorrelateTickVs(Func<PgSamplePoint, float> selector, int maxPoints = 30)
    {
        var count = Math.Min(maxPoints, Window1m.Count);
        if (count < 8)
            return 0f;

        double sumX = 0, sumY = 0, sumXX = 0, sumYY = 0, sumXY = 0;
        var start = Window1m.Count - count;
        for (var i = start; i < Window1m.Count; i++)
        {
            var p = Window1m.Get(i);
            double x = p.TickMs;
            double y = selector(p);
            sumX += x;
            sumY += y;
            sumXX += x * x;
            sumYY += y * y;
            sumXY += x * y;
        }

        var n = (double)count;
        var num = n * sumXY - sumX * sumY;
        var den = Math.Sqrt((n * sumXX - sumX * sumX) * (n * sumYY - sumY * sumY));
        if (den < 1e-9)
            return 0f;
        return (float)Math.Clamp(num / den, -1.0, 1.0);
    }
}
