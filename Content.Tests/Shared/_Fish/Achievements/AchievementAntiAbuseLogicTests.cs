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
                allowGenericTrigger: true,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                default),
            Is.True);
    }

    [Test]
    public void ProgressWithoutAllowGenericOrParams_IsRejected()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Interaction,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                default),
            Is.False);
    }

    [Test]
    public void KillRequiresPlayerVictim()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Kill,
                allowGenericTrigger: true,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                new AchievementTriggerContext(VictimIsPlayerHumanoid: false)),
            Is.False);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Kill,
                allowGenericTrigger: true,
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
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                new AchievementTriggerContext(IsSuicide: true)),
            Is.False);
    }

    [Test]
    public void WeaponParam_FiltersMismatch()
    {
        var weaponParams = new Dictionary<string, string> { { AchievementConditionParams.Weapon, "WeaponRevolver" } };
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.GunShot,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                weaponParams,
                new AchievementTriggerContext(WeaponPrototypeId: "WeaponRevolver")),
            Is.True);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.GunShot,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                weaponParams,
                new AchievementTriggerContext(WeaponPrototypeId: "WeaponLaserGun")),
            Is.False);
    }

    [Test]
    public void TargetParam_FiltersInteraction()
    {
        var targetParams = new Dictionary<string, string> { { AchievementConditionParams.Target, "MobMule" } };
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Interaction,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                targetParams,
                new AchievementTriggerContext(EntityPrototypeId: "MobMule")),
            Is.True);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Interaction,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                targetParams,
                new AchievementTriggerContext(EntityPrototypeId: "VendingMachine")),
            Is.False);
    }

    [Test]
    public void InherentlySpecific_ChasmFallWithoutParams()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.ChasmFall,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                default),
            Is.True);
    }

    [Test]
    public void TargetParam_FiltersGavelStrike()
    {
        var targetParams = new Dictionary<string, string> { { AchievementConditionParams.Target, "GavelBlock" } };
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.GavelStrike,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                targetParams,
                new AchievementTriggerContext(EntityPrototypeId: "GavelBlock")),
            Is.True);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.GavelStrike,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                targetParams,
                new AchievementTriggerContext(EntityPrototypeId: "ClownRecorder")),
            Is.False);
    }

    [Test]
    public void TagParam_FiltersIntactFloorTilePry()
    {
        var tagParams = new Dictionary<string, string> { { AchievementConditionParams.Tag, "IntactFloor" } };
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.TilePry,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                tagParams,
                new AchievementTriggerContext(VerifiedTag: "IntactFloor")),
            Is.True);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.TilePry,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                tagParams,
                new AchievementTriggerContext(VerifiedTag: null)),
            Is.False);
    }

    [Test]
    public void GunShotGeneric_DoesNotMatchSurvivalWhenBlocked()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.GunShot,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                new AchievementTriggerContext(WeaponPrototypeId: "WeaponRevolver")),
            Is.False);
    }

    [Test]
    public void InherentlySpecific_GibbedWithoutParams()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Gibbed,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                default),
            Is.True);
    }

    [Test]
    public void EventKeyDedupe_SameKeyRejectedTwice()
    {
        var tracker = new AchievementEventKeyTracker();
        Assert.That(tracker.TryConsume(default, "kill:1"), Is.True);
        Assert.That(tracker.TryConsume(default, "kill:1"), Is.False);
        Assert.That(tracker.TryConsume(default, "kill:2"), Is.True);
    }

    [Test]
    public void InherentlySpecific_SingularityConsumedWithoutParams()
    {
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.SingularityConsumed,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                EmptyParams,
                default),
            Is.True);
    }

    [Test]
    public void EmoteParam_FiltersHonk()
    {
        var emoteParams = new Dictionary<string, string> { { AchievementConditionParams.Emote, "Honk" } };
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Emote,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                emoteParams,
                new AchievementTriggerContext(EmotePrototypeId: "Honk")),
            Is.True);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Emote,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                emoteParams,
                new AchievementTriggerContext(EmotePrototypeId: "Clap")),
            Is.False);
    }

    [Test]
    public void ReagentParam_FiltersDesoxyephedrine()
    {
        var reagentParams = new Dictionary<string, string> { { AchievementConditionParams.Reagent, "Desoxyephedrine" } };
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.ReagentMetabolize,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                reagentParams,
                new AchievementTriggerContext(ReagentPrototypeId: "Desoxyephedrine")),
            Is.True);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.ReagentMetabolize,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                reagentParams,
                new AchievementTriggerContext(ReagentPrototypeId: "Water")),
            Is.False);
    }

    [Test]
    public void ExamineTag_FiltersMeteorOnly()
    {
        var tagParams = new Dictionary<string, string> { { AchievementConditionParams.Tag, "Meteor" } };
        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Examine,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                tagParams,
                new AchievementTriggerContext(VerifiedTag: "Meteor")),
            Is.True);

        Assert.That(
            AchievementAntiAbuseLogic.MatchesContext(
                AchievementConditionKeys.Examine,
                allowGenericTrigger: false,
                requirePlayerVictim: true,
                ignoreSuicide: true,
                tagParams,
                new AchievementTriggerContext(VerifiedTag: null)),
            Is.False);
    }
}
