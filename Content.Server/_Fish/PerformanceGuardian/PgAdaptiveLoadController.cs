using Content.Shared._Fish.PerformanceGuardian;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Adaptive load controller — updates from sampler ratios; never amplifies lag.
/// </summary>
public sealed class PgAdaptiveLoadController
{
    public PgLoadLevel Level { get; private set; } = PgLoadLevel.Full;

    private float _reduced = 1.15f;
    private float _degraded = 1.4f;
    private float _critical = 1.8f;

    public void Configure(float reduced, float degraded, float critical)
    {
        _reduced = reduced;
        _degraded = degraded;
        _critical = critical;
    }

    public PgLoadLevel Update(float tickMs, float budgetMs)
    {
        if (budgetMs <= 0.001f)
        {
            Level = PgLoadLevel.Full;
            return Level;
        }

        var ratio = tickMs / budgetMs;
        if (ratio >= _critical)
            Level = PgLoadLevel.Critical;
        else if (ratio >= _degraded)
            Level = PgLoadLevel.Degraded;
        else if (ratio >= _reduced)
            Level = PgLoadLevel.Reduced;
        else
            Level = PgLoadLevel.Full;

        return Level;
    }

    public bool AllowSecondaryCollectors => Level is PgLoadLevel.Full or PgLoadLevel.Reduced;
    public bool AllowAnalyzer => Level is PgLoadLevel.Full or PgLoadLevel.Reduced;
    public bool EssentialOnly => Level is PgLoadLevel.Degraded or PgLoadLevel.Critical;
    public bool FreezeBlackBox => Level == PgLoadLevel.Critical;

    public float AnalyzerIntervalMultiplier => Level switch
    {
        PgLoadLevel.Full => 1f,
        PgLoadLevel.Reduced => 2f,
        PgLoadLevel.Degraded => 4f,
        PgLoadLevel.Critical => 999f,
        _ => 1f,
    };
}
