// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Robust.Shared.Random;
using Content.Shared.Humanoid;
using Content.Shared.Zombies;
using Content.Shared.Inventory;

namespace Content.Shared._Sunrise.Disease;

public abstract class SharedDiseaseRoleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    public override void Initialize()
    {
        base.Initialize();
    }
    public void OnInfect(InfectEvent ev, float probability = 0)
    {
        if (ev.Handled)
            return;

        if (!TryComp<DiseaseRoleComponent>(ev.Performer, out var comp)) return;
        if (!HasComp<HumanoidAppearanceComponent>(ev.Target)) return;
        if (HasComp<DiseaseImmuneComponent>(ev.Target)) return;
        if (HasComp<DiseaseTempImmuneComponent>(ev.Target)) return;
        if (HasComp<SickComponent>(ev.Target)) return;

        var targetEv = new ZombificationResistanceQueryEvent(SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.OUTERCLOTHING);
        RaiseLocalEvent(ev.Target, targetEv);
        if (targetEv.TotalCoefficient < 1.0f)
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
