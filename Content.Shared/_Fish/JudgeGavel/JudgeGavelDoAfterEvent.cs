using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Fish.JudgeGavel;

[Serializable, NetSerializable]
public sealed partial class JudgeGavelDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
