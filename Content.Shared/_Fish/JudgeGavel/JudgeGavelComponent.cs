using System.Numerics;
using Content.Shared.Pinpointer;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Fish.JudgeGavel;

/// <summary>
///     Component for the Admin Judge Gavel.
///     When activated, starts a DoAfter that teleports sentient creatures in a radius to the Centcomm courtroom.
///     FIsh edit
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class JudgeGavelComponent : Component
{
    [DataField]
    public float Range = 10f;

    [DataField]
    public float Duration = 900f; // Seconds of pacifism

    [DataField]
    public string CourtroomBeaconId = "station-beacon-courtroom";

    [DataField]
    public float GodmodeDuration = 2f;

    [DataField]
    public LocId Chant = "judge-gavel-chant";

    [DataField]
    public float DoAfterTime = 3f;
}
