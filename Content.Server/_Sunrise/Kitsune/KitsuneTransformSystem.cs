using Content.Server.Polymorph.Systems;
using Content.Shared._Sunrise.Kitsune;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Kitsune;

public sealed class KitsuneTransformSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneTransformActionEvent>(OnKitsuneTransform);
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneTransformDoAfterEvent>(OnKitsuneTransformDoAfter);
    }

    private void OnKitsuneTransform(EntityUid uid, KitsuneTransformComponent component, KitsuneTransformActionEvent args)
    {
        args.Handled = true;

        if (component.IsTransformed)
        {
            _popup.PopupEntity("You are already in fox form!", uid, uid, PopupType.MediumCaution);
            return;
        }

        // Check if they have enough blood
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionManager))
        {
            _popup.PopupEntity("You need a bloodstream to transform!", uid, uid, PopupType.MediumCaution);
            return;
        }

        if (!solutionManager.TryGetSolution("blood", out var bloodSolution))
        {
            _popup.PopupEntity("You have no blood!", uid, uid, PopupType.MediumCaution);
            return;
        }

        var bloodAmount = bloodSolution.Comp.Solution.GetTotalPrototypeQuantity("Blood");
        if (bloodAmount < 50)
        {
            _popup.PopupEntity($"You need 50 blood to transform. You have {bloodAmount}.", uid, uid, PopupType.MediumCaution);
            return;
        }

        // Start the do-after
        var doAfterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(5),
            new KitsuneTransformDoAfterEvent(),
            eventTarget: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupEntity("You begin your transformation into fox form...", uid, uid, PopupType.MediumCaution);
        }
    }

    private void OnKitsuneTransformDoAfter(EntityUid uid, KitsuneTransformComponent component, ref KitsuneTransformDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Check blood again (in case they lost some)
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionManager) ||
            !solutionManager.TryGetSolution("blood", out var bloodSolution))
        {
            _popup.PopupEntity("You lost your blood mid-transformation!", uid, uid, PopupType.MediumCaution);
            return;
        }

        var bloodAmount = bloodSolution.Comp.Solution.GetTotalPrototypeQuantity("Blood");
        if (bloodAmount < 50)
        {
            _popup.PopupEntity("You don't have enough blood to transform!", uid, uid, PopupType.MediumCaution);
            return;
        }

        // Consume 50 blood
        bloodSolution.Comp.Solution.RemoveReagent("Blood", FixedPoint2.New(50));

        // Transform into fox
        if (!_prototypeManager.TryIndex<PolymorphPrototype>("KitsuneTransform", out var prototype))
        {
            _popup.PopupEntity("Transformation failed - no transform prototype found!", uid, uid, PopupType.MediumCaution);
            Logger.Warning($"Kitsune transform failed: could not find 'KitsuneTransform' polymorph prototype");
            return;
        }

        // Store the original entity reference before polymorph
        component.StashedHumanoid = uid;
        component.IsTransformed = true;

        // Perform polymorph
        _polymorph.PolymorphEntity(uid, prototype);

        _popup.PopupEntity("You transform into a nine-tailed fox!", uid, uid, PopupType.MediumCaution);
    }
}
