using System.Numerics;
using Content.Shared._Fish.Mechs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._Fish.Mechs;

/// <summary>
/// Направленная броня и deflect для мехов.
/// </summary>
public abstract class SharedMechFacingArmorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechFacingArmorComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<MechFacingArmorComponent> ent, ref DamageModifyEvent args)
    {
        if (args.Damage.Empty)
            return;

        var side = MechFacingSide.Side;
        if (args.Origin is { } origin && origin != ent.Owner)
            side = GetFacingSide(ent.Owner, origin);

        var coefficient = side switch
        {
            MechFacingSide.Front => ent.Comp.FrontCoefficient,
            MechFacingSide.Back => ent.Comp.BackCoefficient,
            _ => ent.Comp.SideCoefficient,
        };

        var deflectMult = side switch
        {
            MechFacingSide.Front => ent.Comp.FrontDeflectMultiplier,
            MechFacingSide.Back => ent.Comp.BackDeflectMultiplier,
            _ => ent.Comp.SideDeflectMultiplier,
        };

        var deflectChance = (ent.Comp.DeflectChance + ent.Comp.DefenceDeflectBonus) * deflectMult;
        if (deflectChance > 0f && _random.Prob(Math.Clamp(deflectChance, 0f, 0.95f)))
        {
            args.Damage *= 0;
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("mech-facing-armor-deflect"), ent);
            return;
        }

        args.Damage *= coefficient;
    }

    public MechFacingSide GetFacingSide(EntityUid mech, EntityUid origin)
    {
        var mechXform = Transform(mech);
        var originXform = Transform(origin);

        var mechPos = _transform.GetWorldPosition(mechXform);
        var originPos = _transform.GetWorldPosition(originXform);
        var delta = originPos - mechPos;
        if (delta.LengthSquared() < 0.0001f)
            return MechFacingSide.Side;

        var toOrigin = delta.ToWorldAngle();
        var facing = _transform.GetWorldRotation(mechXform);
        var abs = Math.Abs(Angle.ShortestDistance(facing, toOrigin).Theta);

        if (abs <= Math.PI / 4)
            return MechFacingSide.Front;

        if (abs >= 3 * Math.PI / 4)
            return MechFacingSide.Back;

        return MechFacingSide.Side;
    }
}

public enum MechFacingSide : byte
{
    Front,
    Side,
    Back,
}
