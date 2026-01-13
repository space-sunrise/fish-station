using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._Sunrise.Kitsune;
using Content.Shared._Sunrise.SpriteColor;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
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
    private const float TransformDurationSeconds = 240f; // 4 minutes
    private const float TransformDoAfterDurationSeconds = 3f;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SpriteColorSystem _spriteColor = default!;

    private ISawmill _sawmill = default!;

    // Dictionary to track when each transformed entity should auto-revert
    // Key: The fox entity UID (the one that needs to be reverted)
    // Value: Time remaining in seconds
    private Dictionary<EntityUid, float> _transformDurations = new();

    public override void Initialize()
    {
        base.Initialize();

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
                toUpdate[uid] = newTimeLeft;
        }

        // Update the timers
        _transformDurations = toUpdate;

        // Auto-revert expired transforms
        foreach (var uid in expired)
        {
            // uid here should be the fox entity
            if (TryComp<PolymorphedEntityComponent>(uid, out var morphComp))
            {
                _polymorph.Revert((uid, morphComp));
                _popup.PopupEntity(Loc.GetString("kitsune-transform-expired"), uid, uid, PopupType.MediumCaution);
            }
        }
    }

    private void OnKitsuneTransform(Entity<KitsuneTransformComponent> ent, ref KitsuneTransformActionEvent args)
    {
        args.Handled = true;

        if (TryComp<PolymorphedEntityComponent>(ent, out _))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-already-transformed"), ent, ent, PopupType.MediumCaution);
            return;
        }

        // Start the do-after
        var doAfterArgs = new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(TransformDoAfterDurationSeconds),
            new KitsuneTransformDoAfterEvent(),
            ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1f,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-starting"), ent, ent, PopupType.MediumCaution);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/butcher.ogg"), ent);
        }
    }

    private void OnKitsuneTransformDoAfter(Entity<KitsuneTransformComponent> ent, ref KitsuneTransformDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Transform into fox
        if (!_prototypeManager.TryIndex<PolymorphPrototype>(new ProtoId<PolymorphPrototype>("KitsuneTransform"), out var prototype))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-failed"), ent, ent, PopupType.MediumCaution);
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
        // Fix CS1503: Pass ent.Owner explicitly or use the correct overload if available.
        // Usually Entity<T> can correspond to EntityUid overload, but maybe not for this specific method signature in this codebase version.
        // Using ent.Owner is safe.
        _damage.TryChangeDamage(ent.Owner, damage);

        // Store the original entity reference before polymorph
        ent.Comp.StashedHumanoid = ent.Owner;

        // Perform polymorph
        var newUid = _polymorph.PolymorphEntity(ent, prototype) ?? throw new ArgumentNullException("_polymorph.PolymorphEntity(uid, prototype)");

        // Store the fox entity UID
        ent.Comp.FoxUid = newUid;

        // Set transform duration timer for the FOX entity
        _transformDurations[newUid] = TransformDurationSeconds;

        // Transfer TTS voice to the fox form from the original humanoid's voice
        if (TryComp<TTSComponent>(ent, out var originalTts))
        {
            if (TryComp<TTSComponent>(newUid, out var foxTts))
            {
                foxTts.VoicePrototypeId = originalTts.VoicePrototypeId;
            }
        }

        // Apply the humanoid's hair color to the colored fur layer
        if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoidAppearance))
        {
            // Use CachedHairColor if available, otherwise fallback to SkinColor
            var hairColor = humanoidAppearance.CachedHairColor ?? humanoidAppearance.EyeColor;
            _spriteColor.SetStateColor(newUid, "nine-tail_fox_gray_color", hairColor);
        }

        _popup.PopupEntity(Loc.GetString("kitsune-transform-success"), newUid, newUid, PopupType.MediumCaution);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg"), newUid);
    }

    private void OnKitsuneRevert(Entity<KitsuneTransformComponent> ent, ref KitsuneRevertActionEvent args)
    {
        args.Handled = true;

        // This event is raised on the original humanoid (ent), because the action is likely still attached to it (or the player mind).
        // But the entity effectively active in the world is the FoxUid.
        // We need to check if we have a valid fox entity that is currently polymorphed.

        if (ent.Comp.FoxUid == null || !TryComp<PolymorphedEntityComponent>(ent.Comp.FoxUid, out _))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-revert-not-transformed"), ent, ent, PopupType.MediumCaution);
            return;
        }

        var foxUid = ent.Comp.FoxUid.Value;

        // Start the do-after for revert
        // We attach the do-after to the original entity (ent) because that's where the event handler is running,
        // but we'll use the fox entity (foxUid) for the target logic in the specific event handling if needed.
        // Actually, for visual feedback (cast bar), it should probably be on the entity the player is controlling... which is the fox.
        // The event args might give us the performer.

        var performer = args.Performer; // Expected to be the player entity (fox?)

        // If the performer is the fox, we can use that.
        // But let's stick to using the stored FoxUid to be safe about who we are reverting.

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(3),
            new KitsuneRevertDoAfterEvent(),
            ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1f,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-revert-starting"), foxUid, foxUid, PopupType.MediumCaution);
        }
    }

    private void OnKitsuneRevertDoAfter(Entity<KitsuneTransformComponent> ent, ref KitsuneRevertDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.FoxUid == null)
            return;

        var foxUid = ent.Comp.FoxUid.Value;

        // Clear the duration timer
        _transformDurations.Remove(foxUid);

        // Revert the polymorph on the fox entity
        if (!TryComp<PolymorphedEntityComponent>(foxUid, out var morphComp))
            return;

        // Clean up the reference
        ent.Comp.FoxUid = null;

        _polymorph.Revert((foxUid, morphComp));

        _popup.PopupEntity(Loc.GetString("kitsune-revert-success"), ent, ent, PopupType.MediumCaution);
    }
}
