namespace Content.Shared._Fish.Achievements;

/// <summary>
/// Ключи conditionParams для фильтрации семейств условий.
/// </summary>
public static class AchievementConditionParams
{
    public const string Job = "job";
    public const string Event = "event";
    public const string CounterKey = "key";
    public const string Shuttle = "shuttle";
    /// <summary>EntProtoId цели (kill/heal/interaction target).</summary>
    public const string Target = "target";
    /// <summary>TagPrototype id жертвы/цели.</summary>
    public const string Tag = "tag";
    /// <summary>EntProtoId предмета (ingest/equip/craft).</summary>
    public const string Item = "item";
    /// <summary>AntagPrototype id.</summary>
    public const string Antag = "antag";
    /// <summary>Objective entity prototype id.</summary>
    public const string Objective = "objective";
    /// <summary>Порог playtime в минутах (account-wide).</summary>
    public const string ThresholdMinutes = "threshold";
    /// <summary>DepartmentPrototype id.</summary>
    public const string Department = "department";
    /// <summary>EntProtoId оружия (gun-shot / kill с фильтром).</summary>
    public const string Weapon = "weapon";
    /// <summary>EmotePrototype id.</summary>
    public const string Emote = "emote";
    /// <summary>ReagentPrototype id.</summary>
    public const string Reagent = "reagent";
}
