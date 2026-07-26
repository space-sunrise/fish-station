using Content.Server.Atmos.Components;
using Content.Shared._Fish.PerformanceGuardian;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Cheap periodic gauges. No gameplay analysis.
/// </summary>
public sealed class PgServerSampler
{
    private readonly IEntityManager _entities;
    private readonly IGameTiming _timing;
    private readonly ISharedPlayerManager _players;
    private readonly SharedPhysicsSystem _physics;

    private readonly Stopwatch _sw = new();
    private TimeSpan _lastSampleAt;
    private float _lastTickMs;

    public PgServerSampler(
        IEntityManager entities,
        IGameTiming timing,
        ISharedPlayerManager players,
        SharedPhysicsSystem physics)
    {
        _entities = entities;
        _timing = timing;
        _players = players;
        _physics = physics;
    }

    public float LastTickMs => _lastTickMs;

    public PgSamplePoint Sample(PgLoadLevel load, float riskScore)
    {
        _sw.Restart();

        var budgetMs = (float)_timing.TickPeriod.TotalMilliseconds;
        var entityCount = _entities.EntityCount;
        var gridCount = _entities.Count<MapGridComponent>();
        var awake = _physics.AwakeBodies.Count;
        var players = _players.PlayerCount;

        var atmosActive = 0;
        var atmosHot = 0;
        var atmosExcited = 0;

        // O(grids) — never O(tiles)
        var query = _entities.AllEntityQueryEnumerator<GridAtmosphereComponent>();
        while (query.MoveNext(out _, out var atmos))
        {
            atmosActive += atmos.ActiveTilesCount;
            atmosHot += atmos.HotspotTilesCount;
            atmosExcited += atmos.ExcitedGroupCount;
        }

        var mem = GC.GetTotalMemory(false);
        var g0 = GC.CollectionCount(0);
        var g1 = GC.CollectionCount(1);
        var g2 = GC.CollectionCount(2);

        _sw.Stop();
        // Measured sample cost stands in for tick pressure proxy between full-frame hooks.
        _lastTickMs = Math.Max((float)_sw.Elapsed.TotalMilliseconds, EstimateTickPressure(budgetMs, awake, atmosActive));
        _lastSampleAt = _timing.CurTime;

        var tps = budgetMs > 0.001f ? 1000f / Math.Max(_lastTickMs, budgetMs * 0.25f) : 0f;
        // Clamp reported TPS to a sensible band around tick rate.
        var targetTps = budgetMs > 0.001f ? 1000f / budgetMs : 0f;
        tps = Math.Clamp(tps, 0f, targetTps * 1.05f);

        return new PgSamplePoint
        {
            At = _timing.CurTime,
            TickMs = _lastTickMs,
            TickBudgetMs = budgetMs,
            Tps = tps,
            EntityCount = entityCount,
            GridCount = gridCount,
            AwakeBodies = awake,
            AtmosActiveTiles = atmosActive,
            AtmosHotspots = atmosHot,
            AtmosExcitedGroups = atmosExcited,
            GcMemoryBytes = mem,
            GcGen0 = g0,
            GcGen1 = g1,
            GcGen2 = g2,
            PlayerCount = players,
            LoadLevel = load,
            RiskScore = riskScore,
        };
    }

    /// <summary>
    /// Combines sample stopwatch with relative load proxies so adaptive thresholds react
    /// without wrapping the entire gameplay tick (no engine patch).
    /// </summary>
    private static float EstimateTickPressure(float budgetMs, int awake, int atmosActive)
    {
        // Heuristic pressure units mapped into milliseconds of "virtual" tick cost.
        var pressure = awake * 0.002f + atmosActive * 0.0004f;
        return Math.Min(budgetMs * 3f, pressure);
    }
}
