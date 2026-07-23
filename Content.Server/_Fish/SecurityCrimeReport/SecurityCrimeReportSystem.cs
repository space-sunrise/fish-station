using System.Text;
using Content.Server._Sunrise.Messenger;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._Fish.SecurityCrimeReport;
using Content.Shared._Sunrise.Laws;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Fish.SecurityCrimeReport;

/// <summary>
/// Sends curated corporate-law crime reports from the security gas mask Action radial.
/// FIsh edit
/// </summary>
public sealed class SecurityCrimeReportSystem : SharedSecurityCrimeReportSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MessengerServerSystem _messenger = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private static readonly ProtoId<RadioChannelPrototype> SecurityChannel = "Security";
    private static readonly char[] InterferenceChars = new[] { '@', '#', '%', '&', '*', '$' };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SecurityCrimeReportSelectedEvent>(OnCrimeSelected);
    }

    private void OnCrimeSelected(SecurityCrimeReportSelectedEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } officer)
            return;

        var device = GetEntity(msg.Device);
        if (!TryComp<SecurityCrimeReportComponent>(device, out var comp))
            return;

        // Must still be wearing this mask in the mask slot.
        if (!_inventory.TryGetSlotEntity(officer, "mask", out var wornMask) || wornMask != device)
            return;

        if (!comp.Articles.Contains(msg.Law))
            return;

        if (!_prototype.TryIndex(msg.Law, out var law) ||
            string.IsNullOrEmpty(law.LawIdentifier))
            return;

        var code = law.LawIdentifier;
        var title = Loc.GetString(law.Title);
        string message;

        if (IsMalfunctioning(comp))
        {
            message = Loc.GetString("security-crime-report-message-malfunction",
                ("code", code),
                ("title", GarbleTitle(title)));
        }
        else
        {
            var location = FormattedMessage.RemoveMarkupOrThrow(
                _navMap.GetNearestBeaconString(officer, onlyName: true));

            if (string.IsNullOrWhiteSpace(location))
                location = Loc.GetString("nav-beacon-pos-no-beacons");

            message = Loc.GetString("security-crime-report-message",
                ("code", code),
                ("title", title),
                ("location", location));
        }

        SendSecurityAnnounce(device, officer, message);
    }

    private void SendSecurityAnnounce(EntityUid device, EntityUid officer, string message)
    {
        var station = _station.GetOwningStation(device) ?? _station.GetOwningStation(officer);

        // Optional messenger mirror.
        if (_messenger.GetServerEntity(station) is var (server, _) &&
            _messenger.GetGroupIdByRadioChannel(SecurityChannel) is { } groupId)
        {
            _messenger.SendSystemMessageToGroup(server, groupId, message);
        }

        // Always push to the Security radio channel (headset listeners).
        // Use the officer as radioSource so EmpDisabled on mask/headset does not cancel the send.
        _radio.SendRadioMessage(officer, message, SecurityChannel, officer);
    }

    private string GarbleTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return Loc.GetString("security-crime-report-interference");

        var result = new StringBuilder(title.Length);
        foreach (var c in title)
        {
            if (char.IsLetter(c) && _random.Prob(0.35f))
                result.Append(_random.Pick(InterferenceChars));
            else
                result.Append(c);
        }

        return result.ToString();
    }
}
