using System.Linq;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Mech.Equipment.EntitySystems;
using Content.Server.Mech.Systems;
using Content.Shared._Fish.Mechs;
using Content.Shared._Fish.Mechs.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Fish.Mechs;

/// <summary>
/// Стабилизация пациента в бортовом медмодуле и дозированная инъекция из шасси-резервуаров.
/// </summary>
public sealed class MechMedicalSleeperSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly MechGrabberSystem _grabber = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;

    private TimeSpan _nextTick;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechMedicalSleeperComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
        SubscribeLocalEvent<MechMedicalSleeperComponent, MechEquipmentUiMessageRelayEvent>(OnUiMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextTick)
            return;

        _nextTick = _timing.CurTime + TimeSpan.FromSeconds(1);

        var query = EntityQueryEnumerator<MechMedicalSleeperComponent, MechGrabberComponent>();
        while (query.MoveNext(out _, out var sleeper, out var grabber))
        {
            foreach (var patient in grabber.ItemContainer.ContainedEntities)
            {
                if (!HasComp<MobStateComponent>(patient))
                    continue;

                _damageable.TryChangeDamage(patient, sleeper.HealPerSecond);
            }
        }
    }

    private void OnUiStateReady(EntityUid uid, MechMedicalSleeperComponent component, MechEquipmentUiStateReadyEvent args)
    {
        if (!TryComp<MechGrabberComponent>(uid, out var grabber))
            return;

        var state = new MechMedicalSleeperUiState
        {
            Contents = GetNetEntityList(grabber.ItemContainer.ContainedEntities.ToList()),
            MaxContents = grabber.MaxContents,
            InjectAmount = component.InjectAmount,
            Reagents = CollectReagents(uid, component)
        };

        args.States[GetNetEntity(uid)] = state;
    }

    private void OnUiMessage(EntityUid uid, MechMedicalSleeperComponent component, MechEquipmentUiMessageRelayEvent args)
    {
        if (!TryComp<MechEquipmentComponent>(uid, out var equipment) || equipment.EquipmentOwner is not { } mech)
            return;

        switch (args.Message)
        {
            case MechGrabberEjectMessage eject:
                HandleEject(uid, mech, eject);
                break;
            case MechMedicalSleeperInjectMessage inject:
                HandleInject(uid, mech, component, inject);
                break;
        }
    }

    private void HandleEject(EntityUid sleeper, EntityUid mech, MechGrabberEjectMessage msg)
    {
        if (!TryComp<MechGrabberComponent>(sleeper, out var grabber))
            return;

        var item = GetEntity(msg.Item);
        if (!grabber.ItemContainer.Contains(item))
            return;

        _grabber.RemoveItem(sleeper, mech, item, grabber);
    }

    private void HandleInject(
        EntityUid sleeper,
        EntityUid mech,
        MechMedicalSleeperComponent component,
        MechMedicalSleeperInjectMessage msg)
    {
        if (!TryComp<MechGrabberComponent>(sleeper, out var grabber))
            return;

        var patient = grabber.ItemContainer.ContainedEntities.FirstOrDefault();
        if (patient == default || !HasComp<MobStateComponent>(patient))
        {
            _popup.PopupEntity(Loc.GetString("mech-sleeper-no-patient"), mech, PopupType.MediumCaution);
            return;
        }

        if (!_solutions.TryGetInjectableSolution(patient, out var targetSoln, out _))
        {
            _popup.PopupEntity(Loc.GetString("mech-sleeper-inject-failed"), mech, PopupType.MediumCaution);
            return;
        }

        if (!TryResolveSourceSolution(sleeper, mech, component, msg.ReagentId, out var source))
        {
            _popup.PopupEntity(Loc.GetString("mech-sleeper-no-reagents"), mech, PopupType.MediumCaution);
            return;
        }

        var amount = FixedPoint2.Min(component.InjectAmount, source.Comp.Solution.GetTotalPrototypeQuantity(msg.ReagentId));
        if (amount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("mech-sleeper-no-reagents"), mech, PopupType.MediumCaution);
            return;
        }

        amount = FixedPoint2.Min(amount, targetSoln.Value.Comp.Solution.AvailableVolume);
        if (amount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("mech-sleeper-inject-failed"), mech, PopupType.MediumCaution);
            return;
        }

        var removed = _solutions.RemoveReagent(source, msg.ReagentId, amount);
        if (removed <= FixedPoint2.Zero)
            return;

        var transferred = new Solution(msg.ReagentId, removed);
        _reactive.DoEntityReaction(patient, transferred, ReactionMethod.Injection);
        _solutions.Inject(patient, targetSoln.Value, transferred);

        var reagentName = msg.ReagentId;
        if (_prototypes.TryIndex<ReagentPrototype>(msg.ReagentId, out var proto))
            reagentName = proto.LocalizedName;

        _popup.PopupEntity(
            Loc.GetString("mech-sleeper-injected", ("amount", removed), ("reagent", reagentName), ("patient", patient)),
            mech);

        _mech.UpdateUserInterface(mech);
    }

    private bool TryResolveSourceSolution(
        EntityUid sleeper,
        EntityUid mech,
        MechMedicalSleeperComponent component,
        string reagentId,
        out Entity<SolutionComponent> source)
    {
        source = default;

        // Сначала sibling chem-резервуар на шасси, затем запас самого медмодуля.
        foreach (var candidate in EnumerateInjectionSources(sleeper, mech, component))
        {
            if (candidate.Comp.Solution.GetTotalPrototypeQuantity(reagentId) > FixedPoint2.Zero)
            {
                source = candidate;
                return true;
            }
        }

        return false;
    }

    private IEnumerable<Entity<SolutionComponent>> EnumerateInjectionSources(
        EntityUid sleeper,
        EntityUid mech,
        MechMedicalSleeperComponent component)
    {
        if (TryComp<MechComponent>(mech, out var mechComp))
        {
            foreach (var equipment in mechComp.EquipmentContainer.ContainedEntities)
            {
                if (MetaData(equipment).EntityPrototype?.ID != "MechEquipmentSyringeGun")
                    continue;

                if (_solutions.TryGetSolution(equipment, "chemReserve", out var tank, out _))
                    yield return tank.Value;

                if (!TryComp<StorageComponent>(equipment, out var storage))
                    continue;

                foreach (var item in storage.Container.ContainedEntities)
                {
                    if (_solutions.TryGetSolution(item, "injector", out var injector, out _))
                        yield return injector.Value;
                    else if (_solutions.TryGetSolution(item, "pen", out var pen, out _))
                        yield return pen.Value;
                }
            }
        }

        if (_solutions.TryGetSolution(sleeper, component.SolutionName, out var sleeperSoln, out _))
            yield return sleeperSoln.Value;
    }

    private List<MechSleeperReagentEntry> CollectReagents(EntityUid sleeper, MechMedicalSleeperComponent component)
    {
        var result = new Dictionary<string, MechSleeperReagentEntry>();

        if (!TryComp<MechEquipmentComponent>(sleeper, out var equipment) || equipment.EquipmentOwner is not { } mech)
        {
            if (_solutions.TryGetSolution(sleeper, component.SolutionName, out _, out var onlySleeper))
                AggregateSolutionContents(onlySleeper, result);

            return result.Values.OrderBy(e => e.DisplayName).ToList();
        }

        foreach (var soln in EnumerateInjectionSources(sleeper, mech, component))
            AggregateSolutionContents(soln.Comp.Solution, result);

        return result.Values.OrderBy(e => e.DisplayName).ToList();
    }

    private void AggregateSolutionContents(Solution solution, Dictionary<string, MechSleeperReagentEntry> result)
    {
        foreach (var (reagent, quantity) in solution.GetReagentPrototypes(_prototypes))
        {
            var id = reagent.ID;
            if (result.TryGetValue(id, out var existing))
            {
                existing.Quantity += quantity;
                continue;
            }

            result[id] = new MechSleeperReagentEntry
            {
                ReagentId = id,
                DisplayName = reagent.LocalizedName,
                Quantity = quantity
            };
        }
    }
}
