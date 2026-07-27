using Content.Shared.Chemistry.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Disease;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SickComponent : Component
{
    [DataField("owner")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public EntityUid owner;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("icon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string Icon = "SmartDiseaseIcon";

    [DataField("inited")]
    [AutoNetworkedField]
    public bool Inited = false;

    [DataField] public int Stady = 0;

    [DataField] public List<string> Symptoms = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextStadyAt = TimeSpan.Zero;

    [DataField("stadyDelay")]
    public TimeSpan StadyDelay = TimeSpan.FromMinutes(5);

    [DataField("beforeInfectedBloodReagents")]
    public Solution BeforeInfectedBloodReagents = new();
}
