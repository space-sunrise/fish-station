using Content.Server._Fish.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Tag;

namespace Content.Server._Fish.Objectives.Systems;

/// <summary>
/// Вычисляет прогресс условий целей животных.
/// </summary>
public sealed class AnimalObjectiveConditionsSystem : EntitySystem
{
    [Dependency] private readonly AnimalObjectiveTrackerSystem _tracker = default!;
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimalEatCountConditionComponent, ObjectiveGetProgressEvent>(OnEatCount);
        SubscribeLocalEvent<AnimalDrinkVolumeConditionComponent, ObjectiveGetProgressEvent>(OnDrinkVolume);
        SubscribeLocalEvent<AnimalDrinkReagentConditionComponent, ObjectiveGetProgressEvent>(OnDrinkReagent);
        SubscribeLocalEvent<AnimalEatTagConditionComponent, ObjectiveGetProgressEvent>(OnEatTag);
        SubscribeLocalEvent<AnimalEatPaperConditionComponent, ObjectiveGetProgressEvent>(OnEatPaper);
        SubscribeLocalEvent<AnimalEatBlankPaperConditionComponent, ObjectiveGetProgressEvent>(OnEatBlankPaper);
        SubscribeLocalEvent<AnimalTileDistanceConditionComponent, ObjectiveGetProgressEvent>(OnTileDistance);
        SubscribeLocalEvent<AnimalExploreGridsConditionComponent, ObjectiveGetProgressEvent>(OnExploreGrids);
        SubscribeLocalEvent<AnimalTryNewFoodConditionComponent, ObjectiveGetProgressEvent>(OnTryNewFood);
    }

    private static float Ratio(int current, int target)
    {
        if (target <= 0)
            return 1f;

        return Math.Min(1f, (float) current / target);
    }

    private static float Ratio(float current, float target)
    {
        if (target <= 0f)
            return 1f;

        return Math.Min(1f, current / target);
    }

    private void OnEatCount(EntityUid uid, AnimalEatCountConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity)
            return;

        args.Progress = Ratio(_tracker.GetEatCount(entity), _number.GetTarget(uid));
    }

    private void OnDrinkVolume(EntityUid uid, AnimalDrinkVolumeConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity)
            return;

        args.Progress = Ratio(_tracker.GetDrinkVolume(entity), _number.GetTarget(uid));
    }

    private void OnDrinkReagent(EntityUid uid, AnimalDrinkReagentConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity)
            return;

        args.Progress = Ratio(_tracker.GetReagentVolume(entity, comp.Reagent), _number.GetTarget(uid));
    }

    private void OnEatTag(EntityUid uid, AnimalEatTagConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity)
            return;

        args.Progress = Ratio(_tracker.GetTagEatCount(entity, comp.Tag), _number.GetTarget(uid));
    }

    private void OnEatPaper(EntityUid uid, AnimalEatPaperConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity)
            return;

        args.Progress = Ratio(_tracker.GetPaperEaten(entity), _number.GetTarget(uid));
    }

    private void OnEatBlankPaper(EntityUid uid, AnimalEatBlankPaperConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity)
            return;

        args.Progress = Ratio(_tracker.GetBlankPaperEaten(entity), _number.GetTarget(uid));
    }

    private void OnTileDistance(EntityUid uid, AnimalTileDistanceConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity)
            return;

        args.Progress = Ratio(_tracker.GetTilesMoved(entity), _number.GetTarget(uid));
    }

    private void OnExploreGrids(EntityUid uid, AnimalExploreGridsConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity)
            return;

        args.Progress = Ratio(_tracker.GetVisitedGridCount(entity), _number.GetTarget(uid));
    }

    private void OnTryNewFood(EntityUid uid, AnimalTryNewFoodConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity is not { } entity)
            return;

        args.Progress = Ratio(_tracker.GetUniqueFoodCount(entity), _number.GetTarget(uid));
    }
}
