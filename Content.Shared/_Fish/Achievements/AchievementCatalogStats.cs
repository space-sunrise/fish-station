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
        return CountForSelection(prototypes, states, AllCategoriesId);
    }

    /// <summary>
    /// Прогресс выбранной вкладки: «Все» или конкретная категория.
    /// </summary>
    public static (int Unlocked, int Total) CountForSelection(
        IPrototypeManager prototypes,
        IReadOnlyDictionary<string, AchievementPlayerState> states,
        string? categoryId)
    {
        var visible = EnumerateVisible(prototypes, states, categoryId).ToList();
        var unlocked = visible.Count(a => states.TryGetValue(a.ID, out var st) && st.Unlocked);
        return (unlocked, visible.Count);
    }

    public static CategoryProgress GetSelectionProgress(
        IPrototypeManager prototypes,
        IReadOnlyDictionary<string, AchievementPlayerState> states,
        string? categoryId)
    {
        var (unlocked, total) = CountForSelection(prototypes, states, categoryId);
        var percent = total > 0 ? (int) System.Math.Round(unlocked * 100d / total) : 0;

        if (categoryId == null || categoryId == AllCategoriesId)
            return new CategoryProgress(AllCategoriesId, "fish-achievements-category-all", unlocked, total, percent);

        if (prototypes.TryIndex<AchievementCategoryPrototype>(categoryId, out var category))
            return new CategoryProgress(category.ID, category.Name, unlocked, total, percent);

        return new CategoryProgress(categoryId, categoryId, unlocked, total, percent);
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
            var (unlocked, total) = CountForSelection(prototypes, states, category.ID);
            var percent = total > 0 ? (int) System.Math.Round(unlocked * 100d / total) : 0;
            result.Add(new CategoryProgress(category.ID, category.Name, unlocked, total, percent));
        }

        return result;
    }
}
