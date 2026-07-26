using Content.Shared._Fish.PerformanceGuardian;
using Robust.Shared.Timing;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Anomaly, risk, correlation, timeline, auto-reports — hard CPU budget with resume cursor.
/// </summary>
public sealed class PgAnalyzer
{
    private readonly IGameTiming _timing;
    private readonly List<PgAlert> _alerts = new();
    private readonly List<PgTimelineEvent> _timeline = new();
    private readonly List<PgHeatRow> _heat = new();
    private readonly List<PgTopRow> _topEntities = new();
    private readonly List<PgTopRow> _topSystems = new();
    private readonly Stopwatch _sw = new();

    private int _alertId = 1;
    private int _phase;
    private int _maxAlerts = 64;
    private float _budgetMs = 2f;
    private float _lastBudgetUsed;
    private float _risk;
    private float _corrAtmos;
    private float _corrAwake;
    private float _corrEvents;
    private string _profilerNote = "Event-rate heat + rolling z-scores (no EntitySystemManager patch).";

    public float RiskScore => _risk;
    public float LastBudgetUsedMs => _lastBudgetUsed;
    public float CorrTickVsAtmos => _corrAtmos;
    public float CorrTickVsAwake => _corrAwake;
    public float CorrTickVsEvents => _corrEvents;
    public string ProfilerNote => _profilerNote;

    public PgAnalyzer(IGameTiming timing)
    {
        _timing = timing;
    }

    public void Configure(float budgetMs, int maxAlerts)
    {
        _budgetMs = Math.Max(0.25f, budgetMs);
        _maxAlerts = Math.Max(8, maxAlerts);
    }

    public bool Tick(
        PgAggregator aggregator,
        PgReportStore reports,
        PgLoadLevel load,
        in PgSamplePoint sample)
    {
        _sw.Restart();
        var done = false;

        while (_sw.Elapsed.TotalMilliseconds < _budgetMs)
        {
            switch (_phase)
            {
                case 0:
                    UpdateRisk(aggregator, sample);
                    _phase = 1;
                    break;
                case 1:
                    DetectAnomalies(aggregator, sample, load, reports);
                    _phase = 2;
                    break;
                case 2:
                    _corrAtmos = aggregator.CorrelateTickVs(static p => p.AtmosActiveTiles);
                    if (_sw.Elapsed.TotalMilliseconds >= _budgetMs)
                        goto finish;
                    _corrAwake = aggregator.CorrelateTickVs(static p => p.AwakeBodies);
                    if (_sw.Elapsed.TotalMilliseconds >= _budgetMs)
                        goto finish;
                    _corrEvents = aggregator.CorrelateTickVs(static p => p.EventRatePerSec);
                    _phase = 3;
                    break;
                case 3:
                    RebuildHeat(aggregator);
                    _phase = 4;
                    break;
                case 4:
                    RebuildTops(aggregator, sample);
                    _phase = 0;
                    done = true;
                    goto finish;
                default:
                    _phase = 0;
                    done = true;
                    goto finish;
            }
        }

        finish:
        _sw.Stop();
        _lastBudgetUsed = (float)_sw.Elapsed.TotalMilliseconds;
        return done;
    }

    private void UpdateRisk(PgAggregator aggregator, in PgSamplePoint sample)
    {
        var zTick = Math.Abs(aggregator.TickMs.ZScore(sample.TickMs));
        var zAtmos = Math.Abs(aggregator.AtmosActive.ZScore(sample.AtmosActiveTiles));
        var zAwake = Math.Abs(aggregator.AwakeBodies.ZScore(sample.AwakeBodies));
        var zEvents = Math.Abs(aggregator.EventRate.ZScore(aggregator.LastEventRatePerSec));
        var overrun = sample.TickBudgetMs > 0.001f ? sample.TickMs / sample.TickBudgetMs : 1f;

        _risk = Math.Clamp(
            zTick * 12f + zAtmos * 8f + zAwake * 8f + zEvents * 10f + Math.Max(0f, overrun - 1f) * 40f,
            0f,
            100f);
    }

    private void DetectAnomalies(
        PgAggregator aggregator,
        in PgSamplePoint sample,
        PgLoadLevel load,
        PgReportStore reports)
    {
        TryAlert(aggregator.TickMs, sample.TickMs, "tick_ms", "Tick duration anomaly", load, reports);
        TryAlert(aggregator.AtmosActive, sample.AtmosActiveTiles, "atmos_active", "Atmos active tiles anomaly", load, reports);
        TryAlert(aggregator.AwakeBodies, sample.AwakeBodies, "awake_bodies", "Awake physics bodies anomaly", load, reports);
        TryAlert(aggregator.EventRate, aggregator.LastEventRatePerSec, "event_rate", "Gameplay event-rate anomaly", load, reports);

        if (load == PgLoadLevel.Critical)
        {
            PushTimeline("load", "Entered Critical adaptive load", PgAlertSeverity.Critical);
        }
    }

