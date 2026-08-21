using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Fish.Achievements;
using Content.Shared.Ghost;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Fish.Achievements;

/// <summary>
/// Account-wide кеш, persistence и антиабуз достижений. Выдача только с сервера.
/// </summary>
public sealed class AchievementManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private ISawmill _sawmill = default!;

    private readonly Dictionary<ICommonSession, Dictionary<string, AchievementPlayerState>> _cache = new();
    private readonly Dictionary<string, List<AchievementPrototype>> _byCondition = new();
    private readonly Dictionary<NetUserId, Dictionary<string, int>> _roundProgressTicks = new();
    private readonly Dictionary<(NetUserId User, string AchievementId), TimeSpan> _progressCooldownUntil = new();
    private readonly Dictionary<NetUserId, TimeSpan> _roundPresenceStart = new();

    private int _roundSerial;

    public event Action<ICommonSession, AchievementPlayerState, bool>? ProgressChanged;

    public void Initialize()
    {
        _sawmill = _log.GetSawmill("fish.achievements");
        RebuildConditionIndex();
        _prototypes.PrototypesReloaded += _ => RebuildConditionIndex();
    }

    public void OnRoundStarting()
    {
        _roundSerial++;
        _roundProgressTicks.Clear();
        _progressCooldownUntil.Clear();
        _roundPresenceStart.Clear();
    }

    public void MarkRoundPresence(ICommonSession session)
    {
        _roundPresenceStart[session.UserId] = _timing.CurTime;
    }

    private void RebuildConditionIndex()
    {
        _byCondition.Clear();
        foreach (var proto in _prototypes.EnumeratePrototypes<AchievementPrototype>())
        {
            if (!_byCondition.TryGetValue(proto.Condition, out var list))
            {
                list = [];
                _byCondition[proto.Condition] = list;
            }

            list.Add(proto);
        }
    }

    public bool TryGetState(ICommonSession session, out IReadOnlyDictionary<string, AchievementPlayerState> states)
    {
        if (_cache.TryGetValue(session, out var dict))
        {
            states = dict;
            return true;
        }

        states = new Dictionary<string, AchievementPlayerState>();
        return false;
    }

    public List<AchievementPlayerState> GetSnapshot(ICommonSession session)
    {
        if (!_cache.TryGetValue(session, out var dict))
            return [];

        return dict.Values.ToList();
    }

    public IReadOnlyList<AchievementPrototype> GetByCondition(string conditionKey)
    {
        return _byCondition.TryGetValue(conditionKey, out var list) ? list : Array.Empty<AchievementPrototype>();
    }

    /// <summary>
    /// Вклад в семейство условий с антиабузом и фильтрами.
    /// </summary>
    public async Task ContributeAsync(
        ICommonSession session,
        string conditionKey,
        AchievementTriggerContext context = default,
        Func<AchievementPrototype, bool>? filter = null,
        int delta = 1)
    {
        if (delta <= 0 || !_cache.ContainsKey(session))
            return;

        if (!PassesSessionGate(session))
            return;

        foreach (var proto in GetByCondition(conditionKey))
        {
            if (filter != null && !filter(proto))
                continue;

            if (!MatchesContext(proto, context))
                continue;

            if (proto.ProgressTarget > 1)
                await TryAddProgressInternalAsync(session, proto, delta, context);
            else
                await TryUnlockInternalAsync(session, proto, context);
        }
    }

    public Task<bool> TryAddProgressAsync(ICommonSession session, string achievementId, int delta = 1)
    {
        if (!_prototypes.TryIndex<AchievementPrototype>(achievementId, out var proto))
            return Task.FromResult(false);

        return TryAddProgressInternalAsync(session, proto, delta, default);
    }

    public Task<bool> TryUnlockAsync(ICommonSession session, string achievementId)
    {
        if (!_prototypes.TryIndex<AchievementPrototype>(achievementId, out var proto))
            return Task.FromResult(false);

        return TryUnlockInternalAsync(session, proto, default);
    }

    /// <summary>
    /// Принудительная выдача (admin), без антиабуза и без требования живого тела.
    /// </summary>
    public async Task<bool> TryForceUnlockAsync(ICommonSession session, string achievementId)
    {
        if (!_prototypes.TryIndex<AchievementPrototype>(achievementId, out var proto))
            return false;

        if (!_cache.TryGetValue(session, out var cache))
            return false;

        cache.TryGetValue(proto.ID, out var existing);
        if (existing.Unlocked)
            return false;

        var target = Math.Max(1, proto.ProgressTarget);
        return await CommitProgressAsync(session, cache, proto, existing, target);
    }

    public Task TryUnlockMatchingAsync(
        ICommonSession session,
        string conditionKey,
        Func<AchievementPrototype, bool>? filter = null)
    {
        return ContributeAsync(session, conditionKey, filter: filter);
    }

    private async Task<bool> TryAddProgressInternalAsync(
        ICommonSession session,
        AchievementPrototype proto,
        int delta,
        AchievementTriggerContext context)
    {
        if (delta <= 0 || !PassesSessionGate(session))
            return false;

        if (!_cache.TryGetValue(session, out var cache))
            return false;

        cache.TryGetValue(proto.ID, out var existing);
        if (existing.Unlocked)
            return false;

        if (!PassesAntiAbuse(session, proto))
            return false;

        var newProgress = existing.Progress + delta;
        var ok = await CommitProgressAsync(session, cache, proto, existing, newProgress);
        if (ok)
            MarkRoundTick(session, proto);

        return ok;
    }

    private async Task<bool> TryUnlockInternalAsync(
        ICommonSession session,
        AchievementPrototype proto,
        AchievementTriggerContext context)
    {
        if (!PassesSessionGate(session))
            return false;

        if (!_cache.TryGetValue(session, out var cache))
            return false;

        cache.TryGetValue(proto.ID, out var existing);
        if (existing.Unlocked)
            return false;

        if (!PassesAntiAbuse(session, proto))
            return false;

        var target = Math.Max(1, proto.ProgressTarget);
        var ok = await CommitProgressAsync(session, cache, proto, existing, target);
        if (ok)
            MarkRoundTick(session, proto);

        return ok;
    }

    private bool PassesSessionGate(ICommonSession session)
    {
        if (session.AttachedEntity is not { } ent)
            return false;

        if (_entities.HasComponent<GhostComponent>(ent))
            return false;

        return true;
    }

    private bool PassesAntiAbuse(ICommonSession session, AchievementPrototype proto)
    {
        if (proto.MinRoundSeconds > 0)
        {
            if (!_roundPresenceStart.TryGetValue(session.UserId, out var start))
                return false;

            if ((_timing.CurTime - start).TotalSeconds < proto.MinRoundSeconds)
                return false;
        }

        if (proto.OncePerRound &&
            _roundProgressTicks.TryGetValue(session.UserId, out var map) &&
            map.TryGetValue(proto.ID, out var serial) &&
            serial == _roundSerial)
        {
            return false;
        }

        var cooldownKey = (session.UserId, proto.ID);
        if (_progressCooldownUntil.TryGetValue(cooldownKey, out var until) && _timing.CurTime < until)
            return false;

        var cooldown = TimeSpan.FromSeconds(Math.Max(0, proto.ProgressCooldownSeconds));
        if (cooldown > TimeSpan.Zero)
            _progressCooldownUntil[cooldownKey] = _timing.CurTime + cooldown;

        return true;
    }

    private void MarkRoundTick(ICommonSession session, AchievementPrototype proto)
    {
        if (!proto.OncePerRound)
            return;

        if (!_roundProgressTicks.TryGetValue(session.UserId, out var map))
        {
            map = new Dictionary<string, int>();
            _roundProgressTicks[session.UserId] = map;
        }

        map[proto.ID] = _roundSerial;
    }

    private static bool MatchesContext(AchievementPrototype proto, AchievementTriggerContext context)
    {
        if (proto.IgnoreSuicide && context.IsSuicide)
            return false;

        if (proto.RequirePlayerVictim &&
            (proto.Condition == AchievementConditionKeys.Kill ||
             proto.Condition == AchievementConditionKeys.DamageDealt) &&
            !context.VictimIsPlayerHumanoid)
        {
            return false;
        }

        if (proto.ConditionParams.TryGetValue("job", out var job) &&
            !string.Equals(job, context.JobId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (proto.ConditionParams.TryGetValue("event", out var eventId) &&
            !string.Equals(eventId, context.EventId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (proto.ConditionParams.TryGetValue("key", out var key) &&
            !string.Equals(key, context.CounterKey, StringComparison.OrdinalIgnoreCase))
            return false;

        if (proto.ConditionParams.TryGetValue("shuttle", out var shuttle) &&
            shuttle.Equals("emergency", StringComparison.OrdinalIgnoreCase) &&
            !context.OnEmergencyShuttle)
            return false;

        // Антиабуз shotgun: бинарные без params не открываем пачкой.
        if (proto.ProgressTarget <= 1 &&
            proto.ConditionParams.Count == 0 &&
            !proto.AllowGenericTrigger)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> CommitProgressAsync(
        ICommonSession session,
        Dictionary<string, AchievementPlayerState> cache,
        AchievementPrototype proto,
        AchievementPlayerState existing,
        int newProgress)
    {
        var entry = await _db.UpsertFishAchievementProgressAsync(
            session.UserId.UserId,
            proto.ID,
            newProgress,
            proto.ProgressTarget);

        var state = ToState(entry);
        cache[proto.ID] = state;
        var justUnlocked = state.Unlocked && !existing.Unlocked;
        ProgressChanged?.Invoke(session, state, justUnlocked);
        return justUnlocked || state.Progress != existing.Progress;
    }

    private async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        var rows = await _db.GetFishAchievementsAsync(session.UserId.UserId, cancel);
        var dict = new Dictionary<string, AchievementPlayerState>(rows.Count);
        foreach (var row in rows)
        {
            dict[row.AchievementId] = ToState(row);
        }

        _cache[session] = dict;
    }

    private void ClientDisconnected(ICommonSession session)
    {
        _cache.Remove(session);
        _roundPresenceStart.Remove(session.UserId);
    }

    private static AchievementPlayerState ToState(FishAchievementProgress entry)
    {
        TimeSpan? unlockedAt = entry.UnlockedAt is { } at
            ? at.UtcDateTime - DateTime.UnixEpoch
            : null;

        return new AchievementPlayerState(
            entry.AchievementId,
            entry.Progress,
            entry.UnlockedAt != null,
            unlockedAt);
    }

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
