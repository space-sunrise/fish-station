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
            proto.AllowGenericTrigger,
            proto.RequirePlayerVictim,
            proto.IgnoreSuicide,
            proto.ConditionParams,
            context);
    }

    public static bool MatchesContext(
        string condition,
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

        if (conditionParams.TryGetValue(AchievementConditionParams.Job, out var job) &&
            !string.Equals(job, context.JobId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Event, out var eventId) &&
            !string.Equals(eventId, context.EventId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.CounterKey, out var key) &&
            !string.Equals(key, context.CounterKey, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Shuttle, out var shuttle) &&
            shuttle.Equals("emergency", StringComparison.OrdinalIgnoreCase) &&
            !context.OnEmergencyShuttle)
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Target, out var target) &&
            !string.Equals(target, context.EntityPrototypeId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Tag, out var tag) &&
            !string.Equals(tag, context.VerifiedTag, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Item, out var item) &&
            !string.Equals(item, context.EntityPrototypeId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Antag, out var antag) &&
            !string.Equals(antag, context.AntagPrototypeId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Objective, out var objective))
        {
            if (objective == "*" && context.ObjectivePrototypeId != null)
                return true;

            if (!string.Equals(objective, context.ObjectivePrototypeId, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (conditionParams.TryGetValue(AchievementConditionParams.ThresholdMinutes, out var thresholdStr) &&
            int.TryParse(thresholdStr, out var threshold) &&
            context.PlaytimeMinutes < threshold)
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Weapon, out var weapon) &&
            !string.Equals(weapon, context.WeaponPrototypeId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Emote, out var emote) &&
            !string.Equals(emote, context.EmotePrototypeId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (conditionParams.TryGetValue(AchievementConditionParams.Reagent, out var reagent) &&
            !string.Equals(reagent, context.ReagentPrototypeId, StringComparison.OrdinalIgnoreCase))
            return false;

        // Условия, где сам handler уже задаёт единственный gameplay-смысл.
        if (conditionParams.Count == 0 && !allowGenericTrigger &&
            !IsInherentlySpecificCondition(condition))
            return false;

        return true;
    }

    private static bool IsInherentlySpecificCondition(string condition)
    {
        return condition switch
        {
            AchievementConditionKeys.BecameGhost => true,
            AchievementConditionKeys.SingularityConsumed => true,
            AchievementConditionKeys.Succumb => true,
            AchievementConditionKeys.FirstLateJoin => true,
            AchievementConditionKeys.AntagWin => true,
            AchievementConditionKeys.RoundEndAlive => true,
            AchievementConditionKeys.RoundSurvive => true,
            AchievementConditionKeys.ShuttleArrive => true,
            AchievementConditionKeys.ChasmFall => true,
            _ => false,
        };
    }
}

/// <summary>
/// Трекер уникальных EventKey за раунд (на пользователя).
/// </summary>
public sealed class AchievementEventKeyTracker
{
    private readonly Dictionary<NetUserId, HashSet<string>> _consumed = new();

    public void Clear() => _consumed.Clear();

    public void ClearUser(NetUserId user) => _consumed.Remove(user);

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
