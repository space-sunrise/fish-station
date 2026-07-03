using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Objectives.Components;

/// <summary>
/// Счётчики прогресса целей животного на сущности-игроке.
/// </summary>
[RegisterComponent]
public sealed partial class AnimalObjectiveTrackerComponent : Component
{
    [DataField]
    public int EatCount;

    [DataField]
    public FixedPoint2 DrinkVolume;

    [DataField]
    public int PaperEaten;

    [DataField]
    public int BlankPaperEaten;

    [DataField]
    public int TilesMoved;

    [DataField]
    public HashSet<EntityUid> VisitedGrids = new();

    [DataField]
    public HashSet<ProtoId<EntityPrototype>> EatenFoodProtos = new();

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> DrunkReagents = new();

    [DataField]
    public Dictionary<ProtoId<TagPrototype>, int> EatenTagCounts = new();

    [ViewVariables]
    public EntityUid? LastGrid;

    [ViewVariables]
    public Vector2i? LastTile;
}
