using System;
using System.Collections.Generic;
using Robust.Shared.Network;

namespace Content.Shared._Fish.Achievements;

/// <summary>
/// Чистая логика MatchesContext / EventKey для Manager и unit-тестов (без инстанцирования прототипов).
/// </summary>
public static class AchievementAntiAbuseLogic
{
    public static bool MatchesContext(AchievementPrototype proto, AchievementTriggerContext context)
    {
        return MatchesContext(
            proto.Condition,
            proto.ProgressTarget,
            proto.AllowGenericTrigger,
            proto.RequirePlayerVictim,
            proto.IgnoreSuicide,
            proto.ConditionParams,
            context);
    }

    public static bool MatchesContext(
        string condition,
        int progressTarget,
        bool allowGenericTrigger,
        bool requirePlayerVictim,
        bool ignoreSuicide,
        IReadOnlyDictionary<string, string> conditionParams,
        AchievementTriggerContext context)
    {
        if (ignoreSuicide && context.IsSuicide)
            return false;

        if (requirePlayerVictim &&
            (condition == AchievementConditionKeys.Kill ||
             condition == AchievementConditionKeys.DamageDealt) &&
            !context.VictimIsPlayerHumanoid)
        {
            return false;
        }

        if (conditionParams.TryGetValue("job", out var job) &&
            !string.Equals(job, context.JobId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue("event", out var eventId) &&
            !string.Equals(eventId, context.EventId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue("key", out var key) &&
            !string.Equals(key, context.CounterKey, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue("shuttle", out var shuttle) &&
            shuttle.Equals("emergency", StringComparison.OrdinalIgnoreCase) &&
            !context.OnEmergencyShuttle)
            return false;

        // Без params и без allowGenericTrigger — не матчим ни бинарные, ни progress.
        // Иначе сотни stub-ачивок с progressTarget>1 ловят любой Contribute своей семьи (2 клика → 2/40 у всех).
        if (conditionParams.Count == 0 && !allowGenericTrigger)
            return false;

        return true;
    }
}

/// <summary>
/// Трекер уникальных EventKey за раунд (на пользователя).
/// </summary>
public sealed class AchievementEventKeyTracker
{
    private readonly Dictionary<NetUserId, HashSet<string>> _consumed = new();

    public void Clear() => _consumed.Clear();

    public bool IsConsumed(NetUserId user, string eventKey)
    {
        return _consumed.TryGetValue(user, out var set) && set.Contains(eventKey);
    }

    public bool TryConsume(NetUserId user, string eventKey)
    {
        if (!_consumed.TryGetValue(user, out var set))
        {
            set = new HashSet<string>();
            _consumed[user] = set;
        }

        return set.Add(eventKey);
    }
}
