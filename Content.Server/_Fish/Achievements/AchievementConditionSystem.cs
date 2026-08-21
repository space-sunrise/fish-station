using Content.Shared._Fish.Achievements;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Slippery;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._Fish.Achievements;

/// <summary>
/// Event-driven handlers для семейств условий seed/базового каталога.
/// </summary>
public sealed class AchievementConditionSystem : EntitySystem
{
    [Dependency] private readonly AchievementManager _achievements = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
        SubscribeLocalEvent<SlipEvent>(OnSlip);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private async void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (ev.LateJoin)
            await _achievements.TryUnlockMatchingAsync(ev.Player, AchievementConditionKeys.FirstLateJoin);
    }

    private async void OnRoundEnd(RoundEndMessageEvent ev)
    {
        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity is not { } ent)
                continue;

            if (!TryComp<MobStateComponent>(ent, out var mob) || mob.CurrentState != MobState.Alive)
                continue;

            await _achievements.TryUnlockMatchingAsync(session, AchievementConditionKeys.RoundEndAlive);
            await _achievements.TryAddProgressAsync(session, "FishAchHabitualSurvivor");
            await _achievements.TryUnlockMatchingAsync(session, AchievementConditionKeys.ShuttleArrive);
        }
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

        await _achievements.TryUnlockMatchingAsync(session, AchievementConditionKeys.Death);

        if (HasComp<AchievementSlippedMarkerComponent>(args.Target))
            await _achievements.TryUnlockMatchingAsync(session, AchievementConditionKeys.SlipDeath);
    }
}

/// <summary>
/// Маркер недавнего slip для связки slip→death.
/// </summary>
[RegisterComponent]
public sealed partial class AchievementSlippedMarkerComponent : Component;
