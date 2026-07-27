using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.PAI;

/// <summary>
/// Syndicate pAI medical suite: carrier injection, reagent cycling, master binding.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSyndicatePaiSystem))]
public sealed partial class SyndicatePaiComponent : Component
{
    /// <summary>
    /// Prototype of the regenerating hypo spawned into innate_items.
    /// </summary>
    [DataField]
    public EntProtoId HypoPrototype = "HypoPaiSyndicateMedical";

    /// <summary>
    /// Action that opens the medical suite BUI.
    /// </summary>
    [DataField]
    public EntProtoId OpenMedicalAction = "ActionSyndicatePaiOpenMedical";

    /// <summary>
    /// Instant action: inject the current carrier/master.
    /// </summary>
    [DataField]
    public EntProtoId InjectCarrierAction = "ActionSyndicatePaiInjectCarrier";

    /// <summary>
    /// Instant action: cycle hypo reagent.
    /// </summary>
    [DataField]
    public EntProtoId CycleReagentAction = "ActionSyndicatePaiCycleReagent";

    [DataField, AutoNetworkedField]
    public EntityUid? OpenMedicalActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? InjectCarrierActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? CycleReagentActionEntity;

    /// <summary>
    /// Bound master (DNA imprint analog). Set when a player activates the empty device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Master;

    /// <summary>
    /// Supplemental directive text set by the master (SS13 secondary laws analog).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? SupplementalDirective;

    public const string InnateItemContainerId = "innate_items";
}
