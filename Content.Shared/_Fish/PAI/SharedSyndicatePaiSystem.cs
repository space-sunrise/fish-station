using System.Diagnostics.CodeAnalysis;
using Content.Shared._Sunrise.SolutionRegenerationSwitcher;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.PAI;

/// <summary>
/// Shared API for Syndicate pAI medical suite (carrier inject, reagent cycle, master).
/// </summary>
public abstract partial class SharedSyndicatePaiSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicatePaiComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SyndicatePaiComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SyndicatePaiComponent, SyndicatePaiOpenMedicalEvent>(OnOpenMedical);
        SubscribeLocalEvent<SyndicatePaiComponent, SyndicatePaiInjectCarrierEvent>(OnInjectCarrierAction);
        SubscribeLocalEvent<SyndicatePaiComponent, SyndicatePaiCycleReagentEvent>(OnCycleReagentAction);
    }

    private void OnMapInit(Entity<SyndicatePaiComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.OpenMedicalActionEntity, ent.Comp.OpenMedicalAction);
        _actions.AddAction(ent.Owner, ref ent.Comp.InjectCarrierActionEntity, ent.Comp.InjectCarrierAction);
        _actions.AddAction(ent.Owner, ref ent.Comp.CycleReagentActionEntity, ent.Comp.CycleReagentAction);
        Dirty(ent);
    }

    private void OnShutdown(Entity<SyndicatePaiComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.OpenMedicalActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.InjectCarrierActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.CycleReagentActionEntity);
    }

    private void OnOpenMedical(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiOpenMedicalEvent args)
    {
        if (args.Handled)
            return;

        _ui.TryToggleUi(ent.Owner, SyndicatePaiUiKey.Key, args.Performer);
        args.Handled = true;
        UpdateUiState(ent);
    }

    private void OnInjectCarrierAction(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiInjectCarrierEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryInjectCarrier(ent, args.Performer);
    }

    private void OnCycleReagentAction(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiCycleReagentEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryCycleReagent(ent, args.Performer);
    }

    /// <summary>
    /// Inject current hypo contents into the carrier or bound master.
    /// </summary>
    public bool TryInjectCarrier(Entity<SyndicatePaiComponent> ent, EntityUid user, bool quiet = false)
    {
        if (!CanInjectCarrier(ent, user, out var target, out var hypo, quiet))
            return false;

        var hypoUid = hypo!.Value;
        var targetUid = target!.Value;

        _interaction.InteractUsing(
            user,
            hypoUid,
            targetUid,
            Transform(targetUid).Coordinates,
            checkCanInteract: false,
            checkCanUse: false,
            needHand: false);

        UpdateUiState(ent);
        return true;
    }

    public bool CanInjectCarrier(
        Entity<SyndicatePaiComponent> ent,
        EntityUid user,
        out EntityUid? target,
        out EntityUid? hypo,
        bool quiet = false)
    {
        target = null;
        hypo = null;

        if (!TryGetHypo(ent, out hypo) || hypo == null)
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("syndicate-pai-no-hypo"), ent.Owner, user);
            return false;
        }

        if (!TryGetInjectionTarget(ent, out target) || target == null)
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("syndicate-pai-no-carrier"), ent.Owner, user);
            return false;
        }

        if (!HasComp<BloodstreamComponent>(target.Value))
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("syndicate-pai-target-not-organic"), ent.Owner, user);
            return false;
        }

        return true;
    }

    public bool TryCycleReagent(Entity<SyndicatePaiComponent> ent, EntityUid user, bool quiet = false)
    {
        if (!CanCycleReagent(ent, user, out var hypo, out var switcher, quiet))
            return false;

        switcher.CurrentIndex = (switcher.CurrentIndex + 1) % switcher.Options.Count;
        return TrySelectReagent(ent, user, switcher.CurrentIndex, quiet);
    }

    public bool CanCycleReagent(
        Entity<SyndicatePaiComponent> ent,
        EntityUid user,
        out EntityUid? hypo,
        [NotNullWhen(true)] out SolutionRegenerationSwitcherComponent? switcher,
        bool quiet = false)
    {
        hypo = null;
        switcher = null;

        if (!TryGetHypo(ent, out hypo) || hypo == null)
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("syndicate-pai-no-hypo"), ent.Owner, user);
            return false;
        }

        if (!TryComp(hypo.Value, out switcher) || switcher.Options.Count <= 1)
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("syndicate-pai-no-reagents"), ent.Owner, user);
            return false;
        }

        return true;
    }

    public bool TrySelectReagent(Entity<SyndicatePaiComponent> ent, EntityUid user, int index, bool quiet = false)
    {
        if (!TryGetHypo(ent, out var hypo) || hypo == null)
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("syndicate-pai-no-hypo"), ent.Owner, user);
            return false;
        }

        if (!TryComp<SolutionRegenerationSwitcherComponent>(hypo.Value, out var switcher))
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("syndicate-pai-no-reagents"), ent.Owner, user);
            return false;
        }

        if (index < 0 || index >= switcher.Options.Count)
            return false;

        if (!TryComp<SolutionRegenerationComponent>(hypo.Value, out var regeneration))
            return false;

        var reagent = switcher.Options[index];
        if (regeneration.Generated.ContainsReagent(reagent.Reagent))
        {
            if (!quiet)
                _popup.PopupClient(Loc.GetString("solution-regeneration-switcher-already-select"), ent.Owner, user);
            return false;
        }

        if (!switcher.KeepSolution &&
            _solutions.TryGetSolution(hypo.Value, regeneration.SolutionName, out var solution))
        {
            _solutions.RemoveAllSolution(solution.Value);
        }

        regeneration.ChangeGenerated(reagent);
        switcher.CurrentIndex = index;
        Dirty(hypo.Value, switcher);

        if (_prototypes.TryIndex(reagent.Reagent.Prototype, out ReagentPrototype? proto) && !quiet)
        {
            _popup.PopupClient(
                Loc.GetString("solution-regeneration-switcher-switched", ("reagent", proto.LocalizedName)),
                ent.Owner,
                user);
        }

        UpdateUiState(ent);
        return true;
    }

    public bool TryGetHypo(Entity<SyndicatePaiComponent> ent, out EntityUid? hypo)
    {
        hypo = null;

        if (!_container.TryGetContainer(ent.Owner, SyndicatePaiComponent.InnateItemContainerId, out var container))
            return false;

        foreach (var contained in container.ContainedEntities)
        {
            if (!HasComp<InjectorComponent>(contained))
                continue;

            // Предпочитаем гипо с переключателем реагентов (медицинский набор)
            if (HasComp<SolutionRegenerationSwitcherComponent>(contained) ||
                HasComp<SolutionRegenerationComponent>(contained))
            {
                hypo = contained;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Carrier (holder) first, then bound master if organic and present.
    /// </summary>
    public bool TryGetInjectionTarget(Entity<SyndicatePaiComponent> ent, out EntityUid? target)
    {
        target = null;

        if (TryGetCarrier(ent.Owner, out var carrier) &&
            carrier != null &&
            HasComp<BloodstreamComponent>(carrier.Value))
        {
            target = carrier;
            return true;
        }

        if (ent.Comp.Master is { Valid: true } master &&
            !TerminatingOrDeleted(master) &&
            HasComp<BloodstreamComponent>(master))
        {
            target = master;
            return true;
        }

        return false;
    }

    public bool TryGetCarrier(EntityUid pai, out EntityUid? carrier)
    {
        carrier = null;
        var current = Transform(pai).ParentUid;

        while (current.IsValid() && !TerminatingOrDeleted(current))
        {
            if (HasComp<HandsComponent>(current) ||
                HasComp<InventoryComponent>(current) ||
                HasComp<StorageComponent>(current))
            {
                if (HasComp<MobStateComponent>(current) || HasComp<BloodstreamComponent>(current))
                {
                    carrier = current;
                    return true;
                }
            }

            var parent = Transform(current).ParentUid;
            if (parent == current)
                break;
            current = parent;
        }

        return false;
    }

    public void TryImprintMaster(Entity<SyndicatePaiComponent> ent, EntityUid master, EntityUid user)
    {
        if (!HasComp<BloodstreamComponent>(master))
        {
            _popup.PopupClient(Loc.GetString("syndicate-pai-imprint-failed"), ent.Owner, user);
            return;
        }

        ent.Comp.Master = master;
        Dirty(ent);
        _popup.PopupClient(
            Loc.GetString("syndicate-pai-imprint-success", ("master", Identity.Name(master, EntityManager))),
            ent.Owner,
            user);
        UpdateUiState(ent);
    }

    public void SetSupplementalDirective(Entity<SyndicatePaiComponent> ent, string? directive)
    {
        ent.Comp.SupplementalDirective = string.IsNullOrWhiteSpace(directive) ? null : directive.Trim();
        Dirty(ent);
        UpdateUiState(ent);
    }

    protected void UpdateUiState(Entity<SyndicatePaiComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, SyndicatePaiUiKey.Key))
            return;

        var state = BuildUiState(ent);
        _ui.SetUiState(ent.Owner, SyndicatePaiUiKey.Key, state);
    }

    protected SyndicatePaiBoundUserInterfaceState BuildUiState(Entity<SyndicatePaiComponent> ent)
    {
        var state = new SyndicatePaiBoundUserInterfaceState
        {
            SupplementalDirective = ent.Comp.SupplementalDirective,
            CurrentReagentIndex = 0,
        };

        if (TryGetCarrier(ent.Owner, out var carrier) && carrier != null)
            state.CarrierName = Identity.Name(carrier.Value, EntityManager);

        if (ent.Comp.Master is { Valid: true } master && !TerminatingOrDeleted(master))
            state.MasterName = Identity.Name(master, EntityManager);

        if (!TryGetHypo(ent, out var hypo) || hypo == null)
            return state;

        if (TryComp<SolutionRegenerationSwitcherComponent>(hypo.Value, out var switcher))
        {
            state.CurrentReagentIndex = switcher.CurrentIndex;
            for (var i = 0; i < switcher.Options.Count; i++)
            {
                var option = switcher.Options[i];
                var name = option.Reagent.Prototype;
                if (_prototypes.TryIndex(option.Reagent.Prototype, out ReagentPrototype? proto))
                    name = proto.LocalizedName;

                state.Reagents.Add(new SyndicatePaiReagentEntry
                {
                    Id = option.Reagent.Prototype,
                    Name = name,
                    Index = i,
                });
            }
        }

        if (TryComp<SolutionRegenerationComponent>(hypo.Value, out var regen) &&
            _solutions.TryGetSolution(hypo.Value, regen.SolutionName, out _, out var solution))
        {
            state.CurrentVolume = solution.Volume.Float();
            state.MaxVolume = solution.MaxVolume.Float();
            if (solution.Contents.Count > 0)
            {
                var primary = solution.GetPrimaryReagentId();
                if (primary != null && _prototypes.TryIndex(primary.Value.Prototype, out ReagentPrototype? current))
                    state.CurrentReagent = current.LocalizedName;
                else if (primary != null)
                    state.CurrentReagent = primary.Value.Prototype;
            }
        }

        return state;
    }
}
