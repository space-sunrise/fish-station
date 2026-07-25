using Content.Server.Mech.Equipment.Components;
using Content.Shared._Fish.Mechs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Server._Fish.Mechs;

/// <summary>
/// Лечение пациентов в mech sleeper (grabber container).
/// </summary>
public sealed class MechMedicalSleeperSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextTick;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextTick)
            return;

        _nextTick = _timing.CurTime + TimeSpan.FromSeconds(1);

        var query = EntityQueryEnumerator<MechMedicalSleeperComponent, MechGrabberComponent>();
        while (query.MoveNext(out var uid, out var sleeper, out var grabber))
        {
            foreach (var patient in grabber.ItemContainer.ContainedEntities)
            {
                if (!HasComp<MobStateComponent>(patient))
                    continue;

                _damageable.TryChangeDamage(patient, sleeper.HealPerSecond);
            }
        }
    }
}
