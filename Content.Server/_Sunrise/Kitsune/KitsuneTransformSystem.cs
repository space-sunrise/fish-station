using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._Sunrise.Kitsune;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Kitsune;

public sealed class KitsuneTransformSystem : EntitySystem
{
    private const float TransformDurationSeconds = 300f; // 5 minutes
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    private ISawmill _sawmill = default!;

    // Dictionary to track when each transformed entity should auto-revert
    private Dictionary<EntityUid, float> _transformDurations = new();

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("kitsune");

        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneTransformActionEvent>(OnKitsuneTransform);
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneTransformDoAfterEvent>(OnKitsuneTransformDoAfter);
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneRevertActionEvent>(OnKitsuneRevert);
        SubscribeLocalEvent<KitsuneTransformComponent, KitsuneRevertDoAfterEvent>(OnKitsuneRevertDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Check for expired transforms
        var expired = new List<EntityUid>();
        var toUpdate = new Dictionary<EntityUid, float>();

        foreach (var (uid, timeLeft) in _transformDurations)
        {
            var newTimeLeft = timeLeft - frameTime;
            if (newTimeLeft <= 0)
                expired.Add(uid);
            else
            {
                toUpdate[uid] = newTimeLeft;
            }
        }

        // Update the timers
        _transformDurations = toUpdate;

        // Auto-revert expired transforms
        foreach (var uid in expired)
        {
            if (TryComp<KitsuneTransformComponent>(uid, out var component) &&
                TryComp<PolymorphedEntityComponent>(uid, out var morphComp))
            {
                _polymorph.Revert((uid, morphComp));
                component.IsTransformed = false;
                _popup.PopupEntity(Loc.GetString("kitsune-transform-expired"), uid, uid, PopupType.MediumCaution);
            }
        }
    }

    private void OnKitsuneTransform(EntityUid uid, KitsuneTransformComponent component, KitsuneTransformActionEvent args)
    {
        args.Handled = true;

        if (component.IsTransformed)
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-already-transformed"), uid, uid, PopupType.MediumCaution);
            return;
        }

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
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/blood.ogg"), uid);
        }
    }

    private void OnKitsuneTransformDoAfter(EntityUid uid, KitsuneTransformComponent component, ref KitsuneTransformDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Transform into fox
        if (!_prototypeManager.TryIndex<PolymorphPrototype>(new ProtoId<PolymorphPrototype>("KitsuneTransform"), out var prototype))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-failed"), uid, uid, PopupType.MediumCaution);
            _sawmill.Warning($"Kitsune transform failed: could not find 'KitsuneTransform' polymorph prototype");
            return;
        }

        // Apply 9 slash damage to self
        var damage = new DamageSpecifier()
        {
            DamageDict = new Dictionary<string, FixedPoint2>
            {
                { "Slash", FixedPoint2.New(9) }
            }
        };
        _damage.TryChangeDamage(uid, damage);

        // Store the original entity reference before polymorph
        component.StashedHumanoid = uid;
        component.IsTransformed = true;

        // Set transform duration timer (5 minutes)
        _transformDurations[uid] = TransformDurationSeconds;

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

        // Clear the duration timer
        _transformDurations.Remove(uid);

        // Revert the polymorph
        if (TryComp<PolymorphedEntityComponent>(uid, out var morphComp))
        {
            _polymorph.Revert((uid, morphComp));
            component.IsTransformed = false;
            _popup.PopupEntity(Loc.GetString("kitsune-revert-success"), uid, uid, PopupType.MediumCaution);
        }
    }
}
