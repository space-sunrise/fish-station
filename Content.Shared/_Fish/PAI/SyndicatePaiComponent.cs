using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Fish.PAI;

/// <summary>
/// Syndicate pAI: master binding, purchasable modules, medical suite.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedSyndicatePaiSystem))]
public sealed partial class SyndicatePaiComponent : Component
{
    /// <summary>
    /// Prototype of the regenerating hypo granted with the medical module.
    /// </summary>
    [DataField]
    public EntProtoId HypoPrototype = "HypoPaiSyndicateMedical";

    /// <summary>
    /// Health analyzer granted with the medical module.
    /// </summary>
    [DataField]
    public EntProtoId AnalyzerPrototype = "HandheldHealthAnalyzer";

    [DataField]
    public EntProtoId OpenMedicalAction = "ActionSyndicatePaiOpenMedical";

    [DataField]
    public EntProtoId ScanOwnerAction = "ActionSyndicatePaiScanOwner";

    [DataField]
    public EntProtoId DoorHackAction = "ActionSyndicatePaiDoorHack";

    [DataField]
    public EntProtoId SecRecordsAction = "ActionSyndicatePaiSecRecords";

    [DataField, AutoNetworkedField]
    public EntityUid? OpenMedicalActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? ScanOwnerActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? DoorHackActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? SecRecordsActionEntity;

    /// <summary>
    /// Bound master (DNA imprint). Required for medical inject/scan.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Master;

    /// <summary>
    /// Supplemental directive text set by the master.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? SupplementalDirective;

    [DataField, AutoNetworkedField]
    public bool MedicalUnlocked;

    [DataField, AutoNetworkedField]
    public bool DoorHackUnlocked;

    [DataField, AutoNetworkedField]
    public bool SecRecordsUnlocked;

    /// <summary>
    /// Visually masquerade as a normal personal AI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Disguised;

    /// <summary>
    /// Next time door-hack may be used (server timing).
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDoorHackTime;

    /// <summary>
    /// Door-hack cooldown between uses.
    /// </summary>
    [DataField]
    public TimeSpan DoorHackCooldown = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Radius (tiles) around the master for door-hack.
    /// </summary>
    [DataField]
    public float DoorHackRadius = 3f;

    public const string InnateItemContainerId = "innate_items";
}
