using Content.Server._Fish.Objectives.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Objectives.Components;

/// <summary>
/// Съесть определённое количество раз.
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalEatCountConditionComponent : Component;

/// <summary>
/// Выпить определённый объём жидкости.
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalDrinkVolumeConditionComponent : Component;

/// <summary>
/// Выпить определённый напиток (реагент).
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalDrinkReagentConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent = default!;
}

/// <summary>
/// Съесть еду с определённым тегом.
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalEatTagConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag = default!;
}

/// <summary>
/// Съесть листы бумаги.
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalEatPaperConditionComponent : Component;

/// <summary>
/// Съесть чистую бумагу без печатей и текста.
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalEatBlankPaperConditionComponent : Component;

/// <summary>
/// Пройти определённое количество плиток.
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalTileDistanceConditionComponent : Component;

/// <summary>
/// Посетить несколько разных гридов (зон станции).
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalExploreGridsConditionComponent : Component;

/// <summary>
/// Попробовать несколько разных видов еды.
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectiveConditionsSystem))]
public sealed partial class AnimalTryNewFoodConditionComponent : Component;
