using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Kitsune;

public sealed partial class KitsuneTransformActionEvent : InstantActionEvent
{
}

public sealed partial class KitsuneRevertActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class KitsuneTransformDoAfterEvent : SimpleDoAfterEvent
{
}
