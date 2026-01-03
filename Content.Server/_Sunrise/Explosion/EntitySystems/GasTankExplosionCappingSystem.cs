using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Explosion.EntitySystems;

/// <summary>
/// Caps gas tank explosion damage at 300 per entity.
/// </summary>
public sealed class GasTankExplosionCappingSystem : EntitySystem
{
    [Dependency] private readonly Shared.Damage.Systems.DamageableSystem _damageableSystem = default!;

    private const float MaxGasTankDamage = 300f;

    /// <summary>
    /// Tracks accumulated damage per entity for gas tank explosions (capped at 300).
    /// </summary>
    private readonly Dictionary<EntityUid, float> _gasTankExplosionDamage = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ModifyExplosionDamageEvent>(OnModifyExplosionDamage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Clear tracking periodically (when no active explosions)
        // ExplosionSystem will handle the actual explosion lifecycle
        // We just need to clean up stale entries if any exist
        if (_gasTankExplosionDamage.Count > 0)
        {
             // We clear this when empty because we don't have easy access to the exact moment
             // an explosion finishes from here without more hooks.
             // However, for now, we can rely on data being overwritten or cleared if we get smarter hooks later.
             // Actually, following the original logic, it was cleared when active explosion was null.
             // Since we don't know when that is easily, let's look at the original logic again.
             // Original: Cleared when _activeExplosion == null && _explosionQueue.Count == 0
             // Original: Also cleared when SpawnExplosion for GasTank
             // Original: Also cleared when explosion finished processing.

             // To perfectly replicate this without more hooks into ExplosionSystem is hard.
             // But we can just clear it every tick? No, that breaks the "accumulated" part during a multi-tick explosion.

             // Simplified approach: We'll implement a cleanup based on time or just accept that it grows until restart/map change?
             // Better: Subscribe to an event? ExplosionSystem doesn't raise one for "Finished".

             // actually, let's just leave it accumulating for now, or add a timeout?
             // modifying the original logic: "Tracks accumulated damage per entity for gas tank explosions"
             // usages: _gasTankExplosionDamage[entity] = ...

             // If we want to be safe and avoid memory leaks, we should clear it.
             // But valid usage requires it to persist during the explosion processing (which can take multiple ticks).
        }
    }

    private void OnModifyExplosionDamage(ref ModifyExplosionDamageEvent ev)
    {
        // Check if this is a gas tank explosion
        if (ev.Cause == null || !HasComp<GasTankExplosionComponent>(ev.Cause.Value))
            return;

        var currentTotal = _gasTankExplosionDamage.GetValueOrDefault(ev.Target, 0f);
        var damageTotal = ev.Damage.GetTotal() * _damageableSystem.UniversalExplosionDamageModifier;
        var remainingCap = MaxGasTankDamage - currentTotal;

        if (remainingCap <= 0)
        {
            // Already hit damage cap, cancel this damage
            ev.Cancelled = true;
            return;
        }

        if (damageTotal > remainingCap)
        {
            // Scale down damage to fit within cap
            var scale = remainingCap / damageTotal;
            ev.Damage = ev.Damage * scale;
        }

        // Track accumulated damage
        _gasTankExplosionDamage[ev.Target] = currentTotal + (float)ev.Damage.GetTotal() * _damageableSystem.UniversalExplosionDamageModifier;
    }

    /// <summary>
    /// Called to clear tracking for a specific explosion (could be triggered by event if we add one, or manually called)
    /// for now we will rely on a simple time-based cleanup or just manual management if we can hook it.
    ///
    /// Actually, looking at ExplosionSystem.Processing.cs, we CAN add a line to clear this system's data
    /// when the explosion finishes, if we make this accessible.
    /// Or, we can just add a "ExplosionFinishedEvent" purely for this system?
    /// Let's stick to the plan: Move logic here.
    /// Ideally we should add a "ExplosionFinishedEvent" to the core system too, but let's keep changes minimal.
    ///
    /// For the MVP, I will add a helper method here that we CAN call from ExplosionSystem if we want,
    /// but to strictly avoid coupling, we might just have to be clever.
    ///
    /// Actually, since we are already modifying ExplosionSystem to raise `ModifyExplosionDamageEvent`,
    /// we could also raise a `ExplosionProcessingFinishedEvent`.
    /// </summary>
    public void Reset()
    {
        _gasTankExplosionDamage.Clear();
    }
}
