using Content.Server._Fish.Objectives.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Objectives.Components;

[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalEatCountConditionComponent : Component;

[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalDrinkVolumeConditionComponent : Component;

[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalDrinkReagentConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent = default!;

    [DataField]
    public List<ProtoId<ReagentPrototype>> AlsoReagents = new();
}

[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalEatFoodConditionComponent : Component
{
    [DataField]
    public ProtoId<TagPrototype>? Tag;

    [DataField]
    public ProtoId<EntityPrototype>? FoodParent;
}

[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalEatPaperConditionComponent : Component
{
    [DataField]
    public bool RequireBlank;
}

[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalTileDistanceConditionComponent : Component;

/// <summary>
/// Посетить N именованных мест станции (<see cref="Content.Shared.Warps.WarpPointComponent.Location"/>).
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalVisitLocationsConditionComponent : Component;

[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalTryNewFoodConditionComponent : Component;
