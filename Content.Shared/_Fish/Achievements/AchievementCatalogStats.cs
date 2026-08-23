using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Achievements;

/// <summary>
/// Подсчёт unlocked/total по категориям из прототипов + player state (без дублирования server logic).
/// </summary>
public static class AchievementCatalogStats
{
    public const string AllCategoriesId = "__all__";

    public readonly record struct CategoryProgress(string CategoryId, string DisplayName, int Unlocked, int Total, int Percent);

    public static IEnumerable<AchievementPrototype> EnumerateVisible(
        IPrototypeManager prototypes,
        IReadOnlyDictionary<string, AchievementPlayerState> states,
        string? categoryId)
    {
        var showAll = categoryId == null || categoryId == AllCategoriesId;

        return prototypes
            .EnumeratePrototypes<AchievementPrototype>()
            .Where(a => showAll || categoryId != null && a.Category == categoryId)
            .Where(a => IsVisibleInCatalog(a, states));
    }

    public static bool IsVisibleInCatalog(
        AchievementPrototype proto,
        IReadOnlyDictionary<string, AchievementPlayerState> states)
    {
        if (proto.Condition != AchievementConditionKeys.Manual)
            return true;

        return states.TryGetValue(proto.ID, out var st) && (st.Unlocked || st.Progress > 0);
    }

    public static (int Unlocked, int Total) CountGlobal(
        IPrototypeManager prototypes,
        IReadOnlyDictionary<string, AchievementPlayerState> states)
    {
        var visible = EnumerateVisible(prototypes, states, AllCategoriesId).ToList();
        var unlocked = visible.Count(a => states.TryGetValue(a.ID, out var st) && st.Unlocked);
        return (unlocked, visible.Count);
    }

    public static IReadOnlyList<CategoryProgress> CountByCategory(
        IPrototypeManager prototypes,
        IReadOnlyDictionary<string, AchievementPlayerState> states)
    {
        var categories = prototypes
            .EnumeratePrototypes<AchievementCategoryPrototype>()
            .OrderBy(c => c.Order)
            .ThenBy(c => c.ID)
            .ToList();

        var result = new List<CategoryProgress>(categories.Count);
        foreach (var category in categories)
        {
            var items = EnumerateVisible(prototypes, states, category.ID).ToList();
            var total = items.Count;
            var unlocked = items.Count(a => states.TryGetValue(a.ID, out var st) && st.Unlocked);
            var percent = total > 0 ? (int) System.Math.Round(unlocked * 100d / total) : 0;
            result.Add(new CategoryProgress(category.ID, category.Name, unlocked, total, percent));
        }

        return result;
    }
}
