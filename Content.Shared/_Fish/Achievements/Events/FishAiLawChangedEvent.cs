namespace Content.Shared._Fish.Achievements.Events;

/// <summary>
/// Broadcast при смене законов у silicon — без duplicate subscription на law systems.
/// </summary>
[ByRefEvent]
public record struct FishAiLawChangedEvent(EntityUid Silicon, string Source);
