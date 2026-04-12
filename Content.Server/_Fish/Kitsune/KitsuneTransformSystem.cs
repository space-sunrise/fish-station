using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._Fish.Kitsune;
using Content.Shared._Sunrise.SpriteColor;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Kitsune;

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
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SpriteColorSystem _spriteColor = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    // Dictionary to track when each transformed entity should auto-revert
    private Dictionary<EntityUid, float> _transformDurations = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitsuneTransformComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<KitsuneTransformComponent, ComponentShutdown>(OnShutdown);
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
            if (!TryComp<KitsuneTransformComponent>(uid, out var component) ||
                !TryComp<PolymorphedEntityComponent>(uid, out var morphComp))
                continue;
            _polymorph.Revert((uid, morphComp));
            _popup.PopupEntity(Loc.GetString("kitsune-transform-expired"), uid, uid, PopupType.MediumCaution);
        }
    }

    private void OnMapInit(EntityUid uid, KitsuneTransformComponent component, MapInitEvent args)
    {
        // Grant actions defined in the component
        // This is moved here from ActionGrant to prevent loss during anomaly infection
        foreach (var actionProto in component.Actions)
        {
            EntityUid? actionEnt = null;
            _actions.AddAction(uid, ref actionEnt, actionProto);

            if (actionEnt != null)
            {
                component.ActionEntities.Add(actionEnt.Value);

                // If this is the revert action, set its icon to the parent entity (the humanoid)
                // This makes it look like the vanilla revert action
                if (TryComp<PolymorphedEntityComponent>(uid, out var morphComp) &&
                    _actions.GetAction(actionEnt.Value) is { } action &&
                    _actions.GetEvent(actionEnt.Value) is KitsuneRevertActionEvent)
                {
                    _actions.SetEntityIcon((actionEnt.Value, action.Comp), morphComp.Parent);
                }
            }
        }
    }

    private void OnShutdown(EntityUid uid, KitsuneTransformComponent component, ComponentShutdown args)
    {
        // Remove actions granted by this component
        foreach (var actionEnt in component.ActionEntities)
        {
            _actions.RemoveAction(uid, actionEnt);
        }
    }

    private void OnKitsuneTransform(EntityUid uid, KitsuneTransformComponent component, KitsuneTransformActionEvent args)
    {
        args.Handled = true;

        if (TryComp<PolymorphedEntityComponent>(uid, out _))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-already-transformed"), uid, uid, PopupType.MediumCaution);
            return;
        }

        // Start the do-after
        var doAfterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(TransformDoAfterDurationSeconds),
            new KitsuneTransformDoAfterEvent(),
            uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1f,
            NeedHand = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;
        _popup.PopupEntity(Loc.GetString("kitsune-transform-starting"), uid, uid, PopupType.MediumCaution);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/butcher.ogg"), uid);
    }

    private void OnKitsuneTransformDoAfter(EntityUid uid, KitsuneTransformComponent component, ref KitsuneTransformDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Transform into fox
        if (!_prototypeManager.TryIndex<PolymorphPrototype>(new ProtoId<PolymorphPrototype>("KitsuneTransform"), out var prototype))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-transform-failed"), uid, uid, PopupType.MediumCaution);
            return;
        }

        // Apply 9 slash damage to self
        var damage = new DamageSpecifier()
        {
            DamageDict = new Dictionary<string, FixedPoint2>
            {
                { "Slash", FixedPoint2.New(9) },
            },
        };
        _damage.TryChangeDamage(uid, damage);
        // Store the original entity reference before polymorph
        component.StashedHumanoid = uid;

        // Set transform duration timer
        _transformDurations[uid] = TransformDurationSeconds;

        // Extract radio channels from ears slot before polymorph
        var channels = new HashSet<ProtoId<RadioChannelPrototype>>();
        if (TryComp<InventoryComponent>(uid, out var invComp) &&
            _inventory.TryGetSlotEntity(uid, "ears", out var headsetUid, invComp))
        {
            if (TryComp<EncryptionKeyHolderComponent>(headsetUid, out var keyHolder))
            {
                channels.UnionWith(keyHolder.Channels);
            }
        }

        // Perform polymorph
        var newUid = _polymorph.PolymorphEntity(uid, prototype) ?? throw new ArgumentNullException("_polymorph.PolymorphEntity(uid, prototype)");

        // Apply intrinsic radio if we found any channels
        if (channels.Count > 0)
        {
            var activeRadio = EnsureComp<ActiveRadioComponent>(newUid);
            activeRadio.Channels.UnionWith(channels);

            var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(newUid);
            transmitter.Channels.UnionWith(channels);

            EnsureComp<IntrinsicRadioReceiverComponent>(newUid);
        }

        // Transfer TTS voice to the fox form from the original humanoid's voice
        if (TryComp<TTSComponent>(uid, out var originalTts))
        {
            if (TryComp<TTSComponent>(newUid, out var foxTts))
                foxTts.VoicePrototypeId = originalTts.VoicePrototypeId;
        }

        // Apply the humanoid's hair color to the colored fur layer
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
        {
            // Use CachedHairColor if available, otherwise fallback to SkinColor
            var hairColor = humanoidAppearance.CachedHairColor ?? humanoidAppearance.EyeColor;
            _spriteColor.SetStateColor(newUid, "nine-tail_fox_gray_color", hairColor);
        }

        _popup.PopupEntity(Loc.GetString("kitsune-transform-success"), newUid, newUid, PopupType.MediumCaution);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg"), newUid);
    }

    private void OnKitsuneRevert(EntityUid uid, KitsuneTransformComponent component, KitsuneRevertActionEvent args)
    {
        args.Handled = true;

        if (!TryComp<PolymorphedEntityComponent>(uid, out _))
        {
            _popup.PopupEntity(Loc.GetString("kitsune-revert-not-transformed"), uid, uid, PopupType.MediumCaution);
            return;
        }

        // Start the do-after for revert
        var doAfterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(3),
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

        // Clear the duration timer
        _transformDurations.Remove(uid);

        // Revert the polymorph
        if (!TryComp<PolymorphedEntityComponent>(uid, out var morphComp))
            return;
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg"), uid);
        _popup.PopupEntity(Loc.GetString("kitsune-revert-success"), uid, uid, PopupType.MediumCaution);
        _polymorph.Revert((uid, morphComp));
    }
}
