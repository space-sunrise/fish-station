using Content.Server.Atmos.Components;
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

    // Простой baseline: экспоненциальное сглаживание.
    private float _baseAwake = 1f;
    private float _baseAtmos = 1f;
    private float _basePressure = 1f;

    public float LastTickMs { get; private set; }
    public float LastTickBudgetMs { get; private set; }
    public float LastTps { get; private set; }
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

    public void Sample()
    {
        _sw.Restart();

        LastTickBudgetMs = (float)_timing.TickPeriod.TotalMilliseconds;
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
        var sampleCost = (float)_sw.Elapsed.TotalMilliseconds;
        // Давление без обёртки всего тика: sample + эвристика physics/atmos.
        LastTickMs = Math.Max(sampleCost, AwakeBodies * 0.002f + AtmosActive * 0.0004f);
        LastTickMs = Math.Min(LastTickMs, LastTickBudgetMs * 3f);

        var targetTps = LastTickBudgetMs > 0.001f ? 1000f / LastTickBudgetMs : 0f;
        LastTps = LastTickBudgetMs > 0.001f
            ? Math.Clamp(1000f / Math.Max(LastTickMs, LastTickBudgetMs * 0.25f), 0f, targetTps * 1.05f)
            : 0f;

        // Baseline обновляем медленно.
        const float alpha = 0.08f;
        _baseAwake = Lerp(_baseAwake, Math.Max(1f, AwakeBodies), alpha);
        _baseAtmos = Lerp(_baseAtmos, Math.Max(1f, AtmosActive), alpha);
        var pressure = LastTickBudgetMs > 0.001f ? LastTickMs / LastTickBudgetMs : 1f;
        _basePressure = Lerp(_basePressure, Math.Max(0.5f, pressure), alpha);
    }

    public float PressureRatio =>
        LastTickBudgetMs > 0.001f ? LastTickMs / LastTickBudgetMs : 1f;

    public float AwakeSpike => AwakeBodies / Math.Max(1f, _baseAwake);
    public float AtmosSpike => AtmosActive / Math.Max(1f, _baseAtmos);

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
