using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Server._Sunrise.StationCentComm;
using Content.Shared._Fish.ObrCall;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Fish.ObrCall;

/// <summary>
/// Общая система запроса ОБР: валидация, покупка, GameRule, FTL, миссия.
/// Консоли ЦК и станции — только источники запроса.
/// </summary>
public sealed partial class ObrCallSystem : EntitySystem
{
    public const string ObrMissionMindRoleId = "MindRoleObrMission";

    private static readonly TimeSpan CallLockDuration = TimeSpan.FromSeconds(3);

    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <summary>
    /// Активные вызовы: teamId → rule entity.
    /// </summary>
    private readonly Dictionary<string, EntityUid> _activeCalls = new();

    /// <summary>
    /// Блокировка от двойных кликов.
    /// </summary>
    private TimeSpan _callLockUntil;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("obr.call");

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<ObrCentCommConsoleComponent, BoundUIOpenedEvent>(OnCentCommUiOpened);
        SubscribeLocalEvent<ObrStationConsoleComponent, BoundUIOpenedEvent>(OnStationUiOpened);

        SubscribeLocalEvent<ObrCentCommConsoleComponent, ObrCallRequestMessage>(OnCentCommRequest);
        SubscribeLocalEvent<ObrStationConsoleComponent, ObrCallRequestMessage>(OnStationRequest);

        SubscribeLocalEvent<GameRuleEndedEvent>(OnGameRuleEnded);

