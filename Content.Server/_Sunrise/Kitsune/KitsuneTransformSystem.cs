using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._Sunrise.Kitsune;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Robust.Shared.Localization;
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
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneRevertActionEvent>(OnKitsuneRevert);
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneRevertDoAfterEvent>(OnKitsuneRevertDoAfter);
    }

    private void OnKitsuneTransform(EntityUid uid, KitsuneTransformComponent component, KitsuneTransformActionEvent args)
    {
        args.Handled = true;

        if (component.IsTransformed)
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-already-transformed"), uid, uid, PopupType.MediumCaution);
            return;
        }

        // Check if they have enough blood
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionManager))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-no-bloodstream"), uid, uid, PopupType.MediumCaution);
            return;
        }

        /*if (!solutionManager.TryGetSolution("blood", out var bloodSolution))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-no-blood"), uid, uid, PopupType.MediumCaution);
            return;
        }

        var bloodAmount = bloodSolution.GetTotalPrototypeQuantity("Blood");
        if (bloodAmount < 50)
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-insufficient-blood", ("amount", bloodAmount)), uid, uid, PopupType.MediumCaution);
            return;
        }*/

        // Start the do-after
        var doAfterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(5),
            new KitsuneTransformDoAfterEvent(),
            uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-starting"), uid, uid, PopupType.MediumCaution);
        }
    }

    private void OnKitsuneTransformDoAfter(EntityUid uid, KitsuneTransformComponent component, ref KitsuneTransformDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        /*// Check blood again (in case they lost some)
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionManager) ||
            !solutionManager.TryGetSolution("blood", out var bloodSolution))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-lost-blood"), uid, uid, PopupType.MediumCaution);
            return;
        }

        var bloodAmount = bloodSolution.GetTotalPrototypeQuantity("Blood");
        if (bloodAmount < 50)
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-insufficient-mid"), uid, uid, PopupType.MediumCaution);
            return;
        }

        // Consume 50 blood
        bloodSolution.RemoveReagent("Blood", FixedPoint2.New(50));*/

        // Transform into fox
        if (!_prototypeManager.TryIndex<PolymorphPrototype>("KitsuneTransform", out var prototype))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-failed"), uid, uid, PopupType.MediumCaution);
            Logger.Warning($"Kitsune transform failed: could not find 'KitsuneTransform' polymorph prototype");
            return;
        }

        // Store the original entity reference before polymorph
        component.StashedHumanoid = uid;
        component.IsTransformed = true;

        // Perform polymorph
        _polymorph.PolymorphEntity(uid, prototype);

        _popup.PopupEntity(Loc.GetString("kitsune-transform-success"), uid, uid, PopupType.MediumCaution);
    }

    private void OnKitsuneRevert(EntityUid uid, KitsuneTransformComponent component, KitsuneRevertActionEvent args)
    {
        args.Handled = true;

        if (!component.IsTransformed)
        {
            _popup.PopupEntity(Loc.GetString("kitsune-revert-not-transformed"), uid, uid, PopupType.MediumCaution);
            return;
        }

        // Start the do-after for revert
        var doAfterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(5),
            new KitsuneRevertDoAfterEvent(),
            uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1f,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-revert-starting"), uid, uid, PopupType.MediumCaution);
        }
    }

    private void OnKitsuneRevertDoAfter(EntityUid uid, KitsuneTransformComponent component, ref KitsuneRevertDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!component.IsTransformed)
            return;

        // Revert the polymorph
        if (TryComp<PolymorphedEntityComponent>(uid, out var morphComp))
        {
            _polymorph.Revert((uid, morphComp));
            component.IsTransformed = false;
            _popup.PopupEntity(Loc.GetString("kitsune-revert-success"), uid, uid, PopupType.MediumCaution);
        }
    }
}
