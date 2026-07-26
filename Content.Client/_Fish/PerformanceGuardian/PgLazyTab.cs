using Content.Shared._Fish.PerformanceGuardian;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Fish.PerformanceGuardian;

/// <summary>
/// Lazy tab: only formats text when selected and a snapshot arrives.
/// </summary>
public sealed class PgLazyTab : Control
{
    private readonly RichTextLabel _label;
    private readonly Func<PgServerSnapshot, string> _formatter;
    public PgSnapshotSection Section { get; }

    public PgLazyTab(PgSnapshotSection section, Func<PgServerSnapshot, string> formatter)
    {
        Section = section;
        _formatter = formatter;
        HorizontalExpand = true;
        VerticalExpand = true;

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _label = new RichTextLabel
        {
            HorizontalExpand = true,
        };

        scroll.AddChild(_label);
        AddChild(scroll);
    }

    public void Apply(PgServerSnapshot snapshot)
    {
        _label.Text = _formatter(snapshot);
    }

    public void SetPlaceholder(string text)
    {
        _label.Text = text;
    }
}

public static class PgSnapshotFormat
{
    public static string Dashboard(PgServerSnapshot s)
    {
        return $"[bold]Performance Guardian[/bold]\n" +
               $"Load: {s.LoadLevel} | Risk: {s.RiskScore:0.0}\n" +
               $"TPS: {s.Tps:0.0} | Tick: {s.TickMs:0.00}ms / {s.TickBudgetMs:0.00}ms\n" +
               $"Entities: {s.EntityCount} | Grids: {s.GridCount} | Players: {s.PlayerCount}\n" +
               $"Awake bodies: {s.AwakeBodies} | Atmos active: {s.AtmosActiveTiles}\n" +
               $"Hotspots: {s.AtmosHotspots} | Excited: {s.AtmosExcitedGroups}\n" +
               $"GC: {s.GcMemoryBytes / (1024f * 1024f):0.0} MiB | Analyzer budget used: {s.AnalyzerBudgetUsedMs:0.00}ms\n" +
               $"Black box frozen: {s.BlackBoxFrozen}\n" +
               $"Alerts: {s.Alerts.Count} | Reports: {s.Reports.Count}";
    }

    public static string Performance(PgServerSnapshot s)
    {
        var hist = new System.Text.StringBuilder();
        hist.AppendLine("[bold]Performance[/bold]");
        hist.AppendLine($"Tick {s.TickMs:0.00}/{s.TickBudgetMs:0.00} ms | TPS {s.Tps:0.0}");
        hist.AppendLine($"Corr tick↔atmos={s.CorrTickVsAtmos:0.00} awake={s.CorrTickVsAwake:0.00} events={s.CorrTickVsEvents:0.00}");
        hist.AppendLine("Recent samples (newest last):");
        var start = Math.Max(0, s.History.Count - 12);
        for (var i = start; i < s.History.Count; i++)
        {
            var p = s.History[i];
            hist.AppendLine($"  t={p.At.TotalSeconds:0} tick={p.TickMs:0.00} atmos={p.AtmosActiveTiles} awake={p.AwakeBodies} load={p.LoadLevel}");
        }

        return hist.ToString();
    }

    public static string Players(PgServerSnapshot s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[bold]Players (risk sorted)[/bold]");
        if (s.Players.Count == 0)
        {
            sb.AppendLine("No tracked player activity yet.");
            return sb.ToString();
        }

        foreach (var p in s.Players)
        {
            sb.AppendLine(
                $"{p.Name}: risk={p.RiskScore:0.0} 10s={p.EventsPerSec10s:0.00}/s 1m={p.EventsPerSec1m:0.00}/s atk={p.AttackCount} dmg={p.DamageCount} thr={p.ThrowCount} proj={p.ProjectileCount}");
        }

        return sb.ToString();
    }

