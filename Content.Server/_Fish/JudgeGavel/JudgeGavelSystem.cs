using Content.Server.Chat.Systems;
using Content.Server.Damage.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Fish.JudgeGavel;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Pinpointer;
using Content.Shared.StatusEffect;
using Content.Server.Station.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Warps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Fish.JudgeGavel;

/// <summary>
///     System for the Admin Judge Gavel.
/// </summary>
public sealed class JudgeGavelSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly TransformSystem _xformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GodmodeSystem _godmode = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JudgeGavelComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<JudgeGavelComponent, JudgeGavelDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(EntityUid uid, JudgeGavelComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // Prevent multiple concurrent swings
        if (component.ActiveDoAfter != null)
            return;

        StartActivation(uid, component, args.User);
        args.Handled = true;
    }

    private void StartActivation(EntityUid uid, JudgeGavelComponent component, EntityUid user)
    {
        // Force speech
        var chant = Loc.GetString(component.Chant);
        _chat.TrySendInGameICMessage(user, chant, InGameICChatType.Speak, ChatTransmitRange.Normal);

        var ev = new JudgeGavelDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.DoAfterTime, ev, uid)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = true
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId))
        {
            component.ActiveDoAfter = doAfterId;
        }
    }

    private void OnDoAfter(EntityUid uid, JudgeGavelComponent component, JudgeGavelDoAfterEvent args)
    {
        component.ActiveDoAfter = null;

        if (args.Cancelled || args.Handled)
            return;

        MapCoordinates? targetMapCoords = null;
        var identifier = component.CourtroomBeaconId;

        // 1. Destination Discovery: WarpPoint or Beacon lookup
        var warpQuery = EntityQueryEnumerator<WarpPointComponent, TransformComponent>();
        while (warpQuery.MoveNext(out _, out var warp, out var xform))
        {
            if (warp.Location == identifier || (identifier == "station-beacon-courtroom" && warp.Location == "Секторальный суд"))
            {
                targetMapCoords = _transform.ToMapCoordinates(xform.Coordinates);
                break;
            }
        }

        // Priority 2: NavMapBeacon
        if (targetMapCoords == null)
        {
            var beaconQuery = EntityQueryEnumerator<NavMapBeaconComponent, TransformComponent>();
            while (beaconQuery.MoveNext(out _, out var beacon, out var xform))
            {
                if (beacon.DefaultText == identifier)
                {
                    targetMapCoords = _transform.ToMapCoordinates(xform.Coordinates);
                    break;
                }
            }
        }

        // 2. Fallback: Grid + Coordinates lookup
        if (targetMapCoords == null)
        {
            EntityUid? centcommGrid = null;
            var stationQuery = EntityQueryEnumerator<BecomesStationComponent>();
            while (stationQuery.MoveNext(out var gridUid, out var becomes))
            {
                if (becomes.Id == "centcomm")
                {
                    centcommGrid = gridUid;
                    break;
                }
            }

            if (centcommGrid != null)
            {
                var fallbackCoords = new EntityCoordinates(centcommGrid.Value, new System.Numerics.Vector2(28.5f, 36.5f));
                targetMapCoords = _transform.ToMapCoordinates(fallbackCoords);
            }
        }

        if (targetMapCoords == null)
            return;

        var sourceCoords = _transform.GetMapCoordinates(uid);

        // Play effect at source
        Spawn("RadiationPulse", sourceCoords);

        // Deduplicate entities in range to avoid multiple teleports per entity (prevents gibbing/cloning)
        var processed = new HashSet<EntityUid>();
        var ents = _lookup.GetEntitiesInRange(sourceCoords, component.Range);

        foreach (var mob in ents)
        {
            if (!processed.Add(mob))
                continue;

            if (!TryComp<MindContainerComponent>(mob, out var mind) || !mind.HasMind)
                continue;


            // Apply temporary Godmode to prevent collision deaths during teleport overlapping
            _godmode.EnableGodmode(mob);

            // Scheduling removal in 2 seconds
            Timer.Spawn(TimeSpan.FromSeconds(2), () =>
            {
                if (Exists(mob))
                    _godmode.DisableGodmode(mob);
            });

            // Apply Pacified
            _statusEffects.TryAddStatusEffect(mob, "Pacified", TimeSpan.FromSeconds(component.Duration), true);

            // Spread out targets within 3 tiles to prevent stacking
            var offset = _random.NextVector2(3.0f);
            var finalTarget = targetMapCoords.Value.Offset(offset);

            // Clear velocity before teleporting to prevent cannonballing into walls/others upon arrival
            if (TryComp<PhysicsComponent>(mob, out var physics))
            {
                _physics.SetLinearVelocity(mob, System.Numerics.Vector2.Zero, body: physics);
                _physics.SetAngularVelocity(mob, 0f, body: physics);
            }

            // Teleport
            _xformSystem.SetMapCoordinates(mob, finalTarget);

            // Effect at individual destination
            Spawn("RadiationPulse", finalTarget);
        }

        args.Handled = true;
    }
}
