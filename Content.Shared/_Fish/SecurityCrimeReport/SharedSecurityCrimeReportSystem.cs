using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Emp;
using Robust.Shared.Timing;

namespace Content.Shared._Fish.SecurityCrimeReport;

/// <summary>
/// Shared Action Hotbar grant/revoke and EMP malfunction for the security gas mask.
/// FIsh edit
/// </summary>
public abstract class SharedSecurityCrimeReportSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SecurityCrimeReportComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SecurityCrimeReportComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SecurityCrimeReportComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<SecurityCrimeReportComponent, OpenSecurityCrimeReportEvent>(OnOpenAction);
        SubscribeLocalEvent<SecurityCrimeReportComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnMapInit(Entity<SecurityCrimeReportComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        Dirty(ent);
    }

    private void OnShutdown(Entity<SecurityCrimeReportComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
    }

    private void OnGetItemActions(Entity<SecurityCrimeReportComponent> ent, ref GetItemActionsEvent args)
    {
        // Only while worn in the clothing slot (mask), same pattern as AddWantedStatus / item actions.
        if (!TryComp<ClothingComponent>(ent.Owner, out var clothing))
            return;

        if (clothing.Slots != args.SlotFlags)
            return;

        args.AddAction(ent.Comp.ActionEntity);
    }

    private void OnOpenAction(Entity<SecurityCrimeReportComponent> ent, ref OpenSecurityCrimeReportEvent args)
    {
        HandleOpenAction(ent, ref args);
    }

    /// <summary>
    /// Marks the InstantAction handled. Client overrides to open SimpleRadialMenu.
    /// </summary>
    protected virtual void HandleOpenAction(Entity<SecurityCrimeReportComponent> ent, ref OpenSecurityCrimeReportEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
    }

    private void OnEmpPulse(Entity<SecurityCrimeReportComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        ent.Comp.MalfunctionUntil = Timing.CurTime + ent.Comp.MalfunctionDuration;
        Dirty(ent);
    }

    protected bool IsMalfunctioning(SecurityCrimeReportComponent comp)
    {
        return comp.MalfunctionUntil is { } until && Timing.CurTime < until;
    }
}
