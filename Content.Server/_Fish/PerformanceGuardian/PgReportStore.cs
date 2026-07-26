using Content.Shared._Fish.PerformanceGuardian;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Fixed-cap auto-report store.
/// </summary>
public sealed class PgReportStore
{
    private readonly List<PgReportSummary> _reports = new();
    private int _nextId = 1;
    private int _max = 32;

    public void Configure(int max)
    {
        _max = Math.Max(4, max);
        Trim();
    }

    public void Add(PgReportSummary report)
    {
        report.Id = _nextId++;
        _reports.Add(report);
        Trim();
    }

    private void Trim()
    {
        while (_reports.Count > _max)
            _reports.RemoveAt(0);
    }

    public void CopyTo(List<PgReportSummary> destination)
    {
        destination.Clear();
        for (var i = 0; i < _reports.Count; i++)
            destination.Add(_reports[i]);
    }
}
