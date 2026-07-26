namespace Content.Shared._Fish.PerformanceGuardian;

/// <summary>
/// Online Welford mean/variance for anomaly z-scores without storing all samples.
/// </summary>
public struct PgWelfordAccumulator
{
    public long Count;
    public double Mean;
    public double M2;

    public void Add(double value)
    {
        Count++;
        var delta = value - Mean;
        Mean += delta / Count;
        var delta2 = value - Mean;
        M2 += delta * delta2;
    }

    public readonly double Variance => Count > 1 ? M2 / (Count - 1) : 0.0;

    public readonly double StdDev
    {
        get
        {
            var v = Variance;
            return v > 0 ? Math.Sqrt(v) : 0.0;
        }
    }

    public readonly float ZScore(double value)
    {
        var sd = StdDev;
        if (sd <= 1e-9 || Count < 8)
            return 0f;

        return (float)((value - Mean) / sd);
    }

    public void Reset()
    {
        Count = 0;
        Mean = 0;
        M2 = 0;
    }
}
