// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt;

using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared._Sunrise.Razor;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Razor;

public sealed class RazorSystem : SharedRazorSystem
{
    private const string BarberChairPrototype = "ChairBarber";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<RazorComponent>(RazorUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUiClosed);
            subs.Event<RazorSelectMessage>(OnRazorSelect);
            subs.Event<RazorAddSlotMessage>(OnTryRazorAddSlot);
            subs.Event<RazorRemoveSlotMessage>(OnTryRazorRemoveSlot);
        });


        SubscribeLocalEvent<RazorComponent, RazorSelectDoAfterEvent>(OnSelectSlotDoAfter);
        SubscribeLocalEvent<RazorComponent, RazorRemoveSlotDoAfterEvent>(OnRemoveSlotDoAfter);
        SubscribeLocalEvent<RazorComponent, RazorAddSlotDoAfterEvent>(OnAddSlotDoAfter);
    }

    private void OnRazorSelect(EntityUid uid, RazorComponent component, RazorSelectMessage message)
    {
        if (component.Target is not { } target)
            return;

        CancelCurrentDoAfter(component);

        var doAfter = new RazorSelectDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
            Marking = message.Marking,
        };

        var time = GetDoAfterTime(target, component.SelectSlotTime);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Actor, time, doAfter, uid, target: target, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = false,
            NeedHand = true
        },
            out var doAfterId);

        component.DoAfter = doAfterId;
        _audio.PlayPvs(component.ChangeHairSound, uid);
    }

    private void OnSelectSlotDoAfter(EntityUid uid, RazorComponent component, RazorSelectDoAfterEvent args)
    {
        component.DoAfter = null;

        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        if (!TryGetMarkingCategory(args.Category, out var category))
            return;

        _humanoid.SetMarkingId(component.Target.Value, category, args.Slot, args.Marking);

        UpdateInterface(uid, component.Target.Value, component);
    }

    private void OnTryRazorRemoveSlot(EntityUid uid, RazorComponent component, RazorRemoveSlotMessage message)
    {
        if (component.Target is not { } target)
            return;

        CancelCurrentDoAfter(component);

        var doAfter = new RazorRemoveSlotDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
        };

        var time = GetDoAfterTime(target, component.RemoveSlotTime);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Actor, time, doAfter, uid, target: target, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            NeedHand = true
        },
            out var doAfterId);

        component.DoAfter = doAfterId;
        _audio.PlayPvs(component.ChangeHairSound, uid);
    }

    private void OnRemoveSlotDoAfter(EntityUid uid, RazorComponent component, RazorRemoveSlotDoAfterEvent args)
    {
        component.DoAfter = null;

        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        if (!TryGetMarkingCategory(args.Category, out var category))
            return;

        _humanoid.RemoveMarking(component.Target.Value, category, args.Slot);

        UpdateInterface(uid, component.Target.Value, component);
    }

    private void OnTryRazorAddSlot(EntityUid uid, RazorComponent component, RazorAddSlotMessage message)
    {
        if (component.Target == null)
            return;

        CancelCurrentDoAfter(component);

        var doAfter = new RazorAddSlotDoAfterEvent()
        {
            Category = message.Category,
        };

        var time = GetDoAfterTime(component.Target.Value, component.AddSlotTime);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Actor, time, doAfter, uid, target: component.Target.Value, used: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = false,
            NeedHand = true
        },
            out var doAfterId);

        component.DoAfter = doAfterId;
        _audio.PlayPvs(component.ChangeHairSound, uid);
    }
    private void OnAddSlotDoAfter(EntityUid uid, RazorComponent component, RazorAddSlotDoAfterEvent args)
    {
        component.DoAfter = null;

        if (args.Handled || args.Target == null || args.Cancelled || component.Target != args.Target)
            return;

        if (!TryGetMarkingCategory(args.Category, out var category) ||
            !TryComp<HumanoidAppearanceComponent>(args.Target.Value, out var humanoid))
            return;

        var marking = _markings.MarkingsByCategoryAndSpecies(category, humanoid.Species).Keys.FirstOrDefault();

        if (string.IsNullOrEmpty(marking))
            return;

        _humanoid.AddMarking(args.Target.Value, marking, Color.Black);

        UpdateInterface(uid, args.Target.Value, component);

    }

    private void OnUiClosed(Entity<RazorComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
        Dirty(ent);
    }

    private void CancelCurrentDoAfter(RazorComponent component)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;
    }

    private TimeSpan GetDoAfterTime(EntityUid target, TimeSpan baseTime)
    {
        if (!TryComp<BuckleComponent>(target, out var buckleComponent) ||
            buckleComponent.BuckledTo == null)
        {
            return baseTime;
        }

        var proto = Prototype(buckleComponent.BuckledTo.Value);
        return proto is { ID: BarberChairPrototype } ? baseTime * 0.5f : baseTime;
    }

    private static bool TryGetMarkingCategory(RazorCategory category, out MarkingCategories markingCategory)
    {
        switch (category)
        {
            case RazorCategory.Hair:
                markingCategory = MarkingCategories.Hair;
                return true;
            case RazorCategory.FacialHair:
                markingCategory = MarkingCategories.FacialHair;
                return true;
            default:
                markingCategory = default;
                return false;
        }
    }
}
