using Content.Server._Fish.Objectives.Components;
using Content.Shared._Fish.Objectives.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Maps;
using Content.Shared.Nutrition;
using Content.Shared.Paper;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Objectives.Systems;

/// <summary>
/// Отслеживает прогресс целей животных: еда, питьё, бумага, перемещение.
/// </summary>
public sealed class AnimalObjectiveTrackerSystem : EntitySystem
{
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaDataComponent, IngestedEvent>(OnIngested);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnimalObjectiveTrackerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tracker, out var xform))
        {
            if (xform.GridUid is not { } grid)
                continue;

            if (_turf.GetTileRef(xform.Coordinates) is not { } tileRef)
                continue;

            var tile = tileRef.GridIndices;

            if (tracker.LastGrid == grid && tracker.LastTile == tile)
                continue;

            if (tracker.LastTile.HasValue)
                tracker.TilesMoved++;

            tracker.LastGrid = grid;
            tracker.LastTile = tile;
            tracker.VisitedGrids.Add(grid);
        }
    }

    private void OnIngested(EntityUid food, MetaDataComponent comp, IngestedEvent args)
    {
        if (!TryComp<AnimalObjectiveTrackerComponent>(args.Target, out var tracker))
            return;

        tracker.EatCount++;
        tracker.DrinkVolume += args.Split.Volume;

        foreach (var (reagent, quantity) in args.Split.Contents)
        {
            var id = reagent.Prototype;
            tracker.DrunkReagents.TryGetValue(id, out var existing);
            tracker.DrunkReagents[id] = existing + quantity;
        }

        if (MetaData(food).EntityPrototype is { } foodProto)
            tracker.EatenFoodProtos.Add(foodProto.ID);

        if (TryComp<TagComponent>(food, out var tagComp))
        {
            foreach (var tag in tagComp.Tags)
            {
                tracker.EatenTagCounts.TryGetValue(tag, out var count);
                tracker.EatenTagCounts[tag] = count + 1;
            }
        }

        if (_tag.HasTag(food, "Paper"))
        {
            tracker.PaperEaten++;

            if (IsBlankPaper(food))
                tracker.BlankPaperEaten++;
        }
    }

    private bool IsBlankPaper(EntityUid paper)
    {
        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return _tag.HasTag(paper, "Paper");

        return paperComp.StampedBy.Count == 0 && string.IsNullOrWhiteSpace(paperComp.Content);
    }

    public int GetEatCount(EntityUid uid, AnimalObjectiveTrackerComponent? tracker = null)
    {
        return Resolve(uid, ref tracker) ? tracker.EatCount : 0;
    }

    public float GetDrinkVolume(EntityUid uid, AnimalObjectiveTrackerComponent? tracker = null)
    {
        return Resolve(uid, ref tracker) ? (float) tracker.DrinkVolume : 0f;
    }

    public float GetReagentVolume(EntityUid uid, ProtoId<ReagentPrototype> reagent, AnimalObjectiveTrackerComponent? tracker = null)
    {
        if (!Resolve(uid, ref tracker))
            return 0f;

        return tracker.DrunkReagents.TryGetValue(reagent, out var volume) ? (float) volume : 0f;
    }

    public int GetTagEatCount(EntityUid uid, ProtoId<TagPrototype> tag, AnimalObjectiveTrackerComponent? tracker = null)
    {
        if (!Resolve(uid, ref tracker))
            return 0;

        return tracker.EatenTagCounts.GetValueOrDefault(tag);
    }

    public int GetPaperEaten(EntityUid uid, AnimalObjectiveTrackerComponent? tracker = null)
    {
        return Resolve(uid, ref tracker) ? tracker.PaperEaten : 0;
    }

    public int GetBlankPaperEaten(EntityUid uid, AnimalObjectiveTrackerComponent? tracker = null)
    {
        return Resolve(uid, ref tracker) ? tracker.BlankPaperEaten : 0;
    }

    public int GetTilesMoved(EntityUid uid, AnimalObjectiveTrackerComponent? tracker = null)
    {
        return Resolve(uid, ref tracker) ? tracker.TilesMoved : 0;
    }

    public int GetVisitedGridCount(EntityUid uid, AnimalObjectiveTrackerComponent? tracker = null)
    {
        return Resolve(uid, ref tracker) ? tracker.VisitedGrids.Count : 0;
    }

    public int GetUniqueFoodCount(EntityUid uid, AnimalObjectiveTrackerComponent? tracker = null)
    {
        return Resolve(uid, ref tracker) ? tracker.EatenFoodProtos.Count : 0;
    }
}
