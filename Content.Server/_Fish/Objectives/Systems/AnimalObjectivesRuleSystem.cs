using System.Linq;
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
        args.Minds = ent.Comp.Minds
            .Where(mindId => Exists(mindId) && TryComp<MindComponent>(mindId, out _))
            .Select(mindId =>
            {
                var mind = Comp<MindComponent>(mindId);
                return (mindId, mind.CharacterName ?? "?");
            })
            .ToList();

        args.AgentName = Loc.GetString(ent.Comp.AgentName);
    }
}
