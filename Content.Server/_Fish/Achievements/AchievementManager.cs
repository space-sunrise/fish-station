using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Fish.Achievements;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Fish.Achievements;

/// <summary>
/// Account-wide кеш, persistence и антиабуз. Выдача только с сервера по NetUserId.
/// </summary>
public sealed class AchievementManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    private ISawmill _sawmill = default!;
    private AchievementGameplayGateSystem? _gate;

    private readonly Dictionary<ICommonSession, Dictionary<string, AchievementPlayerState>> _cache = new();
    private readonly Dictionary<string, List<AchievementPrototype>> _byCondition = new();

    /// <summary>Раундовые тики без EventKey: user → achievementId → roundSerial.</summary>
    private readonly Dictionary<NetUserId, Dictionary<string, int>> _roundProgressTicks = new();

    private readonly AchievementEventKeyTracker _eventKeys = new();

    private readonly Dictionary<(NetUserId User, string AchievementId), TimeSpan> _progressCooldownUntil = new();
    private readonly Dictionary<NetUserId, TimeSpan> _roundPresenceStart = new();

    /// <summary>Сериализация вкладов на пользователя (race / duplicate async).</summary>
    private readonly Dictionary<NetUserId, SemaphoreSlim> _userLocks = new();

    private int _roundSerial;
    private bool _conditionIndexReady;
    private AchievementGameplayGateSystem Gate => _gate ??= _systems.GetEntitySystem<AchievementGameplayGateSystem>();

    public event Action<ICommonSession, AchievementPlayerState, bool>? ProgressChanged;

    public void Initialize()
    {
        _sawmill = _log.GetSawmill("fish.achievements");
        // Не индексируем в Init — прототипы ещё не загружены (YAMLLinter / Map Renderer).
        _prototypes.PrototypesReloaded += _ => RebuildConditionIndex();
    }

    public void OnRoundStarting()
    {
        _roundSerial++;
        _roundProgressTicks.Clear();
        _eventKeys.Clear();
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

        _conditionIndexReady = true;
    }

    private void EnsureConditionIndex()
    {
        if (_conditionIndexReady)
            return;

        try
        {
            RebuildConditionIndex();
        }
        catch (InvalidOperationException)
        {
            // Прототипы ещё не загружены — следующий Contribute/reload подхватит.
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
        EnsureConditionIndex();
        return _byCondition.TryGetValue(conditionKey, out var list) ? list : Array.Empty<AchievementPrototype>();
    }

    private SemaphoreSlim GetUserLock(NetUserId userId)
    {
        lock (_userLocks)
        {
            if (!_userLocks.TryGetValue(userId, out var sem))
            {
                sem = new SemaphoreSlim(1, 1);
                _userLocks[userId] = sem;
            }

            return sem;
        }
    }

    /// <summary>
    /// Вклад в семейство условий с антиабузом, EventKey-dedupe и индексацией по condition.
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

        var sem = GetUserLock(session.UserId);
        await sem.WaitAsync();
        try
        {
            // EventKey: одно реальное событие — один вклад в раунде (все ачивки этого события).
            if (!string.IsNullOrEmpty(context.EventKey) && _eventKeys.IsConsumed(session.UserId, context.EventKey))
                return;

            var anyAccepted = false;

            foreach (var proto in GetByCondition(conditionKey))
            {
                if (filter != null && !filter(proto))
                    continue;

                if (!AchievementAntiAbuseLogic.MatchesContext(proto, context))
                    continue;

                if (!Gate.CanEarnGameplay(session, proto, context.RequireInRound))
                    continue;

                bool ok;
                if (proto.ProgressTarget > 1)
                    ok = await TryAddProgressInternalAsync(session, proto, delta, context);
                else
                    ok = await TryUnlockInternalAsync(session, proto, context);

                anyAccepted |= ok;
            }

            if (anyAccepted && !string.IsNullOrEmpty(context.EventKey))
                _eventKeys.TryConsume(session.UserId, context.EventKey);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<bool> TryAddProgressAsync(ICommonSession session, string achievementId, int delta = 1)
    {
        if (!_prototypes.TryIndex<AchievementPrototype>(achievementId, out var proto))
            return false;

        var sem = GetUserLock(session.UserId);
        await sem.WaitAsync();
        try
        {
            return await TryAddProgressInternalAsync(session, proto, delta, default);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<bool> TryUnlockAsync(ICommonSession session, string achievementId)
    {
        if (!_prototypes.TryIndex<AchievementPrototype>(achievementId, out var proto))
            return false;

        var sem = GetUserLock(session.UserId);
        await sem.WaitAsync();
        try
        {
            return await TryUnlockInternalAsync(session, proto, default);
        }
        finally
        {
            sem.Release();
        }
    }

    /// <summary>
    /// Принудительная выдача (admin), без gameplay-gate.
    /// </summary>
    public async Task<bool> TryForceUnlockAsync(ICommonSession session, string achievementId)
    {
        if (!_prototypes.TryIndex<AchievementPrototype>(achievementId, out var proto))
            return false;

        if (!_cache.TryGetValue(session, out var cache))
            return false;

        var sem = GetUserLock(session.UserId);
        await sem.WaitAsync();
        try
        {
            cache.TryGetValue(proto.ID, out var existing);
            if (existing.Unlocked)
                return false;

            var target = Math.Max(1, proto.ProgressTarget);
            return await CommitProgressAsync(session, cache, proto, existing, target);
        }
        finally
        {
            sem.Release();
        }
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
        if (delta <= 0)
            return false;

        if (!_cache.TryGetValue(session, out var cache))
            return false;

        cache.TryGetValue(proto.ID, out var existing);
        if (existing.Unlocked)
            return false;

        if (!PassesAntiAbuse(session, proto, context))
            return false;

        var newProgress = existing.Progress + delta;
        var ok = await CommitProgressAsync(session, cache, proto, existing, newProgress);
        if (ok)
            OnSuccessfulProgress(session, proto, context);

        return ok;
    }

    private async Task<bool> TryUnlockInternalAsync(
        ICommonSession session,
        AchievementPrototype proto,
        AchievementTriggerContext context)
    {
        if (!_cache.TryGetValue(session, out var cache))
            return false;

        cache.TryGetValue(proto.ID, out var existing);
        if (existing.Unlocked)
            return false;

        if (!PassesAntiAbuse(session, proto, context))
            return false;

        var target = Math.Max(1, proto.ProgressTarget);
        var ok = await CommitProgressAsync(session, cache, proto, existing, target);
        if (ok)
            OnSuccessfulProgress(session, proto, context);

        return ok;
    }

    private bool PassesAntiAbuse(
        ICommonSession session,
        AchievementPrototype proto,
        AchievementTriggerContext context)
    {
        if (proto.MinRoundSeconds > 0)
        {
            if (!_roundPresenceStart.TryGetValue(session.UserId, out var start))
                return false;

            if ((_timing.CurTime - start).TotalSeconds < proto.MinRoundSeconds)
                return false;
        }

        // Без EventKey — once-per-round на ачивку (survive и т.п.).
        // С EventKey — разные события могут давать прогресс в одном раунде.
        if (string.IsNullOrEmpty(context.EventKey) &&
            proto.OncePerRound &&
            _roundProgressTicks.TryGetValue(session.UserId, out var map) &&
            map.TryGetValue(proto.ID, out var serial) &&
            serial == _roundSerial)
        {
            return false;
        }

        if (string.IsNullOrEmpty(context.EventKey))
        {
            var cooldownKey = (session.UserId, proto.ID);
            if (_progressCooldownUntil.TryGetValue(cooldownKey, out var until) && _timing.CurTime < until)
                return false;
        }

        return true;
    }

    private void OnSuccessfulProgress(
        ICommonSession session,
        AchievementPrototype proto,
        AchievementTriggerContext context)
    {
        if (string.IsNullOrEmpty(context.EventKey) && proto.OncePerRound)
        {
            if (!_roundProgressTicks.TryGetValue(session.UserId, out var map))
            {
                map = new Dictionary<string, int>();
                _roundProgressTicks[session.UserId] = map;
            }

            map[proto.ID] = _roundSerial;
        }

        if (string.IsNullOrEmpty(context.EventKey))
        {
            var cooldown = TimeSpan.FromSeconds(Math.Max(0, proto.ProgressCooldownSeconds));
            if (cooldown > TimeSpan.Zero)
                _progressCooldownUntil[(session.UserId, proto.ID)] = _timing.CurTime + cooldown;
        }
    }

    private async Task<bool> CommitProgressAsync(
        ICommonSession session,
        Dictionary<string, AchievementPlayerState> cache,
        AchievementPrototype proto,
        AchievementPlayerState existing,
        int newProgress)
    {
        // Уже unlocked — никаких повторных write/notify.
        if (existing.Unlocked)
            return false;

        var entry = await _db.UpsertFishAchievementProgressAsync(
            session.UserId.UserId,
            proto.ID,
            newProgress,
            proto.ProgressTarget);

        var state = ToState(entry);
        cache[proto.ID] = state;
        var justUnlocked = state.Unlocked && !existing.Unlocked;
        var progressChanged = state.Progress != existing.Progress;

        if (!justUnlocked && !progressChanged)
            return false;

        ProgressChanged?.Invoke(session, state, justUnlocked);
        return true;
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
        // Round ticks / EventKeys / cooldown по UserId сохраняем — reconnect в том же раунде не сбрасывает антиабуз.
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
