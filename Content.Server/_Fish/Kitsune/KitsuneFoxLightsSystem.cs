using Content.Shared._Fish.Kitsune;
using Robust.Shared.Timing;
using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;

namespace Content.Server._Fish.Kitsune
{
    public sealed class KitsuneFoxLightsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        TimeSpan castTime = TimeSpan.FromSeconds(1); // 1 second cast time
        TimeSpan lightDuration = TimeSpan.FromSeconds(90);

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<KitsuneFoxLightsComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<KitsuneFoxLightsActionEvent>(OnAction);
            SubscribeLocalEvent<KitsuneFoxLightsComponent, KitsuneFoxLightsDoAfterEvent>(OnDoAfter);
        }

        private void OnAction(KitsuneFoxLightsActionEvent args)
        {
            if (args.Handled) return;
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/butcher.ogg"), args.Performer);

            var comp = EnsureComp<KitsuneFoxLightsComponent>(args.Performer);
            comp.DieAt = TimeSpan.MaxValue;

            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Performer, castTime, new KitsuneFoxLightsDoAfterEvent(), args.Performer)
            {
                BreakOnMove = false,
                BreakOnDamage = false,
                NeedHand = false
            });

            args.Handled = true;
        }

        private void OnDoAfter(EntityUid uid, KitsuneFoxLightsComponent component, KitsuneFoxLightsDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled)
                return;

            component.DieAt = _timing.CurTime + lightDuration;

            if (component.Orbs.Count < 3)
            {
                SpawnOrbs(uid, component);
            }

            // Play Success Sound
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg"), uid);

            // Damage the performer
            var damage = new DamageSpecifier()
            {
                DamageDict = new Dictionary<string, FixedPoint2>
                {
                    { "Slash", FixedPoint2.New(9) }
                }
            };
            _damageable.TryChangeDamage(uid, damage, ignoreResistances: true);
            args.Handled = true;
        }

        private void SpawnOrbs(EntityUid uid, KitsuneFoxLightsComponent component)
        {
            var xform = Transform(uid);
            var coords = xform.Coordinates;

            for (int i = 0; i < 1; i++)
            {
                var orb = Spawn("KitsuneFoxLight", coords);
                var orbComp = EnsureComp<KitsuneFoxLightsOrbComponent>(orb);
                orbComp.Parent = uid;
                orbComp.Angle = 0;
                orbComp.Radius = 1.5f;
                orbComp.Speed = 1f;

                component.Orbs.Add(orb);
            }
        }

        private void OnShutdown(EntityUid uid, KitsuneFoxLightsComponent component, ComponentShutdown args)
        {
            foreach (var orb in component.Orbs)
            {
                if (!Terminating(orb))
                    QueueDel(orb);
            }
            component.Orbs.Clear();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Handle Duration
            var queryComp = EntityQueryEnumerator<KitsuneFoxLightsComponent>();
            while (queryComp.MoveNext(out var uid, out var comp))
            {
                if (_timing.CurTime > comp.DieAt)
                {
                    RemComp<KitsuneFoxLightsComponent>(uid);
                }
            }

            // Handle Movement
            var query = EntityQueryEnumerator<KitsuneFoxLightsOrbComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var orb, out var xform))
            {
                if (!Exists(orb.Parent) || Terminating(orb.Parent))
                {
                    QueueDel(uid);
                    continue;
                }

                // Update Angle
                orb.Angle += orb.Speed * frameTime;
                if (orb.Angle > MathF.PI * 2) orb.Angle -= MathF.PI * 2;

                // Calculate position relative to parent
                var parentXform = Transform(orb.Parent);
                if (parentXform.MapID != xform.MapID)
                {
                    QueueDel(uid); // Parent changed map
                    continue;
                }

                var offset = new Vector2(MathF.Cos(orb.Angle), MathF.Sin(orb.Angle)) * orb.Radius;

                // We set WorldPosition to Parent + Offset.
                // Creating a smooth orbit.
                _transform.SetWorldPosition(xform, _transform.GetWorldPosition(parentXform) + offset);
            }
        }
    }
}
