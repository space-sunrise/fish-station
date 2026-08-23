using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Shared._Fish.Achievements;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Fish;

/// <summary>
/// Runtime audit: каждый прототип имеет известный handler и unlock path.
/// </summary>
[TestFixture]
public sealed class AchievementTriggerAuditTests
{
    private static readonly HashSet<string> HandledConditions = new()
    {
        AchievementConditionKeys.FirstLateJoin,
        AchievementConditionKeys.JobPlay,
        AchievementConditionKeys.RoundEndAlive,
        AchievementConditionKeys.RoundSurvive,
        AchievementConditionKeys.Counter,
        AchievementConditionKeys.AntagWin,
        AchievementConditionKeys.Death,
        AchievementConditionKeys.SlipDeath,
        AchievementConditionKeys.Kill,
        AchievementConditionKeys.DamageDealt,
        AchievementConditionKeys.Heal,
        AchievementConditionKeys.Craft,
        AchievementConditionKeys.ItemPickup,
        AchievementConditionKeys.Interaction,
        AchievementConditionKeys.StationEvent,
        AchievementConditionKeys.ShuttleArrive,
        AchievementConditionKeys.Explosion,
        AchievementConditionKeys.BecameGhost,
        AchievementConditionKeys.ItemIngest,
        AchievementConditionKeys.AntagSelected,
        AchievementConditionKeys.ObjectiveComplete,
        AchievementConditionKeys.PlaytimeMinutes,
        AchievementConditionKeys.RoleAdded,
        AchievementConditionKeys.Defibrillate,
        AchievementConditionKeys.Surgery,
        AchievementConditionKeys.GunShot,
        AchievementConditionKeys.Examine,
        AchievementConditionKeys.SingularityConsumed,
        AchievementConditionKeys.Succumb,
        AchievementConditionKeys.Emote,
        AchievementConditionKeys.AiLawChanges,
        AchievementConditionKeys.ReagentMetabolize,
        AchievementConditionKeys.ChasmFall,
        AchievementConditionKeys.GavelStrike,
        AchievementConditionKeys.TilePry,
        AchievementConditionKeys.Gibbed,
        AchievementConditionKeys.SlipDeath,
    };

    private static readonly HashSet<string> InherentlySpecificConditions = new()
    {
        AchievementConditionKeys.BecameGhost,
        AchievementConditionKeys.SingularityConsumed,
        AchievementConditionKeys.Succumb,
        AchievementConditionKeys.FirstLateJoin,
        AchievementConditionKeys.AntagWin,
        AchievementConditionKeys.RoundEndAlive,
        AchievementConditionKeys.RoundSurvive,
        AchievementConditionKeys.ShuttleArrive,
        AchievementConditionKeys.ChasmFall,
        AchievementConditionKeys.Gibbed,
        AchievementConditionKeys.SlipDeath,
    };

    private static readonly HashSet<string> SeedFullyImplemented = new()
    {
        "FishAchFirstBreath",
        "FishAchStillStanding",
        "FishAchBananaRequiem",
        "FishAchCentcommTourist",
        "FishAchHabitualSurvivor",
    };

    [Test]
    public async Task Audit_AllAchievements_HaveKnownConditionAndUnlockPath()
    {
        await using var pair = await PoolManager.GetServerClient();
        var protoMan = pair.Server.ResolveDependency<IPrototypeManager>();

        await pair.Server.WaitAssertion(() =>
        {
            var all = protoMan.EnumeratePrototypes<AchievementPrototype>().ToList();
            Assert.That(all, Has.Count.GreaterThan(0));

            var manual = all.Where(p => p.Condition == AchievementConditionKeys.Manual).ToList();
            Assert.That(manual, Has.Count.EqualTo(0), "Manual catalog stubs must be removed, not kept");

            foreach (var proto in all)
            {
                Assert.That(HandledConditions.Contains(proto.Condition), Is.True,
                    $"{proto.ID}: no handler for condition {proto.Condition}");

                if (InherentlySpecificConditions.Contains(proto.Condition))
                    continue;

                Assert.That(
                    proto.AllowGenericTrigger || proto.ConditionParams.Count > 0,
                    Is.True,
                    $"{proto.ID}: missing unlock path (allowGenericTrigger or conditionParams)");
            }

            var seed = all.Where(p => SeedFullyImplemented.Contains(p.ID)).ToList();
            Assert.That(seed, Has.Count.EqualTo(SeedFullyImplemented.Count));
        });
    }
}
