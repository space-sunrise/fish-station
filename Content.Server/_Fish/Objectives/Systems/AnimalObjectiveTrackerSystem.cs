using Content.Shared._Fish.Objectives.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Maps;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Objectives.Systems;

public sealed class AnimalObjectiveTrackerSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> PaperTag = "Paper";

    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<EdibleComponent> _edibleQuery;

    public override void Initialize()
    {
        base.Initialize();

        _edibleQuery = GetEntityQuery<EdibleComponent>();

        SubscribeLocalEvent<MetaDataComponent, IngestedEvent>(OnIngested);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnimalObjectiveTrackerComponent, TransformComponent>();
        while (query.MoveNext(out _, out var tracker, out var xform))
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

    private void OnIngested(EntityUid food, MetaDataComponent comp, ref IngestedEvent args)
    {
        if (!TryComp<AnimalObjectiveTrackerComponent>(args.Target, out var tracker))
            return;

        foreach (var (reagent, quantity) in args.Split.Contents)
        {
            var id = reagent.Prototype;
            tracker.DrunkReagents.TryGetValue(id, out var existing);
            tracker.DrunkReagents[id] = existing + quantity;
        }

        if (_edibleQuery.TryComp(food, out var edible) && edible.Edible == IngestionSystem.Drink)
        {
            tracker.DrinkVolume += args.Split.Volume;
            return;
        }

        tracker.EatCount++;

        if (MetaData(food).EntityPrototype is { } foodProto)
        {
            tracker.EatenFoodProtos.Add(foodProto.ID);
            IncrementFoodParentCounts(tracker, foodProto);
        }

        if (TryComp<TagComponent>(food, out var tagComp))
        {
            foreach (var tag in tagComp.Tags)
            {
                tracker.EatenTagCounts.TryGetValue(tag, out var count);
                tracker.EatenTagCounts[tag] = count + 1;
            }
        }

        if (!_tag.HasTag(food, PaperTag))
            return;

        if (IsBlankPaper(food))
            tracker.BlankPaperEaten++;

        tracker.PaperEaten++;
    }

    private void IncrementFoodParentCounts(AnimalObjectiveTrackerComponent tracker, EntityPrototype foodProto)
    {
        var ancestors = new HashSet<ProtoId<EntityPrototype>>();
        CollectAncestors(foodProto, ancestors);

        foreach (var ancestorId in ancestors)
        {
            tracker.EatenFoodParentCounts.TryGetValue(ancestorId, out var count);
            tracker.EatenFoodParentCounts[ancestorId] = count + 1;
        }
    }

    private void CollectAncestors(EntityPrototype prototype, HashSet<ProtoId<EntityPrototype>> ancestors)
    {
        if (!ancestors.Add(prototype.ID) || prototype.Parents == null)
            return;

        foreach (var parentId in prototype.Parents)
        {
            if (!_proto.TryIndex(parentId, out EntityPrototype? parent))
                continue;

            CollectAncestors(parent, ancestors);
        }
    }

    private bool IsBlankPaper(EntityUid paper)
    {
        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return _tag.HasTag(paper, PaperTag);

        return paperComp.StampedBy.Count == 0 && string.IsNullOrWhiteSpace(paperComp.Content);
    }
}
