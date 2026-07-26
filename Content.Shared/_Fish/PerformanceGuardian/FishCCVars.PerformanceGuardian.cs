using Robust.Shared.Configuration;

namespace Content.Shared._Fish.PerformanceGuardian;

public sealed partial class FishCCVars
{
    /// <summary>
    /// Master switch for Performance Guardian sampling and analysis.
    /// </summary>
    public static readonly CVarDef<bool> PgEnabled =
        CVarDef.Create("pg.enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Interval between cheap server samples (seconds).
    /// </summary>
    public static readonly CVarDef<float> PgSampleIntervalSeconds =
        CVarDef.Create("pg.sample_interval_seconds", 1.0f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Interval between aggregator/analyzer cycles at Full load (seconds).
    /// </summary>
    public static readonly CVarDef<float> PgAnalyzeIntervalSeconds =
        CVarDef.Create("pg.analyze_interval_seconds", 2.0f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Hard CPU budget per aggregator/analyzer pass (milliseconds).
    /// </summary>
    public static readonly CVarDef<float> PgCpuBudgetMs =
        CVarDef.Create("pg.cpu_budget_ms", 2.0f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Tick overrun ratio that enters Reduced load (measured/budget).
    /// </summary>
    public static readonly CVarDef<float> PgLoadReducedThreshold =
        CVarDef.Create("pg.load_reduced_threshold", 1.15f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Tick overrun ratio that enters Degraded load.
    /// </summary>
    public static readonly CVarDef<float> PgLoadDegradedThreshold =
        CVarDef.Create("pg.load_degraded_threshold", 1.4f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Tick overrun ratio that enters Critical load.
    /// </summary>
    public static readonly CVarDef<float> PgLoadCriticalThreshold =
        CVarDef.Create("pg.load_critical_threshold", 1.8f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Max players tracked in rolling profiles.
    /// </summary>
    public static readonly CVarDef<int> PgMaxPlayersTracked =
        CVarDef.Create("pg.max_players_tracked", 128, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Black-box ring capacity (aggregate samples).
    /// </summary>
    public static readonly CVarDef<int> PgBlackBoxSize =
        CVarDef.Create("pg.black_box_size", 120, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Max stored auto-reports.
    /// </summary>
    public static readonly CVarDef<int> PgMaxReports =
        CVarDef.Create("pg.max_reports", 32, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Max active alerts retained.
    /// </summary>
    public static readonly CVarDef<int> PgMaxAlerts =
        CVarDef.Create("pg.max_alerts", 64, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Client snapshot request interval while window is open (seconds).
    /// </summary>
    public static readonly CVarDef<float> PgUiRefreshSeconds =
        CVarDef.Create("pg.ui_refresh_seconds", 1.5f, CVar.REPLICATED | CVar.ARCHIVE);
}
