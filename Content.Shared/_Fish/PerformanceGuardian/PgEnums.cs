using Robust.Shared.Serialization;

namespace Content.Shared._Fish.PerformanceGuardian;

[Serializable, NetSerializable]
public enum PgLoadLevel : byte
{
    Full = 0,
    Reduced = 1,
    Degraded = 2,
    Critical = 3,
}

[Serializable, NetSerializable]
public enum PgAlertSeverity : byte
{
    Info = 0,
    Warning = 1,
    Severe = 2,
    Critical = 3,
}

[Serializable, NetSerializable]
public enum PgMetricCategory : byte
{
    Attack = 0,
    Explosion = 1,
    Throw = 2,
    Projectile = 3,
    Damage = 4,
    Construction = 5,
    Shuttle = 6,
    Collision = 7,
    Count = 8,
}

[Serializable, NetSerializable]
public enum PgSnapshotSection : byte
{
    All = 0,
    Dashboard = 1,
    Performance = 2,
    Players = 3,
    Risk = 4,
    Timeline = 5,
    HeatMap = 6,
    TopEntities = 7,
    TopSystems = 8,
    Alerts = 9,
    Reports = 10,
    Profiler = 11,
    History = 12,
}
