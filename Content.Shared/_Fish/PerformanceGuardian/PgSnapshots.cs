using Robust.Shared.Serialization;

namespace Content.Shared._Fish.PerformanceGuardian;

/// <summary>
/// Compact server sample used in rings and black box (Shared-serializable).
/// </summary>
[Serializable, NetSerializable]
public struct PgSamplePoint
{
    public TimeSpan At;
    public float TickMs;
    public float TickBudgetMs;
    public float Tps;
    public int EntityCount;
    public int GridCount;
    public int AwakeBodies;
    public int AtmosActiveTiles;
    public int AtmosHotspots;
    public int AtmosExcitedGroups;
    public long GcMemoryBytes;
    public int GcGen0;
    public int GcGen1;
    public int GcGen2;
    public int PlayerCount;
    public PgLoadLevel LoadLevel;
    public float RiskScore;
    public float EventRatePerSec;
}

[Serializable, NetSerializable]
public sealed class PgPlayerRow
{
    public string Name = string.Empty;
    public string UserId = string.Empty;
    public float EventsPerSec10s;
    public float EventsPerSec1m;
    public float EventsPerSec5m;
    public float RiskScore;
    public int AttackCount;
    public int DamageCount;
    public int ThrowCount;
    public int ProjectileCount;
}

[Serializable, NetSerializable]
public sealed class PgAlert
{
    public int Id;
    public TimeSpan At;
    public PgAlertSeverity Severity;
    public string Title = string.Empty;
    public string Detail = string.Empty;
    public string Metric = string.Empty;
    public float Value;
    public float Baseline;
}

[Serializable, NetSerializable]
public sealed class PgReportSummary
{
    public int Id;
    public TimeSpan At;
    public PgAlertSeverity Severity;
    public string Title = string.Empty;
    public string Summary = string.Empty;
    public float RiskScore;
    public PgLoadLevel LoadAtIncident;
}

[Serializable, NetSerializable]
public sealed class PgTimelineEvent
{
    public TimeSpan At;
    public string Category = string.Empty;
    public string Message = string.Empty;
    public PgAlertSeverity Severity;
}

[Serializable, NetSerializable]
public sealed class PgHeatRow
{
    public string Name = string.Empty;
    public float RatePerSec;
    public float Share;
}

[Serializable, NetSerializable]
public sealed class PgTopRow
{
    public string Name = string.Empty;
    public float Score;
    public string Detail = string.Empty;
}

[Serializable, NetSerializable]
public sealed class PgServerSnapshot
{
    public TimeSpan ServerTime;
    public PgLoadLevel LoadLevel;
    public float RiskScore;
    public float TickMs;
    public float TickBudgetMs;
    public float Tps;
    public int EntityCount;
    public int GridCount;
    public int AwakeBodies;
    public int AtmosActiveTiles;
    public int AtmosHotspots;
    public int AtmosExcitedGroups;
    public long GcMemoryBytes;
    public int PlayerCount;
    public float AnalyzerBudgetUsedMs;
    public bool BlackBoxFrozen;
    public int[] CategoryRates = Array.Empty<int>();
    public List<PgPlayerRow> Players = new();
    public List<PgAlert> Alerts = new();
    public List<PgReportSummary> Reports = new();
    public List<PgTimelineEvent> Timeline = new();
    public List<PgHeatRow> HeatMap = new();
    public List<PgTopRow> TopEntities = new();
    public List<PgTopRow> TopSystems = new();
    public List<PgSamplePoint> History = new();
    public string ProfilerNote = string.Empty;
    public float CorrTickVsAtmos;
    public float CorrTickVsAwake;
    public float CorrTickVsEvents;
}
