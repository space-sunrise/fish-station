using Content.Shared._Fish.PerformanceGuardian;
using Content.Shared._Sunrise.Storyteller;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.Shuttles.Events;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Минимальные O(1) счётчики событий. Без профилей игроков и без анализа.
/// </summary>
public sealed class PgCollectorSystem : EntitySystem
{
    private PerformanceGuardianSystem? _guardian;
    private int _events;

    public int TakeEventCount()
    {
        var v = _events;
        _events = 0;
        return v;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMelee);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnProjectile);
        // ThrownEvent: broadcast (directed слот занят ThrownItemSystem)
        SubscribeLocalEvent<ThrownEvent>(OnThrown);
        SubscribeLocalEvent<SunriseExplosionEvent>(OnExplosion);
        SubscribeLocalEvent<FTLStartedEvent>(OnFtl);
    }

    private PerformanceGuardianSystem? Guardian =>
        _guardian ??= EntityManager.SystemOrNull<PerformanceGuardianSystem>();

    private void OnMelee(Entity<MeleeWeaponComponent> ent, ref MeleeHitEvent args)
    {
        if (Guardian is not { CollectorsEnabled: true } || !args.IsHit)
            return;
        _events++;
    }

    private void OnProjectile(Entity<ProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (Guardian is not { CollectorsEnabled: true })
            return;
        _events++;
    }

    private void OnThrown(ref ThrownEvent args)
    {
        if (Guardian is not { CollectorsEnabled: true })
            return;
        _events++;
    }

    private void OnExplosion(SunriseExplosionEvent args)
    {
        if (Guardian is not { CollectorsEnabled: true })
            return;
        _events += 3;
    }

    private void OnFtl(ref FTLStartedEvent args)
    {
        if (Guardian is not { CollectorsEnabled: true })
            return;
        _events += 2;
    }
}
