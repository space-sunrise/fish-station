namespace Content.Shared._Fish.Achievements.Events;

/// <summary>
/// Broadcast при crit succumb — для achievements без duplicate subscription на MobStateActionsComponent.
/// </summary>
[ByRefEvent]
public record struct FishCritSuccumbEvent(EntityUid Mob);
