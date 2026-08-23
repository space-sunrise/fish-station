using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Content.Shared._Fish.Achievements;
using Moq;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests.Shared._Fish.Achievements;

[TestFixture]
public sealed class AchievementCatalogStatsTests
{
    private static void SetPrototypeId<T>(T proto, string id) where T : IPrototype
    {
        typeof(T).GetProperty(nameof(IPrototype.ID), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(proto, id);
    }

    private static AchievementPrototype Proto(
        string id,
        string category,
        string condition = AchievementConditionKeys.Kill,
        int order = 0)
    {
#pragma warning disable RA0039 // unit-test stubs без PrototypeManager pool
        var proto = new AchievementPrototype
        {
            Category = category,
            Condition = condition,
            Order = order,
        };
#pragma warning restore RA0039
        SetPrototypeId(proto, id);
        return proto;
    }

    private static AchievementCategoryPrototype Category(string id, int order = 0)
    {
#pragma warning disable RA0039
        var cat = new AchievementCategoryPrototype
        {
            Name = $"fish-achievements-category-{id.ToLowerInvariant()}",
            Order = order,
        };
#pragma warning restore RA0039
        SetPrototypeId(cat, id);
        return cat;
    }

    private static IPrototypeManager CreateProtoMan(
        IEnumerable<AchievementPrototype> achievements,
        IEnumerable<AchievementCategoryPrototype>? categories = null)
    {
        var achList = achievements.ToList();
        var catList = categories?.ToList() ?? new List<AchievementCategoryPrototype>();

        var mock = new Mock<IPrototypeManager>();
        mock.Setup(m => m.EnumeratePrototypes<AchievementPrototype>()).Returns(achList);
        mock.Setup(m => m.EnumeratePrototypes<AchievementCategoryPrototype>()).Returns(catList);
        return mock.Object;
    }

    [Test]
    public void AllCategory_IncludesEveryVisibleAchievement()
    {
        var states = new Dictionary<string, AchievementPlayerState>
        {
            ["A"] = new("A", 0, true, null),
        };

        var protoMan = CreateProtoMan(new[]
        {
            Proto("A", "FishAchCombat"),
            Proto("B", "FishAchMisc"),
            Proto("Manual", "FishAchMisc", AchievementConditionKeys.Manual),
        });

        var all = AchievementCatalogStats.EnumerateVisible(protoMan, states, AchievementCatalogStats.AllCategoriesId).ToList();
        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all.Select(p => p.ID), Is.EquivalentTo(new[] { "A", "B" }));
    }

    [Test]
    public void ManualStub_HiddenUntilProgress()
    {
        var states = new Dictionary<string, AchievementPlayerState>();
        var protoMan = CreateProtoMan(new[] { Proto("Manual", "FishAchMisc", AchievementConditionKeys.Manual) });

        Assert.That(
            AchievementCatalogStats.EnumerateVisible(protoMan, states, AchievementCatalogStats.AllCategoriesId).Any(),
            Is.False);

        states["Manual"] = new("Manual", 1, false, null);
        Assert.That(
            AchievementCatalogStats.EnumerateVisible(protoMan, states, AchievementCatalogStats.AllCategoriesId).Count(),
            Is.EqualTo(1));
    }

    [Test]
    public void CountByCategory_ComputesTotalsDynamically()
    {
        var states = new Dictionary<string, AchievementPlayerState>
        {
            ["A"] = new("A", 0, true, null),
            ["B"] = new("B", 0, false, null),
        };

        var protoMan = CreateProtoMan(
            new[]
            {
                Proto("A", "FishAchCombat"),
                Proto("B", "FishAchCombat"),
                Proto("C", "FishAchMisc"),
            },
            new[]
            {
                Category("FishAchCombat", 10),
                Category("FishAchMisc", 100),
            });

        var rows = AchievementCatalogStats.CountByCategory(protoMan, states);
        var combat = rows.First(r => r.CategoryId == "FishAchCombat");
        var misc = rows.First(r => r.CategoryId == "FishAchMisc");

        Assert.That(combat.Unlocked, Is.EqualTo(1));
        Assert.That(combat.Total, Is.EqualTo(2));
        Assert.That(combat.Percent, Is.EqualTo(50));
        Assert.That(misc.Total, Is.EqualTo(1));
        Assert.That(misc.Unlocked, Is.EqualTo(0));
    }
}
