using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared._Fish.Achievements.Events;

/// <summary>
/// Broadcast после успешного pry/deconstruct floor tile инструментом.
/// </summary>
[ByRefEvent]
public record struct FishTilePriedEvent(
    EntityUid User,
    EntityUid Grid,
    Vector2i Tile,
    EntityUid Tool,
    string RemovedTileId);
