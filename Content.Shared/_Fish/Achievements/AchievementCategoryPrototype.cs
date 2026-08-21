using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Fish.Achievements;

/// <summary>
/// Категория достижений для UI-фильтров.
/// </summary>
[Prototype("achievementCategory")]
public sealed partial class AchievementCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Порядок вкладки в окне достижений.
    /// </summary>
    [DataField]
    public int Order;

    /// <summary>
    /// LocId названия категории.
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;
}
