using Robust.Shared.Serialization;

namespace Content.Shared._Fish.PerformanceGuardian;

/// <summary>
/// Режим работы монитора: почти ничего / идёт инцидент.
/// </summary>
[Serializable, NetSerializable]
public enum PgMode : byte
{
    Idle = 0,
    Incident = 1,
}

/// <summary>
/// Какая подсистема сейчас главная по нагрузке (простыми словами).
/// </summary>
[Serializable, NetSerializable]
public enum PgLoadSource : byte
{
    Unknown = 0,
    Physics = 1,
    Atmos = 2,
    Events = 3,
    Entities = 4,
    Ok = 5,
}

[Serializable, NetSerializable]
public sealed class PgEntityLoadRow
{
    public string Name = string.Empty;
    public string Detail = string.Empty;
}

[Serializable, NetSerializable]
public sealed class PgNearbyPlayerRow
{
    public string Name = string.Empty;
    public string Detail = string.Empty;
}

/// <summary>
/// Единый снимок для админ-окна: всё нужное на одном экране.
/// </summary>
[Serializable, NetSerializable]
public sealed class PgReport
{
    public TimeSpan ServerTime;
    public PgMode Mode;

    /// <summary>Краткий статус: «Норма» / «Нагрузка» / «Критично».</summary>
    public string ServerState = string.Empty;

    public float Tps;
    public float TickMs;
    public float TickBudgetMs;
    public int EntityCount;
    public int GridCount;
    public int AwakeBodies;
    public int AtmosActiveTiles;
    public int AtmosHotspots;
    public int PlayerCount;
    public int EventRatePerSec;

    public PgLoadSource PrimarySource;
    public string PrimarySourceText = string.Empty;

    public string PlaceName = string.Empty;
    public string CoordinatesText = string.Empty;

    public List<PgEntityLoadRow> TopEntities = new();
    public List<PgNearbyPlayerRow> NearbyPlayers = new();

    public string LastIncidentSummary = string.Empty;
    public TimeSpan? LastIncidentAt;
    public string Recommendation = string.Empty;

    public bool DiagnosisAvailable;
}
