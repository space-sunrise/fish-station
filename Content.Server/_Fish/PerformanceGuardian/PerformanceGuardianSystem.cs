using Content.Server.Administration.Managers;
using Content.Shared._Fish.PerformanceGuardian;
using Content.Shared.Administration;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Фасад: idle-мониторинг, диагностика по инциденту/кнопке, сеть для админов.
/// </summary>
public sealed class PerformanceGuardianSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _players = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PgCollectorSystem _collector = default!;

    private readonly HashSet<ICommonSession> _subscribers = new();

    private PgIdleMonitor _idle = default!;
    private PgDiagnostics _diagnostics = default!;

    private bool _enabled = true;
    private float _sampleInterval = 2f;
    private float _pressureThreshold = 1.35f;
    private float _atmosSpike = 1.6f;
    private float _physicsSpike = 1.6f;
    private float _nearbyRange = 16f;
    private int _topLimit = 8;
    private float _diagnoseBudgetMs = 3f;

    private TimeSpan _nextSample;
    private TimeSpan _incidentCooldownUntil;
    private PgMode _mode = PgMode.Idle;
    private int _eventRatePerSec;
    private float _eventsAccum;
    private TimeSpan _eventsWindowStart;

    private PgReport _report = new();

    /// <summary>
    /// Коллекторы выключаются на время инцидента, чтобы не усугублять лаг.
    /// </summary>
    public bool CollectorsEnabled => _enabled && _mode != PgMode.Incident;

    public override void Initialize()
    {
        base.Initialize();

        _idle = new PgIdleMonitor(EntityManager, _timing, _players, _physics);
        _diagnostics = new PgDiagnostics(EntityManager, _xform, _physics, _lookup, _players);

        Subs.CVar(_cfg, FishCCVars.PgEnabled, v => _enabled = v, true);
        Subs.CVar(_cfg, FishCCVars.PgSampleIntervalSeconds, v => _sampleInterval = Math.Max(1f, v), true);
        Subs.CVar(_cfg, FishCCVars.PgIncidentPressureThreshold, v => _pressureThreshold = v, true);
        Subs.CVar(_cfg, FishCCVars.PgIncidentAtmosSpike, v => _atmosSpike = v, true);
        Subs.CVar(_cfg, FishCCVars.PgIncidentPhysicsSpike, v => _physicsSpike = v, true);
        Subs.CVar(_cfg, FishCCVars.PgNearbyPlayerRange, v => _nearbyRange = v, true);
        Subs.CVar(_cfg, FishCCVars.PgTopEntityLimit, v => _topLimit = Math.Clamp(v, 1, 16), true);
        Subs.CVar(_cfg, FishCCVars.PgDiagnoseBudgetMs, v => _diagnoseBudgetMs = Math.Max(1f, v), true);

        SubscribeNetworkEvent<PgSubscribeRequest>(OnSubscribe);
        SubscribeNetworkEvent<PgUnsubscribeRequest>(OnUnsubscribe);
        SubscribeNetworkEvent<PgReportRequest>(OnReportRequest);
        SubscribeNetworkEvent<PgDiagnoseRequest>(OnDiagnoseRequest);

        _players.PlayerStatusChanged += OnPlayerStatusChanged;
        RefreshReportBasics();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        _subscribers.Clear();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            _subscribers.Remove(e.Session);
    }

    private bool IsDebugAdmin(ICommonSession session) =>
        _admins.HasAdminFlag(session, AdminFlags.Debug);

    private void OnSubscribe(PgSubscribeRequest msg, EntitySessionEventArgs args)
    {
        if (!IsDebugAdmin(args.SenderSession))
            return;

        _subscribers.Add(args.SenderSession);
        RaiseNetworkEvent(new PgReportResponse(CloneReport()), args.SenderSession);
    }

    private void OnUnsubscribe(PgUnsubscribeRequest msg, EntitySessionEventArgs args)
    {
        _subscribers.Remove(args.SenderSession);
    }

    private void OnReportRequest(PgReportRequest msg, EntitySessionEventArgs args)
    {
        if (!IsDebugAdmin(args.SenderSession))
            return;

        _subscribers.Add(args.SenderSession);
        RefreshReportBasics();
        RaiseNetworkEvent(new PgReportResponse(CloneReport()), args.SenderSession);
    }

    private void OnDiagnoseRequest(PgDiagnoseRequest msg, EntitySessionEventArgs args)
    {
        if (!IsDebugAdmin(args.SenderSession))
            return;

        RunDiagnostics(manual: true);
        RaiseNetworkEvent(new PgReportResponse(CloneReport()), args.SenderSession);
    }

    public override void Update(float frameTime)
    {
        if (!_enabled)
            return;

        var now = _timing.CurTime;
        if (now < _nextSample)
            return;

        _nextSample = now + TimeSpan.FromSeconds(_sampleInterval);
        _idle.Sample();

        var events = _collector.TakeEventCount();
        if (_eventsWindowStart == TimeSpan.Zero)
            _eventsWindowStart = now;

        _eventsAccum += events;
        var window = (float)(now - _eventsWindowStart).TotalSeconds;
        if (window >= 1f)
        {
            _eventRatePerSec = (int)(_eventsAccum / window);
            _eventsAccum = 0;
            _eventsWindowStart = now;
        }

        RefreshReportBasics();

        // Авто-диагностика только при всплеске и не чаще cooldown.
        if (_mode == PgMode.Incident && now >= _incidentCooldownUntil)
        {
            if (_idle.PressureRatio < _pressureThreshold * 0.85f &&
                _idle.AwakeSpike < _physicsSpike * 0.85f &&
                _idle.AtmosSpike < _atmosSpike * 0.85f)
            {
                _mode = PgMode.Idle;
                RefreshReportBasics();
            }
        }
        else if (_mode == PgMode.Idle && now >= _incidentCooldownUntil && ShouldTriggerIncident())
        {
            RunDiagnostics(manual: false);
        }

        // Лёгкий push только подписчикам (окно открыто).
        if (_subscribers.Count == 0)
            return;

        var payload = CloneReport();
        foreach (var session in _subscribers)
            RaiseNetworkEvent(new PgReportResponse(payload), session);
    }

    private bool ShouldTriggerIncident()
    {
        return _idle.PressureRatio >= _pressureThreshold
               || _idle.AwakeSpike >= _physicsSpike
               || _idle.AtmosSpike >= _atmosSpike;
    }

    private void RunDiagnostics(bool manual)
    {
        _mode = PgMode.Incident;
        _incidentCooldownUntil = _timing.CurTime + TimeSpan.FromSeconds(15);

        _diagnostics.Run(
            _idle,
            _eventRatePerSec,
            _diagnoseBudgetMs,
            _nearbyRange,
            _topLimit,
            out var source,
            out var sourceText,
            out var place,
            out var coords,
            out var top,
            out var nearby,
            out var recommendation);

        _report.PrimarySource = source;
        _report.PrimarySourceText = sourceText;
        _report.PlaceName = place;
        _report.CoordinatesText = coords;
        _report.TopEntities = top;
        _report.NearbyPlayers = nearby;
        _report.Recommendation = recommendation;
        _report.LastIncidentAt = _timing.CurTime;
        _report.LastIncidentSummary = manual
            ? $"Ручная диагностика: {sourceText}"
            : $"Авто-инцидент: {sourceText} на «{place}»";
        _report.DiagnosisAvailable = true;

        RefreshReportBasics();
    }

    private void RefreshReportBasics()
    {
        _report.ServerTime = _timing.CurTime;
        _report.Mode = _mode;
        _report.Tps = _idle.LastTps;
        _report.TickMs = _idle.LastTickMs;
        _report.TickBudgetMs = _idle.LastTickBudgetMs;
        _report.EntityCount = _idle.EntityCount;
        _report.GridCount = _idle.GridCount;
        _report.AwakeBodies = _idle.AwakeBodies;
        _report.AtmosActiveTiles = _idle.AtmosActive;
        _report.AtmosHotspots = _idle.AtmosHotspots;
        _report.PlayerCount = _idle.PlayerCount;
        _report.EventRatePerSec = _eventRatePerSec;
        _report.ServerState = DescribeState();

        if (_report.PrimarySource == PgLoadSource.Unknown && _mode == PgMode.Idle)
        {
            _report.PrimarySource = PgLoadSource.Ok;
            _report.PrimarySourceText = "Явной перегрузки нет";
            if (string.IsNullOrEmpty(_report.Recommendation))
                _report.Recommendation = "Сервер в норме. Нажмите «Диагностика сейчас», если лаги всё равно есть.";
        }
    }

    private string DescribeState()
    {
        if (_mode == PgMode.Incident)
            return "Инцидент — идёт разбор нагрузки";

        if (_idle.PressureRatio >= _pressureThreshold)
            return "Высокая нагрузка";

        if (_idle.PressureRatio >= 1.15f)
            return "Повышенная нагрузка";

        return "Норма";
    }

    private PgReport CloneReport()
    {
        // Лёгкая копия для сети (избегаем мутаций у клиента).
        return new PgReport
        {
            ServerTime = _report.ServerTime,
            Mode = _report.Mode,
            ServerState = _report.ServerState,
            Tps = _report.Tps,
            TickMs = _report.TickMs,
            TickBudgetMs = _report.TickBudgetMs,
            EntityCount = _report.EntityCount,
            GridCount = _report.GridCount,
            AwakeBodies = _report.AwakeBodies,
            AtmosActiveTiles = _report.AtmosActiveTiles,
            AtmosHotspots = _report.AtmosHotspots,
            PlayerCount = _report.PlayerCount,
            EventRatePerSec = _report.EventRatePerSec,
            PrimarySource = _report.PrimarySource,
            PrimarySourceText = _report.PrimarySourceText,
            PlaceName = _report.PlaceName,
            CoordinatesText = _report.CoordinatesText,
            TopEntities = new List<PgEntityLoadRow>(_report.TopEntities),
            NearbyPlayers = new List<PgNearbyPlayerRow>(_report.NearbyPlayers),
            LastIncidentSummary = _report.LastIncidentSummary,
            LastIncidentAt = _report.LastIncidentAt,
            Recommendation = _report.Recommendation,
            DiagnosisAvailable = _report.DiagnosisAvailable,
        };
    }

    public void HintOpenWindow(ICommonSession session)
    {
        if (!IsDebugAdmin(session))
            return;

        RaiseNetworkEvent(new PgOpenWindowHint(), session);
    }
}
