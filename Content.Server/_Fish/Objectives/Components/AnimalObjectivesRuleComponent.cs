using Content.Server._Fish.Objectives.Systems;
using Content.Server.Antag.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Objectives.Components;

[RegisterComponent, Access(typeof(AnimalObjectivesRuleSystem), typeof(AnimalObjectivesSystem))]
public sealed partial class AnimalObjectivesRuleComponent : Component
{
    [DataField]
    public List<EntityUid> Minds = new();

    [DataField(required: true)]
    public LocId AgentName = string.Empty;

    [DataField(required: true)]
    public List<EntProtoId> EligiblePrototypes = new();

    [DataField(required: true)]
    public List<AntagObjectiveSet> Sets = new();

    [DataField(required: true)]
    public float MaxDifficulty = 2f;
}
