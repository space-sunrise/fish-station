using Content.Shared.Eui;
using Content.Shared.Inventory;
using Content.Shared.Overlays;
using Content.Shared.Security;
using Robust.Shared.Serialization;

namespace Content.Shared._Fish.SecurityHud;

/// <summary>
/// Raised on an examiner (and manually on equipped items) to see if they have Security HUD icons.
/// </summary>
[ByRefEvent]
public record struct GetShowCriminalRecordIconsEvent(bool CanShow = false);

/// <summary>
/// Sets <see cref="GetShowCriminalRecordIconsEvent.CanShow"/> when the entity has criminal record HUD icons.
/// </summary>
public sealed class ShowCriminalRecordIconsGateSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShowCriminalRecordIconsComponent, GetShowCriminalRecordIconsEvent>(OnGet);
    }

    private void OnGet(Entity<ShowCriminalRecordIconsComponent> ent, ref GetShowCriminalRecordIconsEvent args)
    {
        args.CanShow = true;
    }

    /// <summary>
    /// True if the entity itself or non-pocket equipment has <see cref="ShowCriminalRecordIconsComponent"/>.
    /// </summary>
    public bool HasCriminalRecordHud(EntityUid examiner)
    {
        var ev = new GetShowCriminalRecordIconsEvent();
        RaiseLocalEvent(examiner, ref ev);
        if (ev.CanShow)
            return true;

        var enumerator = _inventory.GetSlotEnumerator(examiner, ~SlotFlags.POCKET);
        while (enumerator.NextItem(out var item))
        {
            RaiseLocalEvent(item, ref ev);
            if (ev.CanShow)
                return true;
        }

        return false;
    }
}

[Serializable, NetSerializable]
public sealed class SecurityHudCriminalStatusEuiState : EuiStateBase
{
    public NetEntity Target;
    public string TargetName;
    public string? JobTitle;
    public SecurityStatus Status;
    public string? Reason;
    public uint MaxStringLength;

    public SecurityHudCriminalStatusEuiState(
        NetEntity target,
        string targetName,
        string? jobTitle,
        SecurityStatus status,
        string? reason,
        uint maxStringLength)
    {
        Target = target;
        TargetName = targetName;
        JobTitle = jobTitle;
        Status = status;
        Reason = reason;
        MaxStringLength = maxStringLength;
    }
}

[Serializable, NetSerializable]
public sealed class SecurityHudCriminalStatusChangeMessage : EuiMessageBase
{
    public readonly SecurityStatus Status;
    public readonly string? Reason;

    public SecurityHudCriminalStatusChangeMessage(SecurityStatus status, string? reason)
    {
        Status = status;
        Reason = reason;
    }
}
