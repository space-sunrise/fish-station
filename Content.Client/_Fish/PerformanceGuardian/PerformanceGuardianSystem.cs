using Content.Shared._Fish.PerformanceGuardian;
using Robust.Shared.GameObjects;

namespace Content.Client._Fish.PerformanceGuardian;

/// <summary>
/// Клиентский приём отчётов Performance Guardian.
/// </summary>
public sealed class PerformanceGuardianSystem : EntitySystem
{
    public event Action<PgReport>? ReportReceived;
    public event Action? OpenWindowRequested;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PgReportResponse>(OnReport);
        SubscribeNetworkEvent<PgOpenWindowHint>(_ => OpenWindowRequested?.Invoke());
    }

    private void OnReport(PgReportResponse msg)
    {
        ReportReceived?.Invoke(msg.Report);
    }

    public void Subscribe()
    {
        RaiseNetworkEvent(new PgSubscribeRequest());
    }

    public void Unsubscribe()
    {
        RaiseNetworkEvent(new PgUnsubscribeRequest());
    }

    public void RequestReport()
    {
        RaiseNetworkEvent(new PgReportRequest());
    }

    public void RequestDiagnose()
    {
        RaiseNetworkEvent(new PgDiagnoseRequest());
    }
}
