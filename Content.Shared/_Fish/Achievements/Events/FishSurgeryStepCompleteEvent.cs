using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Achievements.Events;

/// <summary>
/// Broadcast после успешного шага операции — для achievements без duplicate subscription на SurgeryStepComponent.
/// </summary>
[ByRefEvent]
public record struct FishSurgeryStepCompleteEvent(
    EntityUid User,
    EntityUid Body,
    EntityUid Part)
{
    public required EntProtoId StepProto { get; init; }
    public required EntProtoId SurgeryProto { get; init; }
    public required bool IsFinal { get; init; }
}
