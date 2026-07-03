using Content.Server._Fish.Objectives.Systems;
using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Objectives.Components;

/// <summary>
/// Правило раунда для отслеживания игроков-животных и их целей в конце раунда.
/// </summary>
[RegisterComponent, Access(typeof(AnimalObjectivesRuleSystem), typeof(AnimalObjectivesSystem))]
public sealed partial class AnimalObjectivesRuleComponent : Component
{
    [DataField]
    public List<EntityUid> Minds = new();

    [DataField(required: true)]
    public LocId AgentName = string.Empty;

    [DataField(required: true)]
    public List<AnimalObjectiveSet> Sets = new();

    [DataField(required: true)]
    public float MaxDifficulty = 2f;
}

/// <summary>
/// Набор случайных целей для животного.
/// </summary>
[DataRecord]
public partial record struct AnimalObjectiveSet()
{
    [DataField(required: true)]
    public ProtoId<WeightedRandomPrototype> Groups = string.Empty;

    [DataField]
    public float Prob = 1f;

    [DataField]
    public int MaxPicks = 2;
}