        SubscribeLocalEvent<ObrMissionTargetComponent, MindAddedMessage>(OnMissionTargetMindAdded);
        SubscribeLocalEvent<ObrMissionBriefingComponent, GetBriefingEvent>(OnGetMissionBriefing);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _activeCalls.Clear();
        _callLockUntil = TimeSpan.Zero;
    }

    private void OnGameRuleEnded(ref GameRuleEndedEvent args)
    {
        var ruleEntity = args.RuleEntity;
        var toRemove = _activeCalls
            .Where(kv => kv.Value == ruleEntity)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in toRemove)
            _activeCalls.Remove(key);
    }

    private void OnCentCommUiOpened(Entity<ObrCentCommConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent.Owner, purchaseMode: false, args.Actor);
    }

    private void OnStationUiOpened(Entity<ObrStationConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent.Owner, purchaseMode: true, args.Actor);
    }

    private void OnCentCommRequest(Entity<ObrCentCommConsoleComponent> ent, ref ObrCallRequestMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (!_access.IsAllowed(actor, ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("obr-call-error-access"), ent, actor, PopupType.MediumCaution);
            return;
        }

        TryProcessCall(actor, ent.Owner, args.TeamId, args.Mission, purchaseMode: false);
    }

    private void OnStationRequest(Entity<ObrStationConsoleComponent> ent, ref ObrCallRequestMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (!_access.IsAllowed(actor, ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("obr-call-error-access"), ent, actor, PopupType.MediumCaution);
            return;
        }

        TryProcessCall(actor, ent.Owner, args.TeamId, args.Mission, purchaseMode: true);
    }

    private void TryProcessCall(
        EntityUid actor,
        EntityUid console,
        string teamId,
        string mission,
        bool purchaseMode)
    {
        var maxMissionLength = purchaseMode
            ? CompOrNull<ObrStationConsoleComponent>(console)?.MaxMissionLength ?? 512
            : CompOrNull<ObrCentCommConsoleComponent>(console)?.MaxMissionLength ?? 512;

        if (_timing.CurTime < _callLockUntil)
        {
            Fail(console, actor, Loc.GetString("obr-call-error-busy"), purchaseMode);
            return;
        }

        _callLockUntil = _timing.CurTime + CallLockDuration;

        if (!_prototypes.TryIndex<ObrTeamPrototype>(teamId, out var team))
        {
            Fail(console, actor, Loc.GetString("obr-call-error-unknown-team"), purchaseMode);
            return;
        }

        if (purchaseMode)
        {
            if (!team.StationAvailable || team.StationCost is null)
            {
                Fail(console, actor, Loc.GetString("obr-call-error-team-unavailable"), purchaseMode);
                return;
            }
        }
        else if (!team.CentCommAvailable)
        {
            Fail(console, actor, Loc.GetString("obr-call-error-team-unavailable"), purchaseMode);
            return;
        }

        var sanitizedMission = SanitizeMission(mission, maxMissionLength);

        if (IsTeamAlreadyActive(team.ID))
        {
            Fail(console, actor, Loc.GetString("obr-call-error-already-active"), purchaseMode);
            return;
        }

        if (!TryGetTargetStation(console, purchaseMode, out var station))
        {
            Fail(console, actor, Loc.GetString("obr-call-error-no-station"), purchaseMode);
            return;
        }

        if (!TryComp<StationDataComponent>(station, out var stationData))
        {
            Fail(console, actor, Loc.GetString("obr-call-error-no-station"), purchaseMode);
            return;
        }

        var targetGrid = _station.GetLargestGrid((station, stationData));
        if (targetGrid == null)
        {
            Fail(console, actor, Loc.GetString("obr-call-error-no-grid"), purchaseMode);
            return;
        }

        var cost = 0;
        var charged = false;
        if (purchaseMode)
        {
            cost = team.StationCost!.Value;
            if (!TryComp<StationBankAccountComponent>(station, out var bank))
            {
                Fail(console, actor, Loc.GetString("obr-call-error-no-bank"), purchaseMode);
                return;
            }

            var balance = _cargo.GetBalanceFromAccount((station, bank), team.Account);
            if (balance < cost)
            {
                Fail(console, actor, Loc.GetString("obr-call-error-insufficient-funds"), purchaseMode);
                return;
            }

            // Списание до старта rule; при провале — возврат.
            _cargo.UpdateBankAccount((station, bank), -cost, team.Account);
            charged = true;
        }

        if (!_gameTicker.StartGameRule(team.GameRule, out var ruleUid))
        {
            if (charged)
                Refund(station, team, cost);

            Fail(console, actor, Loc.GetString("obr-call-error-rule-failed"), purchaseMode);
            return;
        }

        if (!TryComp<RuleGridsComponent>(ruleUid, out var ruleGrids) || ruleGrids.MapGrids.Count == 0)
        {
            _gameTicker.EndGameRule(ruleUid);
            if (charged)
                Refund(station, team, cost);

            Fail(console, actor, Loc.GetString("obr-call-error-shuttle-failed"), purchaseMode);
            return;
        }

        var anyShuttle = false;
        foreach (var grid in ruleGrids.MapGrids)
        {
            if (!TryComp<ShuttleComponent>(grid, out var shuttle))
                continue;

            var called = EnsureComp<ObrCalledShuttleComponent>(grid);
            called.TeamId = team.ID;
            called.Mission = sanitizedMission;
            called.RuleUid = ruleUid;

            MarkMissionTargetsOnGrid(grid);

            if (!TryFtlObrToDistantPoint(grid, shuttle, targetGrid.Value))
            {
                _sawmill.Warning($"OBR shuttle {ToPrettyString(grid)} could not FTL to a safe distant point");
                continue;
            }

            anyShuttle = true;
        }

        if (!anyShuttle)
        {
            _gameTicker.EndGameRule(ruleUid);
            if (charged)
                Refund(station, team, cost);

            Fail(console, actor, Loc.GetString("obr-call-error-shuttle-failed"), purchaseMode);
            return;
        }

        _activeCalls[team.ID] = ruleUid;

        var teamName = Loc.GetString(team.Name);
        var purchaseNote = charged ? $" for {cost}" : " (CentComm)";
        var missionNote = string.IsNullOrWhiteSpace(sanitizedMission) ? string.Empty : $"; mission: {sanitizedMission}";
        _adminLog.Add(
            LogType.Action,
            LogImpact.High,
            $"{ToPrettyString(actor)} called OBR team {team.ID} via {ToPrettyString(console)}{purchaseNote}{missionNote}");

        _popup.PopupEntity(
            Loc.GetString("obr-call-success", ("team", teamName)),
            console,
            actor,
            PopupType.Medium);

        DeliverMissionToActiveMembers(ruleGrids.MapGrids, sanitizedMission);
        UpdateUi(console, purchaseMode, actor, Loc.GetString("obr-call-success", ("team", teamName)));
    }

    /// <summary>
    /// Для покупки — станция консоли (или первая игровая).
    /// Для ЦК — первая игровая станция без CentComm.
    /// </summary>
    private bool TryGetTargetStation(EntityUid console, bool purchaseMode, out EntityUid station)
    {
        station = EntityUid.Invalid;

        if (purchaseMode)
        {
            var owning = _station.GetOwningStation(console);
            if (owning != null && !HasComp<StationCentCommComponent>(owning.Value))
            {
                station = owning.Value;
                return true;
            }
        }

        foreach (var candidate in _station.GetStations())
        {
            if (HasComp<StationCentCommComponent>(candidate))
                continue;

            if (!HasComp<StationDataComponent>(candidate))
                continue;

            station = candidate;
            return true;
        }

        return false;
    }

    private void Refund(EntityUid station, ObrTeamPrototype team, int cost)
    {
        if (!TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        _cargo.UpdateBankAccount((station, bank), cost, team.Account);
        _sawmill.Info($"Refunded {cost} to {team.Account} after failed OBR call {team.ID}");
    }

    private void Fail(EntityUid console, EntityUid actor, string message, bool purchaseMode)
    {
        _popup.PopupEntity(message, console, actor, PopupType.MediumCaution);
        UpdateUi(console, purchaseMode, actor, message);
    }

    private void UpdateUi(EntityUid console, bool purchaseMode, EntityUid? actor = null, string? status = null)
    {
        TryGetTargetStation(console, purchaseMode, out var station);

        var balance = 0;
        if (station.Valid && TryComp<StationBankAccountComponent>(station, out var bank))
            balance = _cargo.GetBalanceFromAccount((station, bank), "Cargo");

        var teams = new List<ObrCallTeamEntry>();
        foreach (var team in _prototypes.EnumeratePrototypes<ObrTeamPrototype>().OrderBy(t => t.SortOrder).ThenBy(t => t.ID))
        {
            if (purchaseMode)
            {
                if (!team.StationAvailable || team.StationCost is null)
                    continue;
            }
            else if (!team.CentCommAvailable)
            {
                continue;
            }

            var available = true;
            string? reason = null;

            if (IsTeamAlreadyActive(team.ID))
            {
                available = false;
                reason = Loc.GetString("obr-call-error-already-active");
            }
            else if (purchaseMode && team.StationCost is { } cost && balance < cost)
            {
                available = false;
                reason = Loc.GetString("obr-call-error-insufficient-funds");
            }

            teams.Add(new ObrCallTeamEntry(
                team.ID,
                Loc.GetString(team.Name),
                team.Description != null ? Loc.GetString(team.Description) : null,
                purchaseMode ? team.StationCost : null,
                available,
                reason));
        }

        var state = new ObrCallBoundUserInterfaceState(purchaseMode, balance, status, teams);
        _ui.SetUiState(console, ObrCallUiKey.Key, state);
    }

    private static string SanitizeMission(string mission, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(mission))
            return string.Empty;

        var trimmed = mission.Trim();
        if (trimmed.Length > maxLength)
            trimmed = trimmed[..maxLength];

        return trimmed.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private void MarkMissionTargetsOnGrid(EntityUid grid)
    {
        var query = EntityQueryEnumerator<MindContainerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            EnsureComp<ObrMissionTargetComponent>(uid);
        }
    }

    private void DeliverMissionToActiveMembers(List<EntityUid> grids, string mission)
    {
        if (string.IsNullOrWhiteSpace(mission))
            return;

        var gridSet = grids.ToHashSet();
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var actor, out var xform))
        {
            if (xform.GridUid is not { } grid || !gridSet.Contains(grid))
                continue;

            ApplyMissionToEntity(uid, actor.PlayerSession, mission);
        }
    }

    private void OnMissionTargetMindAdded(EntityUid uid, ObrMissionTargetComponent component, MindAddedMessage args)
    {
        if (Transform(uid).GridUid is not { } grid)
            return;

        if (!TryComp<ObrCalledShuttleComponent>(grid, out var called))
            return;

        if (string.IsNullOrWhiteSpace(called.Mission))
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        ApplyMissionToEntity(uid, actor.PlayerSession, called.Mission);
    }

    private void ApplyMissionToEntity(EntityUid entity, ICommonSession session, string mission)
    {
        var text = Loc.GetString("obr-call-mission-briefing", ("mission", mission));
        _antag.SendBriefing(session, text, Color.Gold, null);

        if (!_mind.TryGetMind(entity, out var mindId, out var mindComp))
            return;

        // Обновляем существующую роль или добавляем новую.
        foreach (var role in mindComp.MindRoleContainer.ContainedEntities)
        {
            if (!TryComp<ObrMissionBriefingComponent>(role, out var existing))
                continue;

            existing.Mission = mission;
            Dirty(role, existing);
            return;
        }

        _roles.MindAddRole(mindId, ObrMissionMindRoleId, mindComp, silent: true);

        foreach (var role in mindComp.MindRoleContainer.ContainedEntities)
        {
            if (!TryComp<ObrMissionBriefingComponent>(role, out var briefing))
                continue;

            briefing.Mission = mission;
            Dirty(role, briefing);
            break;
        }
    }

    private void OnGetMissionBriefing(EntityUid uid, ObrMissionBriefingComponent component, ref GetBriefingEvent args)
    {
        if (string.IsNullOrWhiteSpace(component.Mission))
            return;

        args.Append(Loc.GetString("obr-call-mission-briefing", ("mission", component.Mission)));
    }

    private bool IsTeamAlreadyActive(string teamId)
    {
        if (_activeCalls.TryGetValue(teamId, out var rule) &&
            Exists(rule) &&
            _gameTicker.IsGameRuleActive(rule))
        {
            return true;
        }

        // Дополнительная защита: шаттл ещё в мире после завершения rule.
        var query = EntityQueryEnumerator<ObrCalledShuttleComponent>();
        while (query.MoveNext(out var uid, out var called))
        {
            if (called.TeamId == teamId && !Deleted(uid))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Публичный API для тестов.
    /// </summary>
    public bool IsTeamActive(string teamId) => IsTeamAlreadyActive(teamId);
}
