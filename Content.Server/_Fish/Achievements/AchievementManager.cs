using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Fish.Achievements;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Achievements;

/// <summary>
/// Account-wide кеш и persistence достижений. Выдача только с сервера.
/// </summary>
public sealed class AchievementManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    private readonly Dictionary<ICommonSession, Dictionary<string, AchievementPlayerState>> _cache = new();

    public event Action<ICommonSession, AchievementPlayerState, bool>? ProgressChanged;

    public void Initialize()
    {
        _sawmill = _log.GetSawmill("fish.achievements");
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

    /// <summary>
    /// Увеличивает прогресс счётчика. Unlock при достижении ProgressTarget.
    /// </summary>
    public async Task<bool> TryAddProgressAsync(
        ICommonSession session,
        string achievementId,
        int delta = 1)
    {
        if (delta <= 0)
            return false;

        if (!_prototypes.TryIndex<AchievementPrototype>(achievementId, out var proto))
        {
            _sawmill.Warning($"Unknown achievement id '{achievementId}'");
            return false;
        }

        if (!_cache.TryGetValue(session, out var cache))
            return false;

        cache.TryGetValue(achievementId, out var existing);
        if (existing.Unlocked)
            return false;

        var newProgress = existing.Progress + delta;
        return await CommitProgressAsync(session, cache, proto, existing, newProgress);
    }

    /// <summary>
    /// Бинарный unlock без клиентского участия.
    /// </summary>
    public async Task<bool> TryUnlockAsync(ICommonSession session, string achievementId)
    {
        if (!_prototypes.TryIndex<AchievementPrototype>(achievementId, out var proto))
        {
            _sawmill.Warning($"Unknown achievement id '{achievementId}'");
            return false;
        }

        if (!_cache.TryGetValue(session, out var cache))
            return false;

        cache.TryGetValue(achievementId, out var existing);
        if (existing.Unlocked)
            return false;

        var target = Math.Max(1, proto.ProgressTarget);
        return await CommitProgressAsync(session, cache, proto, existing, target);
    }

    /// <summary>
    /// Выдать достижения с указанным condition key.
    /// </summary>
    public async Task TryUnlockMatchingAsync(
        ICommonSession session,
        string conditionKey,
        Func<AchievementPrototype, bool>? filter = null)
    {
        foreach (var proto in _prototypes.EnumeratePrototypes<AchievementPrototype>())
        {
            if (proto.Condition != conditionKey)
                continue;

            if (filter != null && !filter(proto))
                continue;

            await TryUnlockAsync(session, proto.ID);
        }
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
