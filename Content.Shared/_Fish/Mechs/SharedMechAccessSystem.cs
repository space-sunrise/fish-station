using Content.Shared._Fish.Mechs.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Forensics.Components;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Mechs;

/// <summary>
/// DNA-замок и maintenance gate для мехов.
/// </summary>
public abstract class SharedMechAccessSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    private static readonly ProtoId<ToolQualityPrototype> AnchoringQuality = "Anchoring";
    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";
    private static readonly ProtoId<ToolQualityPrototype> ScrewingQuality = "Screwing";

    public override void Initialize()
    {
        base.Initialize();

        // DNA entry gate — только на сервере (before MechSystem), см. Content.Server._Fish.Mechs.MechAccessSystem
        SubscribeLocalEvent<MechDnaLockComponent, MechSetDnaLockEvent>(OnSetDna);
        SubscribeLocalEvent<MechDnaLockComponent, MechClearDnaLockEvent>(OnClearDna);

        SubscribeLocalEvent<MechMaintenanceComponent, UpdateCanMoveEvent>(OnMaintCanMove);
        SubscribeLocalEvent<MechMaintenanceComponent, InteractUsingEvent>(OnMaintInteract);
        // Equipment install gate — Server before MechEquipmentSystem
    }

    protected void HandleDnaEntry(Entity<MechDnaLockComponent> ent, ref MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!ent.Comp.Enabled || string.IsNullOrEmpty(ent.Comp.LockedDna))
            return;

        if (!TryComp(args.User, out DnaComponent? dna) || dna.DNA != ent.Comp.LockedDna)
        {
            args.Handled = true;
            Popup.PopupClient(Loc.GetString("mech-dna-lock-denied"), ent, args.User);
        }
    }

    private void OnSetDna(Entity<MechDnaLockComponent> ent, ref MechSetDnaLockEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp(args.Performer, out DnaComponent? dna) || string.IsNullOrEmpty(dna.DNA))
        {
            Popup.PopupClient(Loc.GetString("mech-dna-lock-no-dna"), ent, args.Performer);
            return;
        }

        ent.Comp.LockedDna = dna.DNA;
        ent.Comp.Enabled = true;
        Dirty(ent);
        Popup.PopupClient(Loc.GetString("mech-dna-lock-set"), ent, args.Performer);
    }

    private void OnClearDna(Entity<MechDnaLockComponent> ent, ref MechClearDnaLockEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.LockedDna = null;
        ent.Comp.Enabled = false;
        Dirty(ent);
        Popup.PopupClient(Loc.GetString("mech-dna-lock-cleared"), ent, args.Performer);
    }

    private void OnMaintCanMove(Entity<MechMaintenanceComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.State != MechMaintenanceState.Locked)
            args.Cancel();
    }

    private void OnMaintInteract(Entity<MechMaintenanceComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // ID-карта / доступ: упрощённо — Screwing включает SecureBolts из Locked если MaintAccess.
        if (ent.Comp.MaintAccess &&
            ent.Comp.State is MechMaintenanceState.Locked or MechMaintenanceState.SecureBolts &&
            _tools.HasQuality(args.Used, ScrewingQuality))
        {
            ent.Comp.State = ent.Comp.State == MechMaintenanceState.Locked
                ? MechMaintenanceState.SecureBolts
                : MechMaintenanceState.Locked;
            Dirty(ent);
            _blocker.UpdateCanMove(ent);
            args.Handled = true;
            Popup.PopupClient(
                Loc.GetString(ent.Comp.State == MechMaintenanceState.Locked
                    ? "mech-maint-locked"
                    : "mech-maint-secure-bolts"),
                ent,
                args.User);
            return;
        }

        if (ent.Comp.State is MechMaintenanceState.SecureBolts or MechMaintenanceState.LooseBolts &&
            _tools.HasQuality(args.Used, AnchoringQuality))
        {
            ent.Comp.State = ent.Comp.State == MechMaintenanceState.SecureBolts
                ? MechMaintenanceState.LooseBolts
                : MechMaintenanceState.SecureBolts;
            Dirty(ent);
            _blocker.UpdateCanMove(ent);
            args.Handled = true;
            Popup.PopupClient(
                Loc.GetString(ent.Comp.State == MechMaintenanceState.LooseBolts
                    ? "mech-maint-loose-bolts"
                    : "mech-maint-secure-bolts"),
                ent,
                args.User);
            return;
        }

        if (ent.Comp.State is MechMaintenanceState.LooseBolts or MechMaintenanceState.OpenHatch &&
            _tools.HasQuality(args.Used, PryingQuality))
        {
            ent.Comp.State = ent.Comp.State == MechMaintenanceState.LooseBolts
                ? MechMaintenanceState.OpenHatch
                : MechMaintenanceState.LooseBolts;
            Dirty(ent);
            _blocker.UpdateCanMove(ent);
            args.Handled = true;
            Popup.PopupClient(
                Loc.GetString(ent.Comp.State == MechMaintenanceState.OpenHatch
                    ? "mech-maint-open-hatch"
                    : "mech-maint-loose-bolts"),
                ent,
                args.User);
        }
    }
}
