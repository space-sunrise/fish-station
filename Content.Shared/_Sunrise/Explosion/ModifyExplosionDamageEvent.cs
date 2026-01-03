using Content.Shared.Damage;

namespace Content.Shared.Explosion;

/// <summary>
/// Event raised before explosion damage is applied, allowing systems to modify or cancel it.
/// </summary>
[ByRefEvent]
public record struct ModifyExplosionDamageEvent(EntityUid Target, DamageSpecifier Damage, EntityUid? Cause)
{
    public EntityUid Target = Target;
    public DamageSpecifier Damage = Damage;
    public EntityUid? Cause = Cause;
    public bool Cancelled = false;
}
