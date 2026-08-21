using Content.Shared._Fish.Achievements;
using NUnit.Framework;

namespace Content.Tests.Shared._Fish.Achievements;

/// <summary>
/// Regression: EventKey / shotgun / victim filters.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class AchievementAntiAbuseLogicTests
{
    [Test]
    public void BinaryWithoutAllowGeneric_IsRejected()
    {
        var proto = new AchievementPrototype();
        // ProgressTarget default 1, AllowGenericTrigger default false, empty params
        Assert.That(AchievementAntiAbuseLogic.MatchesContext(proto, default), Is.False);
    }

    [Test]
    public void SeedAllowGeneric_IsAccepted()
    {
        var proto = new AchievementPrototype { AllowGenericTrigger = true };
        Assert.That(AchievementAntiAbuseLogic.MatchesContext(proto, default), Is.True);
    }

    [Test]
    public void KillRequiresPlayerVictim()
    {
        var proto = new AchievementPrototype
        {
            Condition = AchievementConditionKeys.Kill,
            ProgressTarget = 3,
            RequirePlayerVictim = true,
        };

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(proto, new AchievementTriggerContext(VictimIsPlayerHumanoid: false)),
            Is.False);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(proto, new AchievementTriggerContext(VictimIsPlayerHumanoid: true)),
            Is.True);
    }

    [Test]
    public void SuicideIgnoredWhenConfigured()
    {
        var proto = new AchievementPrototype
        {
            Condition = AchievementConditionKeys.Death,
            ProgressTarget = 3,
            IgnoreSuicide = true,
        };

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(proto, new AchievementTriggerContext(IsSuicide: true)),
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
