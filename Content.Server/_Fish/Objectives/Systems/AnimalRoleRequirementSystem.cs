using Content.Server._Fish.Objectives.Components;
using Content.Shared._Fish.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server._Fish.Objectives.Systems;

/// <summary>
/// Проверяет, что цель выдаётся только игрокам-животным.
/// </summary>
public sealed class AnimalRoleRequirementSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimalRoleRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, AnimalRoleRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Mind.OwnedEntity is not { } owned || !HasComp<AnimalObjectivesEligibleComponent>(owned))
            args.Cancelled = true;
    }
}
