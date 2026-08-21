// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Humanoid;

namespace Content.Shared._Sunrise.Disease;

public abstract class SharedDiseaseRoleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
    public float GetDiseaseProtectionCoefficient(EntityUid uid)
    {
        if (HasComp<DiseaseImmuneComponent>(uid))
            return 0f;

        if (TryComp<DiseaseTempImmuneComponent>(uid, out var tempImmune))
        {
            return Math.Clamp(1f - tempImmune.Prob, 0f, 1f);
        }

        return 1f;
    }

    public void OnInfect(InfectEvent ev, float probability = 0)
    {
        if (ev.Handled)
            return;

        if (!TryComp<DiseaseRoleComponent>(ev.Performer, out var comp)) return;
        if (HasComp<DiseaseImmuneComponent>(ev.Target)) return;
        if (HasComp<SickComponent>(ev.Target)) return;

        var protection = GetDiseaseProtectionCoefficient(ev.Target);
        if (protection < 1.0f)
        {
            return;
        }

        ev.Handled = true;

        var comps = AddComp<SickComponent>(ev.Target);
        comps.owner = ev.Performer;
        Dirty(ev.Target, comps);

        comp.Infected.Add(ev.Target);
        Dirty(ev.Performer, comp);
    }
}
