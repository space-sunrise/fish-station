using Content.Server._Fish.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared._Fish.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server._Fish.Objectives.Systems;

public sealed class AnimalObjectiveConditionsSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimalEatCountConditionComponent, ObjectiveGetProgressEvent>(OnEatCount);
        SubscribeLocalEvent<AnimalDrinkVolumeConditionComponent, ObjectiveGetProgressEvent>(OnDrinkVolume);
        SubscribeLocalEvent<AnimalDrinkReagentConditionComponent, ObjectiveGetProgressEvent>(OnDrinkReagent);
        SubscribeLocalEvent<AnimalEatFoodConditionComponent, ObjectiveGetProgressEvent>(OnEatFood);
        SubscribeLocalEvent<AnimalEatPaperConditionComponent, ObjectiveGetProgressEvent>(OnEatPaper);
        SubscribeLocalEvent<AnimalTileDistanceConditionComponent, ObjectiveGetProgressEvent>(OnTileDistance);
        SubscribeLocalEvent<AnimalExploreGridsConditionComponent, ObjectiveGetProgressEvent>(OnExploreGrids);
        SubscribeLocalEvent<AnimalTryNewFoodConditionComponent, ObjectiveGetProgressEvent>(OnTryNewFood);
    }

    private void OnEatCount(EntityUid uid, AnimalEatCountConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (GetTracker(args) is not { } tracker)
            return;

        args.Progress = GetProgress(tracker.EatCount, _number.GetTarget(uid));
    }

    private void OnDrinkVolume(EntityUid uid, AnimalDrinkVolumeConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (GetTracker(args) is not { } tracker)
            return;

        args.Progress = GetProgress((float) tracker.DrinkVolume, _number.GetTarget(uid));
    }

    private void OnDrinkReagent(EntityUid uid, AnimalDrinkReagentConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (GetTracker(args) is not { } tracker)
            return;

        var volume = tracker.DrunkReagents.GetValueOrDefault(comp.Reagent);

        foreach (var reagent in comp.AlsoReagents)
            volume += tracker.DrunkReagents.GetValueOrDefault(reagent);

        args.Progress = GetProgress((float) volume, _number.GetTarget(uid));
    }

    private void OnEatFood(EntityUid uid, AnimalEatFoodConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (GetTracker(args) is not { } tracker)
            return;

        var current = comp.Tag is { } tag
            ? tracker.EatenTagCounts.GetValueOrDefault(tag)
            : comp.FoodParent is { } foodParent
                ? tracker.EatenFoodParentCounts.GetValueOrDefault(foodParent)
                : 0;

        args.Progress = GetProgress(current, _number.GetTarget(uid));
    }

    private void OnEatPaper(EntityUid uid, AnimalEatPaperConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (GetTracker(args) is not { } tracker)
            return;

        var current = comp.RequireBlank ? tracker.BlankPaperEaten : tracker.PaperEaten;
        args.Progress = GetProgress(current, _number.GetTarget(uid));
    }

    private void OnTileDistance(EntityUid uid, AnimalTileDistanceConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (GetTracker(args) is not { } tracker)
            return;

        args.Progress = GetProgress(tracker.TilesMoved, _number.GetTarget(uid));
    }

    private void OnExploreGrids(EntityUid uid, AnimalExploreGridsConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (GetTracker(args) is not { } tracker)
            return;

        args.Progress = GetProgress(tracker.VisitedGrids.Count, _number.GetTarget(uid));
    }

    private void OnTryNewFood(EntityUid uid, AnimalTryNewFoodConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (GetTracker(args) is not { } tracker)
            return;

        args.Progress = GetProgress(tracker.EatenFoodProtos.Count, _number.GetTarget(uid));
    }

    private AnimalObjectiveTrackerComponent? GetTracker(ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity || !TryComp(entity, out AnimalObjectiveTrackerComponent? tracker))
            return null;

        return tracker;
    }

    private static float GetProgress(int current, int target)
    {
        if (target <= 0)
            return 1f;

        return Math.Clamp((float) current / target, 0f, 1f);
    }

    private static float GetProgress(float current, float target)
    {
        if (target <= 0f)
            return 1f;

        return Math.Clamp(current / target, 0f, 1f);
    }
}
