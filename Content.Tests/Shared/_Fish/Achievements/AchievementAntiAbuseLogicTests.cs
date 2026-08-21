using System.Collections.Generic;
using Content.Shared._Fish.Achievements;
using NUnit.Framework;

namespace Content.Tests.Shared._Fish.Achievements;

/// <summary>
/// Regression: EventKey / shotgun / victim filters (без new AchievementPrototype — RA0039).
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class AchievementAntiAbuseLogicTests
{
    private static readonly Dictionary<string, string> EmptyParams = new();

    [Test]
    public void BinaryWithoutAllowGeneric_IsRejected()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Manual,
                progressTarget: 1,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                default),
            Is.False);
    }

    [Test]
    public void SeedAllowGeneric_IsAccepted()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.FirstLateJoin,
                progressTarget: 1,
                allowGenericTrigger: true,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                default),
            Is.True);
    }

    [Test]
    public void KillRequiresPlayerVictim()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Kill,
                progressTarget: 3,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                new AchievementTriggerContext(VictimIsPlayerHumanoid: false)),
            Is.False);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Kill,
                progressTarget: 3,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                new AchievementTriggerContext(VictimIsPlayerHumanoid: true)),
            Is.True);
    }

    [Test]
    public void SuicideIgnoredWhenConfigured()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Death,
                progressTarget: 3,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                new AchievementTriggerContext(IsSuicide: true)),
            Is.False);
    }

    [Test]
    public void EventKeyDedupe_SameKeyRejectedTwice()
    {
        var tracker = new AchievementEventKeyTracker();
        Assert.That(tracker.TryConsume(default, "kill:1"), Is.True);
        Assert.That(tracker.TryConsume(default, "kill:1"), Is.False);
        Assert.That(tracker.TryConsume(default, "kill:2"), Is.True);
    }
}
