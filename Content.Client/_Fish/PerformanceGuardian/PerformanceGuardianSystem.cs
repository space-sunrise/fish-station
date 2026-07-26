using Content.Shared._Fish.PerformanceGuardian;
using Robust.Shared.Timing;

namespace Content.Client._Fish.PerformanceGuardian;

/// <summary>
/// Client net bridge for Performance Guardian snapshots and alerts.
/// </summary>
public sealed class PerformanceGuardianSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public event Action<PgSnapshotSection, PgServerSnapshot>? SnapshotReceived;
    public event Action<PgAlert>? AlertReceived;
    public event Action? OpenWindowRequested;

    public PgServerSnapshot? LastSnapshot { get; private set; }
    public TimeSpan LastSnapshotAt { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PgSnapshotResponse>(OnSnapshot);
        SubscribeNetworkEvent<PgAlertPush>(OnAlert);
        SubscribeNetworkEvent<PgOpenWindowHint>(OnOpenHint);
    }

    private void OnSnapshot(PgSnapshotResponse msg, EntitySessionEventArgs args)
    {
        LastSnapshot = msg.Snapshot;
        LastSnapshotAt = _timing.RealTime;
        SnapshotReceived?.Invoke(msg.Section, msg.Snapshot);
    }

    private void OnAlert(PgAlertPush msg, EntitySessionEventArgs args)
    {
        AlertReceived?.Invoke(msg.Alert);
    }

    private void OnOpenHint(PgOpenWindowHint msg, EntitySessionEventArgs args)
    {
        OpenWindowRequested?.Invoke();
    }

    public void Subscribe()
    {
        RaiseNetworkEvent(new PgSubscribeRequest());
    }

    public void Unsubscribe()
    {
        RaiseNetworkEvent(new PgUnsubscribeRequest());
    }

    public void RequestSnapshot(PgSnapshotSection section)
    {
        RaiseNetworkEvent(new PgSnapshotRequest(section));
    }
}
