using System.Diagnostics.CodeAnalysis;
using Content.Server._Sunrise.Messenger;
using Content.Server.CriminalRecords.Systems;
using Content.Server.EUI;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared._Fish.SecurityHud;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Radio;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Fish.SecurityHud;

/// <summary>
/// Adds an Examine action button (like Health / Strip / ID) that opens criminal-status EUI
/// for users with Security HUD and Security access.
/// </summary>
public sealed class SecurityHudCriminalStatusSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly MessengerServerSystem _messenger = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ShowCriminalRecordIconsGateSystem _hudGate = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public static readonly ProtoId<AccessLevelPrototype> SecurityAccess = "Security";
    public static readonly ProtoId<RadioChannelPrototype> SecurityChannel = "Security";
    public const uint MaxStringLength = 256;

    private readonly Dictionary<ICommonSession, SecurityHudCriminalStatusEui> _openEuis = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<HumanoidAppearanceComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (args.User == args.Target)
            return;

        // Как Strip: нужны руки / доступ / взаимодействие.
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (!_hudGate.HasCriminalRecordHud(args.User))
            return;

        if (!_access.FindAccessTags(args.User).Contains(SecurityAccess))
            return;

        if (!_player.TryGetSessionByEntity(args.User, out _))
            return;

        var target = args.Target;
        var user = args.User;
        var detailsRange = _examine.IsInDetailsRange(user, target);

        var verb = new ExamineVerb
        {
            Act = () =>
            {
                if (!_player.TryGetSessionByEntity(user, out var session))
                    return;

                OpenEui(session, user, target);
            },
            Text = Loc.GetString("security-hud-criminal-status-verb"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange
                ? Loc.GetString("security-hud-criminal-status-verb-message")
                : Loc.GetString("security-hud-criminal-status-verb-disabled"),
            // Существующая иконка розыска (security_icons.rsi / hud_wanted).
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/security_icons.rsi"), "hud_wanted"),
        };

        args.Verbs.Add(verb);
    }

    private void OpenEui(ICommonSession session, EntityUid user, EntityUid target)
    {
        if (_openEuis.TryGetValue(session, out var existing))
        {
            existing.Close();
            _openEuis.Remove(session);
        }

        if (!CanUse(user, target, out var key) ||
            !_records.TryGetRecord<CriminalRecord>(key.Value, out var criminal) ||
            !_records.TryGetRecord<GeneralStationRecord>(key.Value, out var general))
        {
            _popup.PopupEntity(Loc.GetString("criminal-records-console-no-record-found"), user, user);
            return;
        }

        var eui = new SecurityHudCriminalStatusEui(this, target, key.Value, criminal, general);
        _openEuis[session] = eui;
        _eui.OpenEui(eui, session);
    }

    public bool CanUse(EntityUid user, EntityUid target, [NotNullWhen(true)] out StationRecordKey? key)
    {
        key = null;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return false;

        if (!_hudGate.HasCriminalRecordHud(user))
            return false;

        if (!_access.FindAccessTags(user).Contains(SecurityAccess))
            return false;

        return TryFindRecord(user, target, out key);
    }

    public void DenyPermission(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("criminal-records-permission-denied"), user, user);
    }

    public void OnEuiClosed(ICommonSession session)
    {
        _openEuis.Remove(session);
    }

    public SecurityHudCriminalStatusEuiState BuildState(
        EntityUid target,
        StationRecordKey key,
        CriminalRecord criminal,
        GeneralStationRecord general)
    {
        return new SecurityHudCriminalStatusEuiState(
            GetNetEntity(target),
            general.Name ?? Identity.Name(target, EntityManager),
            general.JobTitle,
            criminal.Status,
            criminal.Reason,
            MaxStringLength);
    }

    public bool TryGetRecords(
        StationRecordKey key,
        [NotNullWhen(true)] out CriminalRecord? criminal,
        [NotNullWhen(true)] out GeneralStationRecord? general)
    {
        criminal = null;
        general = null;
        return _records.TryGetRecord(key, out criminal) && _records.TryGetRecord(key, out general);
    }

    public bool TryChangeStatus(EntityUid user, EntityUid target, StationRecordKey key, SecurityStatus status, string? reason)
    {
        if (!_hudGate.HasCriminalRecordHud(user) || !_access.FindAccessTags(user).Contains(SecurityAccess))
        {
            DenyPermission(user);
            return false;
        }

        if (!_examine.IsInDetailsRange(user, target))
            return false;

        if (status == SecurityStatus.Wanted != (reason != null) &&
            status == SecurityStatus.Suspected != (reason != null) &&
            status == SecurityStatus.Hostile != (reason != null))
            return false;

        if (!_records.TryGetRecord<CriminalRecord>(key, out var record) || record.Status == status)
            return false;

        string? trimmedReason = null;
        if (reason != null)
        {
            trimmedReason = reason.Trim();
            if (trimmedReason.Length < 1 || trimmedReason.Length > MaxStringLength)
                return false;
        }

        var oldStatus = record.Status;
        GetOfficer(user, out var officer);

        if (status == SecurityStatus.Detained)
        {
            var oldReason = record.Reason ?? Loc.GetString("criminal-records-console-unspecified-reason");
            var history = Loc.GetString("criminal-records-console-auto-history", ("reason", oldReason));
            _criminalRecords.TryAddHistory(key, history, officer);
        }

        var name = _records.RecordName(key);
        var jobName = "Unknown";
        if (_records.TryGetRecord<GeneralStationRecord>(key, out var entry) && entry.JobTitle != null)
            jobName = entry.JobTitle;

        _criminalRecords.TryChangeStatus(key, status, trimmedReason, officer);

        (string, object)[] locArgs;
        if (trimmedReason != null)
            locArgs = [("name", name), ("officer", officer), ("reason", trimmedReason), ("job", jobName)];
        else
            locArgs = [("name", name), ("officer", officer), ("job", jobName)];

        var statusString = (oldStatus, status) switch
        {
            (_, SecurityStatus.Hostile) => "hostile",
            (_, SecurityStatus.Eliminated) => "eliminated",
            (_, SecurityStatus.Detained) => "detained",
            (_, SecurityStatus.Suspected) => "suspected",
            (_, SecurityStatus.Paroled) => "paroled",
            (_, SecurityStatus.Discharged) => "released",
            (_, SecurityStatus.Wanted) => "wanted",
            (SecurityStatus.Hostile, SecurityStatus.None) => "not-hostile",
            (SecurityStatus.Eliminated, SecurityStatus.None) => "not-eliminated",
            (SecurityStatus.Suspected, SecurityStatus.None) => "not-suspected",
            (SecurityStatus.Wanted, SecurityStatus.None) => "not-wanted",
            (SecurityStatus.Detained, SecurityStatus.None) => "released",
            (SecurityStatus.Paroled, SecurityStatus.None) => "not-parole",
            _ => "not-wanted"
        };

        var locMsg = Loc.GetString($"criminal-records-console-{statusString}", locArgs);
        if (_messenger.GetServerEntity(_station.GetOwningStation(user) ?? key.OriginStation) is var (server, _) &&
            _messenger.GetGroupIdByRadioChannel(SecurityChannel) is { } groupId)
        {
            _messenger.SendSystemMessageToGroup(server, groupId, locMsg);
        }

        return true;
    }

    private bool TryFindRecord(EntityUid user, EntityUid target, [NotNullWhen(true)] out StationRecordKey? key)
    {
        key = null;

        if (_station.GetOwningStation(user) is not { } station)
            station = _station.GetOwningStation(target) ?? default;

        if (!station.IsValid())
            return false;

        var targetName = Identity.Name(target, EntityManager);
        foreach (var (id, record) in _records.GetRecordsOfType<GeneralStationRecord>(station))
        {
            if (!string.Equals(record.Name?.Trim(), targetName?.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            key = new StationRecordKey(id, station);
            return true;
        }

        return false;
    }

    private void GetOfficer(EntityUid uid, out string officer)
    {
        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(null, uid);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);
        officer = tryGetIdentityShortInfoEvent.Title ?? Loc.GetString("criminal-records-console-unknown-officer");
    }
}
