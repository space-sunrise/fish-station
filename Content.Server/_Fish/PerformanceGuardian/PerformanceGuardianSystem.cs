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
/// Facade: sampling, adaptive load, aggregation, analysis, admin net gate.
/// </summary>
public sealed class PerformanceGuardianSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _players = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly HashSet<ICommonSession> _subscribers = new();
    private readonly int[] _rateScratch = new int[(int)PgMetricCategory.Count];
    private readonly List<PgSamplePoint> _historyScratch = new();

    private PgServerSampler _sampler = default!;
    private PgAggregator _aggregator = default!;
    private PgAnalyzer _analyzer = default!;
    private PgBlackBox _blackBox = default!;
    private PgReportStore _reports = default!;
    private PgAdaptiveLoadController _load = default!;

    private TimeSpan _nextSample;
    private TimeSpan _nextAnalyze;
    private TimeSpan _lastSampleAt;
    private bool _enabled = true;
    private float _sampleInterval = 1f;
    private float _analyzeInterval = 2f;
    private PgSamplePoint _latest;
    private PgAlert? _lastPushedAlert;

    public PgCounterBag Counters { get; } = new();
    public PgPlayerProfiles Profiles { get; private set; } = default!;
    public bool CollectorsEnabled => _enabled && !_load.EssentialOnly;
    public bool SecondaryCollectorsEnabled => _enabled && _load.AllowSecondaryCollectors;

    public override void Initialize()
    {
        base.Initialize();

        _load = new PgAdaptiveLoadController();
        _aggregator = new PgAggregator();
        _analyzer = new PgAnalyzer(_timing);
        _reports = new PgReportStore();
        _blackBox = new PgBlackBox(_cfg.GetCVar(FishCCVars.PgBlackBoxSize));
        Profiles = new PgPlayerProfiles(_cfg.GetCVar(FishCCVars.PgMaxPlayersTracked));
        _sampler = new PgServerSampler(EntityManager, _timing, _players, _physics);

        Subs.CVar(_cfg, FishCCVars.PgEnabled, v => _enabled = v, true);
        Subs.CVar(_cfg, FishCCVars.PgSampleIntervalSeconds, v => _sampleInterval = Math.Max(0.25f, v), true);
        Subs.CVar(_cfg, FishCCVars.PgAnalyzeIntervalSeconds, v => _analyzeInterval = Math.Max(0.5f, v), true);
        Subs.CVar(_cfg, FishCCVars.PgCpuBudgetMs, v => _analyzer.Configure(v, _cfg.GetCVar(FishCCVars.PgMaxAlerts)), true);
        Subs.CVar(_cfg, FishCCVars.PgMaxAlerts, v => _analyzer.Configure(_cfg.GetCVar(FishCCVars.PgCpuBudgetMs), v), true);
        Subs.CVar(_cfg, FishCCVars.PgMaxReports, v => _reports.Configure(v), true);
        Subs.CVar(_cfg, FishCCVars.PgBlackBoxSize, v => _blackBox.Resize(v), true);
        Subs.CVar(_cfg, FishCCVars.PgLoadReducedThreshold, _ => RefreshLoadThresholds(), true);
        Subs.CVar(_cfg, FishCCVars.PgLoadDegradedThreshold, _ => RefreshLoadThresholds(), true);
        Subs.CVar(_cfg, FishCCVars.PgLoadCriticalThreshold, _ => RefreshLoadThresholds(), true);

        RefreshLoadThresholds();
        _analyzer.Configure(_cfg.GetCVar(FishCCVars.PgCpuBudgetMs), _cfg.GetCVar(FishCCVars.PgMaxAlerts));
        _reports.Configure(_cfg.GetCVar(FishCCVars.PgMaxReports));

        SubscribeNetworkEvent<PgSubscribeRequest>(OnSubscribe);
        SubscribeNetworkEvent<PgUnsubscribeRequest>(OnUnsubscribe);
        SubscribeNetworkEvent<PgSnapshotRequest>(OnSnapshotRequest);

        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        _subscribers.Clear();
    }

    private void RefreshLoadThresholds()
    {
        _load.Configure(
            _cfg.GetCVar(FishCCVars.PgLoadReducedThreshold),
            _cfg.GetCVar(FishCCVars.PgLoadDegradedThreshold),
            _cfg.GetCVar(FishCCVars.PgLoadCriticalThreshold));
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            _subscribers.Remove(e.Session);
            Profiles.Remove(e.Session.UserId);
        }
    }

    private bool IsDebugAdmin(ICommonSession session)
    {
        return _admins.HasAdminFlag(session, AdminFlags.Debug);
    }

    private void OnSubscribe(PgSubscribeRequest msg, EntitySessionEventArgs args)
    {
        if (!IsDebugAdmin(args.SenderSession))
            return;

        _subscribers.Add(args.SenderSession);
        RaiseNetworkEvent(new PgSnapshotResponse(PgSnapshotSection.All, BuildSnapshot(PgSnapshotSection.All)), args.SenderSession);
    }

    private void OnUnsubscribe(PgUnsubscribeRequest msg, EntitySessionEventArgs args)
    {
        _subscribers.Remove(args.SenderSession);
    }

    private void OnSnapshotRequest(PgSnapshotRequest msg, EntitySessionEventArgs args)
    {
        if (!IsDebugAdmin(args.SenderSession))
            return;

        if (!_subscribers.Contains(args.SenderSession))
            _subscribers.Add(args.SenderSession);

        RaiseNetworkEvent(new PgSnapshotResponse(msg.Section, BuildSnapshot(msg.Section)), args.SenderSession);
    }

    public override void Update(float frameTime)
    {
        if (!_enabled)
            return;

        var now = _timing.CurTime;
        if (now >= _nextSample)
        {
            RunSample(now);
            _nextSample = now + TimeSpan.FromSeconds(_sampleInterval);
        }

        if (_load.AllowAnalyzer && now >= _nextAnalyze)
        {
            _analyzer.Tick(_aggregator, _reports, _load.Level, _latest);
            MaybePushAlert();
            _nextAnalyze = now + TimeSpan.FromSeconds(_analyzeInterval * _load.AnalyzerIntervalMultiplier);
        }
    }

    private void RunSample(TimeSpan now)
    {
        var sample = _sampler.Sample(_load.Level, _analyzer.RiskScore);
        _load.Update(sample.TickMs, sample.TickBudgetMs);
        sample.LoadLevel = _load.Level;
        sample.RiskScore = _analyzer.RiskScore;

        Counters.TakeRates(_rateScratch);
        var dt = _lastSampleAt == TimeSpan.Zero ? _sampleInterval : (float)(now - _lastSampleAt).TotalSeconds;
        if (dt < 0.001f)
            dt = _sampleInterval;

        var total = 0;
        for (var i = 0; i < _rateScratch.Length; i++)
            total += _rateScratch[i];
        sample.EventRatePerSec = total / dt;

        _lastSampleAt = now;
        _latest = sample;

        if (!_load.EssentialOnly || _load.Level == PgLoadLevel.Critical)
            _blackBox.Append(sample);

        if (_load.FreezeBlackBox)
            _blackBox.Freeze();

        if (!_load.EssentialOnly)
            _aggregator.PushSample(sample, _rateScratch);
        else
        {
            // Still keep essential gauges in the short window for UI.
            _aggregator.Window10s.Push(sample);
        }
    }

    private void MaybePushAlert()
    {
        var alert = _analyzer.LatestAlertOrNull();
        if (alert == null || ReferenceEquals(alert, _lastPushedAlert) || alert == _lastPushedAlert)
            return;

        if (_lastPushedAlert != null && _lastPushedAlert.Id == alert.Id)
            return;

        _lastPushedAlert = alert;

        foreach (var admin in _admins.ActiveAdmins)
        {
            if (!_admins.HasAdminFlag(admin, AdminFlags.Admin) &&
                !_admins.HasAdminFlag(admin, AdminFlags.Debug))
                continue;

            RaiseNetworkEvent(new PgAlertPush(alert), admin);
        }
    }

    public PgServerSnapshot BuildSnapshot(PgSnapshotSection section)
    {
        var snap = new PgServerSnapshot
        {
            ServerTime = _timing.CurTime,
            LoadLevel = _load.Level,
            RiskScore = _analyzer.RiskScore,
            TickMs = _latest.TickMs,
            TickBudgetMs = _latest.TickBudgetMs,
            Tps = _latest.Tps,
            EntityCount = _latest.EntityCount,
            GridCount = _latest.GridCount,
            AwakeBodies = _latest.AwakeBodies,
            AtmosActiveTiles = _latest.AtmosActiveTiles,
            AtmosHotspots = _latest.AtmosHotspots,
            AtmosExcitedGroups = _latest.AtmosExcitedGroups,
            GcMemoryBytes = _latest.GcMemoryBytes,
            PlayerCount = _latest.PlayerCount,
            AnalyzerBudgetUsedMs = _analyzer.LastBudgetUsedMs,
            BlackBoxFrozen = _blackBox.IsFrozen,
            CategoryRates = CopyRates(),
            CorrTickVsAtmos = _analyzer.CorrTickVsAtmos,
            CorrTickVsAwake = _analyzer.CorrTickVsAwake,
            CorrTickVsEvents = _analyzer.CorrTickVsEvents,
            ProfilerNote = _analyzer.ProfilerNote,
        };

        var needPlayers = section is PgSnapshotSection.All or PgSnapshotSection.Players or PgSnapshotSection.Dashboard or PgSnapshotSection.Risk;
        var needAlerts = section is PgSnapshotSection.All or PgSnapshotSection.Alerts or PgSnapshotSection.Dashboard;
        var needReports = section is PgSnapshotSection.All or PgSnapshotSection.Reports;
        var needTimeline = section is PgSnapshotSection.All or PgSnapshotSection.Timeline;
        var needHeat = section is PgSnapshotSection.All or PgSnapshotSection.HeatMap or PgSnapshotSection.TopSystems;
        var needTops = section is PgSnapshotSection.All or PgSnapshotSection.TopEntities or PgSnapshotSection.TopSystems;
        var needHistory = section is PgSnapshotSection.All or PgSnapshotSection.History or PgSnapshotSection.Performance or PgSnapshotSection.Profiler;

        if (needPlayers)
            Profiles.CopyRows(snap.Players, 48);
        if (needAlerts)
            _analyzer.CopyAlerts(snap.Alerts);
        if (needReports)
            _reports.CopyTo(snap.Reports);
        if (needTimeline)
            _analyzer.CopyTimeline(snap.Timeline);
        if (needHeat)
            _analyzer.CopyHeat(snap.HeatMap);
        if (needTops)
        {
            _analyzer.CopyTopEntities(snap.TopEntities);
            _analyzer.CopyTopSystems(snap.TopSystems);
        }

        if (needHistory)
        {
            _blackBox.CopyHistory(_historyScratch, 60);
            snap.History.AddRange(_historyScratch);
        }

        return snap;
    }

    private int[] CopyRates()
    {
        var copy = new int[_aggregator.LastRates.Length];
        Array.Copy(_aggregator.LastRates, copy, copy.Length);
        return copy;
    }

    /// <summary>
    /// Used by admin command to hint the client to open the window.
    /// </summary>
    public void HintOpenWindow(ICommonSession session)
    {
        if (!IsDebugAdmin(session))
            return;

        RaiseNetworkEvent(new PgOpenWindowHint(), session);
    }
}
