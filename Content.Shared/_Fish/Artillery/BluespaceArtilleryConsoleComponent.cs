using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Fish.Artillery;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBluespaceArtillerySystem))]
public sealed partial class BluespaceArtilleryConsoleComponent : Component
{
	[DataField("linkingPort")]
	public string LinkingPort = "Artillery";
	
    [ViewVariables]
    public EntityUid? LinkedArtillery;

    [ViewVariables] public ArtilleryVector2 TargetCoordinates;
    [ViewVariables] public string ExplosionType = "Default";
    [ViewVariables] public float TotalIntensity = 100f;
    [ViewVariables] public float Slope = 5f;
    [ViewVariables] public float MaxIntensity = 50f;
    [ViewVariables] public bool PreviewEnabled = false;
}