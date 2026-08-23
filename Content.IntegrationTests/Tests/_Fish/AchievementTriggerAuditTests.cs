using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Shared._Fish.Achievements;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Fish;

/// <summary>
/// Жёсткий аудит trigger chain: non-manual прототипы должны иметь handler и unlock path.
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
    public async Task Audit_AllAchievements_HaveKnownConditionOrManualStub()
    {
        await using var pair = await PoolManager.GetServerClient();
        var protoMan = pair.Server.ResolveDependency<IPrototypeManager>();

        await pair.Server.WaitAssertion(() =>
        {
            var all = protoMan.EnumeratePrototypes<AchievementPrototype>().ToList();
            Assert.That(all, Has.Count.EqualTo(494), "Audit baseline count");

            Assert.That(manual, Has.Count.EqualTo(0));

            var gameplay = all.Where(p => p.Condition != AchievementConditionKeys.Manual).ToList();
            Assert.That(gameplay, Has.Count.EqualTo(494));

            foreach (var proto in manual)
            {
                Assert.That(proto.AllowGenericTrigger, Is.False,
                    $"{proto.ID}: manual catalog stub must not unlock from generic gameplay");
            }

            foreach (var proto in gameplay)
            {
                Assert.That(HandledConditions.Contains(proto.Condition), Is.True,
                    $"{proto.ID}: no handler for condition {proto.Condition}");
                Assert.That(
                    proto.AllowGenericTrigger || proto.ConditionParams.Count > 0,
                    Is.True,
                    $"{proto.ID}: missing unlock path (allowGenericTrigger or conditionParams)");
            }

            Assert.That(
                all.Any(p => p.Condition == AchievementConditionKeys.RoundSurvive),
                Is.False,
                "round-survive handler exists but no prototype yet");
            Assert.That(
                all.Any(p => p.Condition == AchievementConditionKeys.AntagWin),
                Is.False,
                "antag-win handler exists but no prototype yet");

            var seed = all.Where(p => SeedFullyImplemented.Contains(p.ID)).ToList();
            Assert.That(seed, Has.Count.EqualTo(SeedFullyImplemented.Count));
        });
    }
}