    private void TryAlert(
        PgWelfordAccumulator acc,
        double value,
        string metric,
        string title,
        PgLoadLevel load,
        PgReportStore reports)
    {
        var z = Math.Abs(acc.ZScore(value));
        if (z < 2.5f || acc.Count < 12)
            return;

        var severity = z >= 4f ? PgAlertSeverity.Critical
            : z >= 3.2f ? PgAlertSeverity.Severe
            : PgAlertSeverity.Warning;

        var alert = new PgAlert
        {
            Id = _alertId++,
            At = _timing.CurTime,
            Severity = severity,
            Title = title,
            Detail = $"z={z:0.00}, value={value:0.##}, mean={acc.Mean:0.##}",
            Metric = metric,
            Value = (float)value,
            Baseline = (float)acc.Mean,
        };

        _alerts.Add(alert);
        while (_alerts.Count > _maxAlerts)
            _alerts.RemoveAt(0);

        PushTimeline(metric, alert.Detail, severity);

        if (severity >= PgAlertSeverity.Severe)
        {
            reports.Add(new PgReportSummary
            {
                At = _timing.CurTime,
                Severity = severity,
                Title = title,
                Summary = alert.Detail,
                RiskScore = _risk,
                LoadAtIncident = load,
            });
        }
    }

    private void PushTimeline(string category, string message, PgAlertSeverity severity)
    {
        _timeline.Add(new PgTimelineEvent
        {
            At = _timing.CurTime,
            Category = category,
            Message = message,
            Severity = severity,
        });

        while (_timeline.Count > 128)
            _timeline.RemoveAt(0);
    }

    private void RebuildHeat(PgAggregator aggregator)
    {
        _heat.Clear();
        var total = 0;
        for (var i = 0; i < aggregator.LastRates.Length; i++)
            total += aggregator.LastRates[i];

        for (var i = 0; i < aggregator.LastRates.Length; i++)
        {
            var rate = aggregator.LastRates[i];
            _heat.Add(new PgHeatRow
            {
                Name = ((PgMetricCategory)i).ToString(),
                RatePerSec = rate,
                Share = total > 0 ? rate / (float)total : 0f,
            });
        }

        _heat.Sort(static (a, b) => b.RatePerSec.CompareTo(a.RatePerSec));
    }

    private void RebuildTops(PgAggregator aggregator, in PgSamplePoint sample)
    {
        _topSystems.Clear();
        for (var i = 0; i < aggregator.LastRates.Length; i++)
        {
            _topSystems.Add(new PgTopRow
            {
                Name = ((PgMetricCategory)i).ToString(),
                Score = aggregator.LastRates[i],
                Detail = "event-rate heat",
            });
        }

        _topSystems.Sort(static (a, b) => b.Score.CompareTo(a.Score));

        _topEntities.Clear();
        _topEntities.Add(new PgTopRow
        {
            Name = "AwakeBodies",
            Score = sample.AwakeBodies,
            Detail = "physics proxy",
        });
        _topEntities.Add(new PgTopRow
        {
            Name = "AtmosActiveTiles",
            Score = sample.AtmosActiveTiles,
            Detail = "atmos proxy",
        });
        _topEntities.Add(new PgTopRow
        {
            Name = "Entities",
            Score = sample.EntityCount,
            Detail = "entity count",
        });
        _topEntities.Add(new PgTopRow
        {
            Name = "Hotspots",
            Score = sample.AtmosHotspots,
            Detail = "atmos hotspot tiles",
        });
        _topEntities.Sort(static (a, b) => b.Score.CompareTo(a.Score));
    }

    public void CopyAlerts(List<PgAlert> destination)
    {
        destination.Clear();
        destination.AddRange(_alerts);
    }

    public void CopyTimeline(List<PgTimelineEvent> destination)
    {
        destination.Clear();
        destination.AddRange(_timeline);
    }

    public void CopyHeat(List<PgHeatRow> destination)
    {
        destination.Clear();
        destination.AddRange(_heat);
    }

    public void CopyTopEntities(List<PgTopRow> destination)
    {
        destination.Clear();
        destination.AddRange(_topEntities);
    }

    public void CopyTopSystems(List<PgTopRow> destination)
    {
        destination.Clear();
        destination.AddRange(_topSystems);
    }

    public PgAlert? LatestAlertOrNull()
    {
        return _alerts.Count == 0 ? null : _alerts[^1];
    }
}
