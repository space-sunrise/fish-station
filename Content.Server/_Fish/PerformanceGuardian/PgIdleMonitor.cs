using Content.Shared.Atmos.Components;
using Content.Shared._Fish.PerformanceGuardian;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Дешёвые idle-счётчики. Без анализа сущностей и без поиска игроков.
/// </summary>
public sealed class PgIdleMonitor
{
    private readonly IEntityManager _entities;
    private readonly IGameTiming _timing;
    private readonly ISharedPlayerManager _players;
    private readonly SharedPhysicsSystem _physics;
    private readonly Stopwatch _sw = new();

    public float LastSampleCostMs { get; private set; }
    public float LastTickBudgetMs { get; private set; }
    public float LastMaxFrameTime { get; private set; }
    public float LastTpsEstimate { get; private set; }
    public int EntityCount { get; private set; }
    public int GridCount { get; private set; }
    public int AwakeBodies { get; private set; }
    public int AtmosActive { get; private set; }
    public int AtmosHotspots { get; private set; }
    public int PlayerCount { get; private set; }

    public PgIdleMonitor(
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

    /// <param name="maxFrameTimeSeconds">Макс. frameTime с прошлого сэмпла — реальный сигнал отставания цикла.</param>
    public void Sample(float maxFrameTimeSeconds)
    {
        _sw.Restart();

        LastTickBudgetMs = (float)_timing.TickPeriod.TotalMilliseconds;
        LastMaxFrameTime = maxFrameTimeSeconds;
        EntityCount = _entities.EntityCount;
        GridCount = _entities.Count<MapGridComponent>();
        AwakeBodies = _physics.AwakeBodies.Count;
        PlayerCount = _players.PlayerCount;

        var atmosActive = 0;
        var atmosHot = 0;
        var query = _entities.AllEntityQueryEnumerator<GridAtmosphereComponent>();
        while (query.MoveNext(out _, out var atmos))
        {
            atmosActive += atmos.ActiveTilesCount;
            atmosHot += atmos.HotspotTilesCount;
        }

        AtmosActive = atmosActive;
        AtmosHotspots = atmosHot;

        _sw.Stop();
        LastSampleCostMs = (float)_sw.Elapsed.TotalMilliseconds;

        // TPS-оценка только из реального frameTime, без «фейкового» pressure из atmos/physics.
        var frameMs = Math.Max(maxFrameTimeSeconds * 1000f, LastTickBudgetMs * 0.25f);
        var targetTps = LastTickBudgetMs > 0.001f ? 1000f / LastTickBudgetMs : 0f;
        LastTpsEstimate = LastTickBudgetMs > 0.001f
            ? Math.Clamp(1000f / frameMs, 0f, targetTps * 1.05f)
            : 0f;
    }
}
