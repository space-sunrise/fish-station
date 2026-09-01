using Content.Shared.Sound;
using Content.Shared.Maps;
using Content.Shared.GameTicking;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;

namespace Content.Shared._Fish.Artillery;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedBluespaceArtillerySystem))]
public sealed partial class BluespaceArtilleryComponent : Component
{
    [DataField("chargeDuration")]
    public float ChargeDuration = 8.5f;
	
	[DataField("flightDuration")]
	public float FlightDuration = 12.0f;
	
	[DataField("cooldownDuration")]
	public float CooldownDuration = 60.0f;

    [DataField("sectorChargeSound")]
    public SoundSpecifier SectorChargeSound = default!;

    [DataField("chargeSound")]
    public SoundSpecifier ChargeSound = default!;

    [DataField("fireSound")]
    public SoundSpecifier FireSound = default!;

    [DataField("impactSound")]
    public SoundSpecifier ImpactSound = default!;

    [DataField("chargingState")]
    public string ChargingState = "charging";

    [DataField("fireState")]
    public string FireState = "fire";
	
	[DataField("linkingPort")]
	public string LinkingPort = "Artillery";
	
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Console;

    [ViewVariables]
    public EntityUid? LinkedConsole;

    [ViewVariables]
    public bool IsCharging;
	
	[ViewVariables]
	public TimeSpan NextFireTime;
	
	[ViewVariables]
	public MapId? TargetMapId = null;
}