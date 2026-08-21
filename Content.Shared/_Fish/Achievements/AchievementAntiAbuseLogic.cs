using System;
using System.Collections.Generic;
using Robust.Shared.Network;

namespace Content.Shared._Fish.Achievements;

/// <summary>
/// Чистая логика MatchesContext / EventKey для тестов и Manager.
/// </summary>
public static class AchievementAntiAbuseLogic
{
    public static bool MatchesContext(AchievementPrototype proto, AchievementTriggerContext context)
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

        if (proto.ProgressTarget <= 1 &&
            proto.ConditionParams.Count == 0 &&
            !proto.AllowGenericTrigger)
        {
            return false;
        }

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
