using Content.Server.KillTracking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared._Fish.Achievements;
using Content.Shared._Sunrise.Storyteller;
using Content.Shared.Construction;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Achievements;

/// <summary>
/// Event-driven handlers для всех семейств условий достижений.
/// </summary>
public sealed class AchievementConditionSystem : EntitySystem
{
    [Dependency] private readonly AchievementManager _achievements = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private static readonly ProtoId<TagPrototype> MouseTag = "Mouse";
    private static readonly ProtoId<TagPrototype> HamsterTag = "Hamster";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
        SubscribeLocalEvent<SlipEvent>(OnSlip);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<ActorComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ActorComponent, ItemConstructionCreated>(OnCrafted);
        SubscribeLocalEvent<ActorComponent, DidEquipEvent>(OnEquipped);
        SubscribeLocalEvent<ActorComponent, UserInteractHandEvent>(OnUserInteractHand);
        SubscribeLocalEvent<ActorComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<GameRuleStartedEvent>(OnGameRuleStarted);
        SubscribeLocalEvent<EmergencyShuttleComponent, FTLCompletedEvent>(OnEmergencyShuttleArrived);
        SubscribeLocalEvent<SunriseExplosionEvent>(OnExplosion);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _achievements.OnRoundStarting();
    }

    private async void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        _achievements.MarkRoundPresence(ev.Player);

        if (ev.LateJoin)
            await _achievements.ContributeAsync(ev.Player, AchievementConditionKeys.FirstLateJoin);

        if (!string.IsNullOrEmpty(ev.JobId))
        {
            await _achievements.ContributeAsync(
                ev.Player,
                AchievementConditionKeys.JobPlay,
                new AchievementTriggerContext(JobId: ev.JobId));
        }
    }

    private async void OnRoundEnd(RoundEndMessageEvent ev)
    {
        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity is not { } ent)
                continue;

            if (!TryComp<MobStateComponent>(ent, out var mob) || mob.CurrentState != MobState.Alive)
                continue;

            var onShuttle = IsOnEmergencyShuttle(ent);
            var ctx = new AchievementTriggerContext(OnEmergencyShuttle: onShuttle);

            await _achievements.ContributeAsync(session, AchievementConditionKeys.RoundEndAlive, ctx);
            await _achievements.ContributeAsync(session, AchievementConditionKeys.RoundSurvive, ctx);
            await _achievements.ContributeAsync(
                session,
                AchievementConditionKeys.Counter,
                new AchievementTriggerContext(CounterKey: "rounds-survived", OnEmergencyShuttle: onShuttle));

            if (onShuttle)
                await _achievements.ContributeAsync(session, AchievementConditionKeys.ShuttleArrive, ctx);

            if (_mind.TryGetMind(ent, out var mindId, out _) && _roles.MindIsAntagonist(mindId))
                await _achievements.ContributeAsync(session, AchievementConditionKeys.AntagWin, ctx);
        }
    }

    private bool IsOnEmergencyShuttle(EntityUid ent)
    {
        var grid = Transform(ent).GridUid;
        return grid != null && HasComp<EmergencyShuttleComponent>(grid.Value);
    }

    private void OnSlip(ref SlipEvent ev)
    {
        EnsureComp<AchievementSlippedMarkerComponent>(ev.Slipped);
    }

    private async void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!_mind.TryGetMind(args.Target, out _, out var mindComp) || mindComp.UserId is not { } userId)
            return;

        if (!_players.TryGetSessionById(userId, out var session))
            return;

        var suicide = args.Origin == args.Target;
        var ctx = new AchievementTriggerContext(IsSuicide: suicide);

        await _achievements.ContributeAsync(session, AchievementConditionKeys.Death, ctx);

        if (HasComp<AchievementSlippedMarkerComponent>(args.Target))
            await _achievements.ContributeAsync(session, AchievementConditionKeys.SlipDeath, ctx);

        RemComp<AchievementSlippedMarkerComponent>(args.Target);
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        if (ev.Suicide || ev.Primary is not KillPlayerSource playerKill)
            return;

        if (!_players.TryGetSessionById(playerKill.PlayerId, out var session))
            return;

        if (_tags.HasTag(ev.Entity, MouseTag) || _tags.HasTag(ev.Entity, HamsterTag))
            return;

        var victimIsPlayerHumanoid = HasComp<ActorComponent>(ev.Entity) &&
                                     HasComp<HumanoidProfileComponent>(ev.Entity);

        _ = _achievements.ContributeAsync(
            session,
            AchievementConditionKeys.Kill,
            new AchievementTriggerContext(VictimIsPlayerHumanoid: victimIsPlayerHumanoid));
    }

    private async void OnDamageChanged(EntityUid uid, ActorComponent actor, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        if (!args.DamageIncreased && args.Origin is { } healer && healer != uid)
        {
            if (TryComp<ActorComponent>(healer, out var healerActor))
                await _achievements.ContributeAsync(healerActor.PlayerSession, AchievementConditionKeys.Heal);
        }

        if (args.DamageIncreased && args.Origin is { } attacker && attacker != uid)
        {
            if (!TryComp<ActorComponent>(attacker, out var attackerActor))
                return;

            await _achievements.ContributeAsync(
                attackerActor.PlayerSession,
                AchievementConditionKeys.DamageDealt,
                new AchievementTriggerContext(
                    VictimIsPlayerHumanoid: HasComp<HumanoidProfileComponent>(uid)));
        }
    }

    private void OnCrafted(EntityUid uid, ActorComponent actor, ref ItemConstructionCreated args)
    {
        _ = _achievements.ContributeAsync(actor.PlayerSession, AchievementConditionKeys.Craft);
    }

    private async void OnEquipped(EntityUid uid, ActorComponent actor, DidEquipEvent args)
    {
        await _achievements.ContributeAsync(actor.PlayerSession, AchievementConditionKeys.ItemPickup);
    }

    private async void OnUserInteractHand(EntityUid uid, ActorComponent actor, UserInteractHandEvent args)
    {
        await _achievements.ContributeAsync(actor.PlayerSession, AchievementConditionKeys.Interaction);
    }

    private async void OnInteractionAttempt(EntityUid uid, ActorComponent actor, InteractionAttemptEvent args)
    {
        await _achievements.ContributeAsync(actor.PlayerSession, AchievementConditionKeys.Interaction);
    }

    private void OnGameRuleStarted(ref GameRuleStartedEvent ev)
    {
        if (string.IsNullOrEmpty(ev.RuleId))
            return;

        var ctx = new AchievementTriggerContext(EventId: ev.RuleId);
        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity == null)
                continue;

            _ = _achievements.ContributeAsync(session, AchievementConditionKeys.StationEvent, ctx);
        }
    }

    private void OnEmergencyShuttleArrived(EntityUid uid, EmergencyShuttleComponent component, ref FTLCompletedEvent args)
    {
        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity is not { } ent)
                continue;

            if (Transform(ent).GridUid != uid)
                continue;

            if (!TryComp<MobStateComponent>(ent, out var mob) || mob.CurrentState != MobState.Alive)
                continue;

            _ = _achievements.ContributeAsync(
                session,
                AchievementConditionKeys.ShuttleArrive,
                new AchievementTriggerContext(OnEmergencyShuttle: true));
        }
    }

    private async void OnExplosion(SunriseExplosionEvent ev)
    {
        var radius = Math.Max(ev.AffectedTiles / 4f, 8f);
        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity is not { } ent)
                continue;

            var coords = Transform(ent).MapPosition;
            if (coords.MapId != ev.Epicenter.MapId)
                continue;

            if ((coords.Position - ev.Epicenter.Position).Length() > radius)
                continue;

            await _achievements.ContributeAsync(session, AchievementConditionKeys.Explosion);
        }
    }
}
