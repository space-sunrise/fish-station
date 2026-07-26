using Content.Shared._Fish.PerformanceGuardian;
using Content.Shared._Sunrise.Storyteller;
using Content.Shared.Construction;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.Shuttles.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// O(1) gameplay event collectors. No analysis in handlers.
/// </summary>
public sealed class PgCollectorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _players = default!;

    private PerformanceGuardianSystem? _guardian;

    public override void Initialize()
    {
        base.Initialize();

        // Directed: только свободные (comp, event) слоты — глобально один хендлер на пару.
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        // DamageableComponent+DamageChanged уже занят (CarpServant); MobState покрывает живых.
        SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(OnDamageChanged);

        // Broadcast: ThrownEvent поднимается с broadcast=true; directed слот ThrownItem занят ThrownItemSystem.
        SubscribeLocalEvent<ThrownEvent>(OnThrown);
        SubscribeLocalEvent<SunriseExplosionEvent>(OnExplosion);
        SubscribeLocalEvent<FTLStartedEvent>(OnFtlStarted);
        SubscribeLocalEvent<DockEvent>(OnDock);

        // Construction start net messages (spam proxy)
        SubscribeNetworkEvent<TryStartStructureConstructionMessage>(OnStartStructure);
        SubscribeNetworkEvent<TryStartItemConstructionMessage>(OnStartItem);
    }

    private PerformanceGuardianSystem? Guardian =>
        _guardian ??= EntityManager.SystemOrNull<PerformanceGuardianSystem>();

    private void OnMeleeHit(Entity<MeleeWeaponComponent> ent, ref MeleeHitEvent args)
    {
        var g = Guardian;
        if (g == null || !g.CollectorsEnabled || !args.IsHit)
            return;

        g.Counters.Increment(PgMetricCategory.Attack);
        g.Counters.Increment(PgMetricCategory.Collision);
        TryRecordPlayer(args.User, PgMetricCategory.Attack);
    }

    private void OnThrown(ref ThrownEvent args)
    {
        var g = Guardian;
        if (g == null || !g.CollectorsEnabled || !g.SecondaryCollectorsEnabled)
            return;

        g.Counters.Increment(PgMetricCategory.Throw);
        if (args.User is { } user)
            TryRecordPlayer(user, PgMetricCategory.Throw);
    }

    private void OnProjectileHit(Entity<ProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        var g = Guardian;
        if (g == null || !g.CollectorsEnabled)
            return;

        g.Counters.Increment(PgMetricCategory.Projectile);
        g.Counters.Increment(PgMetricCategory.Collision);
        if (args.Shooter is { } shooter)
            TryRecordPlayer(shooter, PgMetricCategory.Projectile);
    }

    private void OnDamageChanged(Entity<MobStateComponent> ent, ref DamageChangedEvent args)
    {
        var g = Guardian;
        if (g == null || !g.CollectorsEnabled || !g.SecondaryCollectorsEnabled)
            return;

        if (!args.DamageIncreased)
            return;

        g.Counters.Increment(PgMetricCategory.Damage);
        if (args.Origin is { } origin)
            TryRecordPlayer(origin, PgMetricCategory.Damage);
    }

    private void OnExplosion(SunriseExplosionEvent args)
    {
        var g = Guardian;
        if (g == null || !g.CollectorsEnabled)
            return;

        g.Counters.Increment(PgMetricCategory.Explosion);
    }

    private void OnFtlStarted(ref FTLStartedEvent args)
    {
        var g = Guardian;
        if (g == null || !g.CollectorsEnabled)
            return;

        g.Counters.Increment(PgMetricCategory.Shuttle);
    }

    private void OnDock(DockEvent args)
    {
        var g = Guardian;
        if (g == null || !g.CollectorsEnabled || !g.SecondaryCollectorsEnabled)
            return;

        g.Counters.Increment(PgMetricCategory.Shuttle);
    }

    private void OnStartStructure(TryStartStructureConstructionMessage msg, EntitySessionEventArgs args)
    {
        var g = Guardian;
        if (g == null || !g.CollectorsEnabled || !g.SecondaryCollectorsEnabled)
            return;

        g.Counters.Increment(PgMetricCategory.Construction);
        g.Profiles.EnsurePlayer(args.SenderSession, _timing.CurTime);
        g.Profiles.Record(args.SenderSession.UserId, PgMetricCategory.Construction, _timing.CurTime);
    }

    private void OnStartItem(TryStartItemConstructionMessage msg, EntitySessionEventArgs args)
    {
        var g = Guardian;
        if (g == null || !g.CollectorsEnabled || !g.SecondaryCollectorsEnabled)
            return;

        g.Counters.Increment(PgMetricCategory.Construction);
        g.Profiles.EnsurePlayer(args.SenderSession, _timing.CurTime);
        g.Profiles.Record(args.SenderSession.UserId, PgMetricCategory.Construction, _timing.CurTime);
    }

    private void TryRecordPlayer(EntityUid uid, PgMetricCategory category)
    {
        var g = Guardian;
        if (g == null)
            return;

        if (!_players.TryGetSessionByEntity(uid, out var session))
            return;

        g.Profiles.EnsurePlayer(session, _timing.CurTime);
        g.Profiles.Record(session.UserId, category, _timing.CurTime);
    }
}
