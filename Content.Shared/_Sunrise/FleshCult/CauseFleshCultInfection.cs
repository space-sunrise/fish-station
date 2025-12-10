using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.Mindshield.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Shared._Sunrise.FleshCult;

public sealed partial class CauseFleshCultInfection : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-flesh-cultist-infection", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        // Fish-start
        if (entityManager.HasComponent<MindShieldComponent>(args.TargetEntity))
        {
            // Inject into chemical solution (bloodstream) specifically, like hyposprays/syringes do
            if (entityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
            {
                var solutionContainerSystem = entityManager.System<SharedSolutionContainerSystem>();
                if (solutionContainerSystem.ResolveSolution(args.TargetEntity, bloodstream.ChemicalSolutionName, ref bloodstream.ChemicalSolution, out var chemSolution))
                {
                    // Remove Carol reagent and replace with Unstable Mutagen
                    chemSolution.RemoveReagent("Carol", FixedPoint2.New(5));
                    chemSolution.AddReagent("UnstableMutagen", FixedPoint2.New(5));
                }
            }
        } // Fish-end
        else
        {
             entityManager.EnsureComponent<PendingFleshCultistComponent>(args.TargetEntity);
        }
    }
}
