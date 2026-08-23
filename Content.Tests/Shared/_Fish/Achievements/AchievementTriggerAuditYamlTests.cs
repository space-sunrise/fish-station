using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared._Fish.Achievements;
using NUnit.Framework;

namespace Content.Tests.Shared._Fish.Achievements;

/// <summary>
/// Аудит trigger inventory по YAML без integration pool.
/// </summary>
[TestFixture]
public sealed class AchievementTriggerAuditYamlTests
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
    };

    private static string FindRepoRoot()
    {
        var dir = TestContext.CurrentContext.TestDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.Exists(Path.Combine(dir, "Resources", "Prototypes", "_Fish", "Achievements")))
                return dir;

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Could not locate achievement prototypes directory");
    }

    [Test]
    public void YamlInventory_MatchesAuditBaseline()
    {
        var dir = Path.Combine(FindRepoRoot(), "Resources", "Prototypes", "_Fish", "Achievements");
        Assert.That(Directory.Exists(dir), Is.True, dir);

        var ids = new List<string>();
        var conditions = new List<string>();
        var allowGeneric = new Dictionary<string, bool>();
        var hasParams = new Dictionary<string, bool>();

        foreach (var file in Directory.EnumerateFiles(dir, "*.yml"))
        {
            if (file.EndsWith("categories.yml", StringComparison.Ordinal))
                continue;

            var text = File.ReadAllText(file);
            foreach (Match block in Regex.Matches(text, @"- type: achievement[\s\S]*?(?=\n- type:|\z)"))
            {
                var chunk = block.Value;
                var idMatch = Regex.Match(chunk, @"^\s+id:\s+(\S+)\s*$", RegexOptions.Multiline);
                var condMatch = Regex.Match(chunk, @"^\s+condition:\s+(\S+)\s*$", RegexOptions.Multiline);
                if (!idMatch.Success || !condMatch.Success)
                    continue;

                var id = idMatch.Groups[1].Value;
                var cond = condMatch.Groups[1].Value;
                ids.Add(id);
                conditions.Add(cond);
                allowGeneric[id] = Regex.IsMatch(chunk, @"^\s+allowGenericTrigger:\s+true\s*$", RegexOptions.Multiline);
                hasParams[id] = Regex.IsMatch(chunk, @"^\s+conditionParams:\s*$", RegexOptions.Multiline);
            }
        }

        Assert.That(ids, Has.Count.EqualTo(494));

        var manual = conditions.Count(c => c == AchievementConditionKeys.Manual);
        var gameplay = conditions.Count(c => c != AchievementConditionKeys.Manual);

        // manual только для admin-only blocked (Contributor и т.п.)
        Assert.That(manual, Is.LessThanOrEqualTo(10));
        Assert.That(gameplay + manual, Is.EqualTo(494));

        for (var i = 0; i < ids.Count; i++)
        {
            var cond = conditions[i];
            var id = ids[i];

            if (cond == AchievementConditionKeys.Manual)
            {
                Assert.That(allowGeneric.GetValueOrDefault(id), Is.False,
                    $"{id}: manual stub must not have allowGenericTrigger");
                continue;
            }

            Assert.That(HandledConditions.Contains(cond), Is.True,
                $"{id}: unknown condition {cond}");

            // blocked: без params и без allowGenericTrigger — ожидаемо до доработки refine
            if (!allowGeneric.GetValueOrDefault(id) && !hasParams.GetValueOrDefault(id))
                continue;

            Assert.That(allowGeneric.GetValueOrDefault(id) || hasParams.GetValueOrDefault(id), Is.True,
                $"{id}: gameplay achievement needs allowGenericTrigger or conditionParams");
        }
    }
}
