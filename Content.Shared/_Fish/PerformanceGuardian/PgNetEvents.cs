using Robust.Shared.Serialization;

namespace Content.Shared._Fish.PerformanceGuardian;

[Serializable, NetSerializable]
public sealed class PgSubscribeRequest : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class PgUnsubscribeRequest : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class PgSnapshotRequest : EntityEventArgs
{
    public PgSnapshotSection Section;

    public PgSnapshotRequest(PgSnapshotSection section)
    {
        Section = section;
    }
}

[Serializable, NetSerializable]
public sealed class PgSnapshotResponse : EntityEventArgs
{
    public PgSnapshotSection Section;
    public PgServerSnapshot Snapshot;

    public PgSnapshotResponse(PgSnapshotSection section, PgServerSnapshot snapshot)
    {
        Section = section;
        Snapshot = snapshot;
    }
}

[Serializable, NetSerializable]
public sealed class PgAlertPush : EntityEventArgs
{
    public PgAlert Alert;

    public PgAlertPush(PgAlert alert)
    {
        Alert = alert;
    }
}

[Serializable, NetSerializable]
public sealed class PgOpenWindowHint : EntityEventArgs
{
}