    public static string Risk(PgServerSnapshot s)
    {
        return $"[bold]Risk[/bold]\nScore: {s.RiskScore:0.0}\nLoad: {s.LoadLevel}\n" +
               $"Tick overrun proxy: {(s.TickBudgetMs > 0 ? s.TickMs / s.TickBudgetMs : 0f):0.00}x\n" +
               $"Top player risk: {(s.Players.Count > 0 ? s.Players[0].RiskScore : 0f):0.0}";
    }

    public static string Timeline(PgServerSnapshot s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[bold]Timeline[/bold]");
        if (s.Timeline.Count == 0)
        {
            sb.AppendLine("No timeline events yet.");
            return sb.ToString();
        }

        for (var i = Math.Max(0, s.Timeline.Count - 40); i < s.Timeline.Count; i++)
        {
            var e = s.Timeline[i];
            sb.AppendLine($"[{e.Severity}] {e.At.TotalSeconds:0}s {e.Category}: {e.Message}");
        }

        return sb.ToString();
    }

    public static string HeatMap(PgServerSnapshot s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[bold]Event-rate heat[/bold]");
        foreach (var row in s.HeatMap)
            sb.AppendLine($"{row.Name}: {row.RatePerSec:0} ({row.Share * 100f:0.0}%)");
        if (s.HeatMap.Count == 0)
            sb.AppendLine("No rates yet.");
        return sb.ToString();
    }

    public static string TopEntities(PgServerSnapshot s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[bold]Top entity proxies[/bold]");
        foreach (var row in s.TopEntities)
            sb.AppendLine($"{row.Name}: {row.Score:0} — {row.Detail}");
        return sb.ToString();
    }

    public static string TopSystems(PgServerSnapshot s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[bold]Top systems (event-rate heat)[/bold]");
        foreach (var row in s.TopSystems)
            sb.AppendLine($"{row.Name}: {row.Score:0} — {row.Detail}");
        return sb.ToString();
    }

    public static string Alerts(PgServerSnapshot s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[bold]Alerts[/bold]");
        if (s.Alerts.Count == 0)
        {
            sb.AppendLine("No alerts.");
            return sb.ToString();
        }

        for (var i = Math.Max(0, s.Alerts.Count - 40); i < s.Alerts.Count; i++)
        {
            var a = s.Alerts[i];
            sb.AppendLine($"#{a.Id} [{a.Severity}] {a.Title}\n  {a.Detail}");
        }

        return sb.ToString();
    }

    public static string Reports(PgServerSnapshot s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[bold]Auto-reports[/bold]");
        if (s.Reports.Count == 0)
        {
            sb.AppendLine("No reports.");
            return sb.ToString();
        }

        foreach (var r in s.Reports)
            sb.AppendLine($"#{r.Id} [{r.Severity}] {r.Title} risk={r.RiskScore:0.0} load={r.LoadAtIncident}\n  {r.Summary}");
        return sb.ToString();
    }

    public static string Profiler(PgServerSnapshot s)
    {
        return $"[bold]Profiler[/bold]\n{s.ProfilerNote}\n" +
               $"Analyzer budget used: {s.AnalyzerBudgetUsedMs:0.00} ms\n" +
               $"History points: {s.History.Count}\n" +
               $"Category rates length: {s.CategoryRates.Length}";
    }

    public static string History(PgServerSnapshot s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[bold]Black-box / history[/bold]");
        sb.AppendLine($"Frozen: {s.BlackBoxFrozen}");
        foreach (var p in s.History)
        {
            sb.AppendLine(
                $"{p.At.TotalSeconds:0}s tick={p.TickMs:0.00} ent={p.EntityCount} atmos={p.AtmosActiveTiles} awake={p.AwakeBodies} risk={p.RiskScore:0.0} load={p.LoadLevel} ev/s={p.EventRatePerSec:0.0}");
        }

        if (s.History.Count == 0)
            sb.AppendLine("Empty.");
        return sb.ToString();
    }
}
