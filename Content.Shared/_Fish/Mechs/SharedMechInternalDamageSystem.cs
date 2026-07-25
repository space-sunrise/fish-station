using Content.Shared._Fish.Mechs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Fish.Mechs;

/// <summary>
/// Internal damage: ролл при уроне, control-lost, short-circuit/fire (тики на сервере).
/// </summary>
public abstract class SharedMechInternalDamageSystem : EntitySystem
{
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly SharedMechSystem Mech = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly INetManager Net = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ToolQualityPrototype> CuttingQuality = "Cutting";
    private static readonly ProtoId<ToolQualityPrototype> WeldingQuality = "Welding";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechInternalDamageComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MechInternalDamageComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<MechInternalDamageComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnDamageChanged(Entity<MechInternalDamageComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (!TryComp(ent, out MechComponent? mech) || mech.MaxIntegrity <= 0)
            return;

        var ratio = (mech.Integrity / mech.MaxIntegrity).Float();
        if (ratio < ent.Comp.IntegrityThreshold)
            return;

        if (!Random.Prob(ent.Comp.DamageChance))
            return;

        var flag = PickDamageFlag(args);
        if ((ent.Comp.Damage & flag) == flag)
            return;

        ent.Comp.Damage |= flag;
        Dirty(ent);

        if (mech.PilotSlot.ContainedEntity is { } pilot)
            Popup.PopupEntity(Loc.GetString("mech-internal-damage-applied"), ent, pilot);
    }

    private MechInternalDamageFlags PickDamageFlag(DamageChangedEvent args)
    {
        FixedPoint2 heat = FixedPoint2.Zero;
        FixedPoint2 blunt = FixedPoint2.Zero;
        if (args.DamageDelta != null)
        {
            args.DamageDelta.DamageDict.TryGetValue("Heat", out heat);
            args.DamageDelta.DamageDict.TryGetValue("Blunt", out blunt);
        }

        if (heat > blunt)
            return MechInternalDamageFlags.Fire | MechInternalDamageFlags.TempControl;

        if (blunt > FixedPoint2.Zero && Random.Prob(0.4f))
            return MechInternalDamageFlags.ControlLost;

        if (Random.Prob(0.35f))
            return MechInternalDamageFlags.ShortCircuit;

        return MechInternalDamageFlags.TankBreach;
    }

    private void OnMoveInput(Entity<MechInternalDamageComponent> ent, ref MoveInputEvent args)
    {
        if ((ent.Comp.Damage & MechInternalDamageFlags.ControlLost) == 0)
            return;

        if (!args.HasDirectionalMovement)
            return;

        if (Random.Prob(0.35f))
            _transform.SetLocalRotation(ent, Random.NextAngle());
    }

    private void OnInteractUsing(Entity<MechInternalDamageComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if ((ent.Comp.Damage & MechInternalDamageFlags.ShortCircuit) != 0 &&
            _tools.HasQuality(args.Used, CuttingQuality))
        {
            ClearFlag(ent, MechInternalDamageFlags.ShortCircuit);
            args.Handled = true;
            Popup.PopupClient(Loc.GetString("mech-internal-damage-repaired-short"), ent, args.User);
            return;
        }

        if ((ent.Comp.Damage & (MechInternalDamageFlags.TankBreach | MechInternalDamageFlags.Fire)) != 0 &&
            _tools.HasQuality(args.Used, WeldingQuality))
        {
            ClearFlag(ent, MechInternalDamageFlags.TankBreach | MechInternalDamageFlags.Fire | MechInternalDamageFlags.TempControl);
            args.Handled = true;
            Popup.PopupClient(Loc.GetString("mech-internal-damage-repaired-breach"), ent, args.User);
        }
    }

    public void ClearFlag(Entity<MechInternalDamageComponent> ent, MechInternalDamageFlags flags)
    {
        ent.Comp.Damage &= ~flags;
        Dirty(ent);
    }
}
