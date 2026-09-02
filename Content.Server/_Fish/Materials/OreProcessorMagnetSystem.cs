using Content.Server.Materials;
using Content.Server.Power.EntitySystems;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server._Fish.Materials;

/// <summary>
/// Добавляет переработчику ПКМ-действие для сбора руды с пола.
/// </summary>
public sealed partial class OreProcessorMagnetSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MaterialStorageSystem _materialStorage = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _nearby = [];
    private EntityQuery<ActiveOreProcessorMagnetComponent> _activeQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _activeQuery = GetEntityQuery<ActiveOreProcessorMagnetComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<OreProcessorMagnetComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<OreProcessorMagnetComponent, ComponentShutdown>(OnMagnetShutdown);
    }

    private void OnGetVerbs(Entity<OreProcessorMagnetComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        var powered = _power.IsPowered(ent);
        var active = _activeQuery.HasComp(ent);
        string? message = null;

        if (!powered)
            message = Loc.GetString("ore-processor-magnet-no-power");
        else if (active)
            message = Loc.GetString("ore-processor-magnet-already-active");

        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("ore-processor-magnet-verb"),
            Disabled = !powered || active,
            Message = message,
            Act = () => TryActivateMagnet((ent.Owner, null), user),
        });
    }

    private void OnMagnetShutdown(Entity<OreProcessorMagnetComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<ActiveOreProcessorMagnetComponent>(ent);
    }

    public bool TryActivateMagnet(Entity<OreProcessorMagnetComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !CanActivateMagnet((ent.Owner, ent.Comp), user))
            return false;

        ActivateMagnet((ent.Owner, ent.Comp), user);
        return true;
    }

    private bool CanActivateMagnet(Entity<OreProcessorMagnetComponent> ent, EntityUid user)
    {
        if (!Exists(user))
            return false;

        if (_activeQuery.HasComp(ent))
        {
            _popup.PopupEntity(Loc.GetString("ore-processor-magnet-already-active"), ent, user);
            return false;
        }

        if (_power.IsPowered(ent))
            return true;

        _popup.PopupEntity(Loc.GetString("ore-processor-magnet-no-power"), ent, user);
        return false;
    }

    private void ActivateMagnet(Entity<OreProcessorMagnetComponent> ent, EntityUid user)
    {
        var active = EnsureComp<ActiveOreProcessorMagnetComponent>(ent);
        active.EndTime = _timing.CurTime + ent.Comp.Duration;
        active.NextScan = _timing.CurTime;
        active.User = user;
        active.CollectedAny = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveOreProcessorMagnetComponent, OreProcessorMagnetComponent>();

        while (query.MoveNext(out var uid, out var active, out var magnet))
        {
            var ent = new Entity<OreProcessorMagnetComponent>(uid, magnet);

            if (!Exists(active.User))
            {
                StopMagnet((uid, active), false);
                continue;
            }

            if (!_power.IsPowered(ent))
            {
                StopMagnet((uid, active), false);
                continue;
            }

            if (currentTime >= active.EndTime)
            {
                StopMagnet((uid, active), true);
                continue;
            }

            if (currentTime < active.NextScan)
                continue;

            active.NextScan = currentTime + magnet.ScanInterval;
            active.CollectedAny |= CollectNearbyOre(ent, active.User);
        }
    }

    private bool CollectNearbyOre(Entity<OreProcessorMagnetComponent> ent, EntityUid user)
    {
        if (!TryComp<MaterialStorageComponent>(ent, out var materialStorage))
            return false;

        _nearby.Clear();
        _lookup.GetEntitiesInRange(
            ent,
            ent.Comp.Range,
            _nearby,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Uncontained);

        var processorXform = Transform(ent);
        var finalCoordinates = processorXform.Coordinates;
        var moverCoordinates = _transform.GetMoverCoordinates(ent, processorXform);
        var inserted = false;

        foreach (var ore in _nearby)
        {
            if (TerminatingOrDeleted(ore))
                continue;

            if (!_physicsQuery.TryComp(ore, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                continue;

            var oreXform = Transform(ore);
            var oreMapCoordinates = _transform.GetMapCoordinates(ore, oreXform);
            var initialCoordinates = _transform.ToCoordinates(moverCoordinates.EntityId, oreMapCoordinates);
            var initialRotation = oreXform.LocalRotation;

            if (!_materialStorage.TryInsertMaterialEntity(user, ore, ent, materialStorage))
                continue;

            _storage.PlayPickupAnimation(ore, initialCoordinates, finalCoordinates, initialRotation, user);
            inserted = true;
        }

        return inserted;
    }

    private void StopMagnet(Entity<ActiveOreProcessorMagnetComponent> ent, bool showEmptyMessage)
    {
        var user = ent.Comp.User;
        var collectedAny = ent.Comp.CollectedAny;

        RemCompDeferred<ActiveOreProcessorMagnetComponent>(ent);

        if (showEmptyMessage && !collectedAny && Exists(user))
            _popup.PopupEntity(Loc.GetString("ore-processor-magnet-no-ore"), ent, user);
    }

}
