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
    /// Prototype of the emergency auto-dispenser hypo (separate reservoir).
    /// </summary>
    [DataField]
    public EntProtoId AutoHypoPrototype = "HypoPaiSyndicateAuto";

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

    [DataField, AutoNetworkedField]
    public bool MedicalUnlocked;

    [DataField, AutoNetworkedField]
    public bool AutoDispenserUnlocked;

    /// <summary>
    /// Автодозатор включён игроком.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoDispenserEnabled;

    /// <summary>
    /// Порог оставшегося здоровья владельца (%) для автоинъекции (0–100).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AutoHealthThreshold = 40f;

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
    /// Next time auto-dispenser may inject (server timing).
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAutoInjectTime;

    /// <summary>
    /// Cooldown after an automatic injection.
    /// </summary>
    [DataField]
    public TimeSpan AutoInjectCooldown = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Radius (tiles) around the master for door-hack.
    /// </summary>
    [DataField]
    public float DoorHackRadius = 3f;

    public const string InnateItemContainerId = "innate_items";
}
