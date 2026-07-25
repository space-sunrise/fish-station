using Content.Server._Fish.Objectives.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Objectives;
using Content.Shared.Mind;

namespace Content.Server._Fish.Objectives.Systems;

public sealed class AnimalObjectivesRuleSystem : GameRuleSystem<AnimalObjectivesRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimalObjectivesRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnObjectivesTextGetInfo(Entity<AnimalObjectivesRuleComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        var minds = new List<(EntityUid, string)>(ent.Comp.Minds.Count);
        foreach (var mindId in ent.Comp.Minds)
        {
            if (!Exists(mindId) || !TryComp<MindComponent>(mindId, out var mind))
                continue;

            minds.Add((mindId, mind.CharacterName ?? "?"));
        }

        args.Minds = minds;
        args.AgentName = Loc.GetString(ent.Comp.AgentName);
    }
}
