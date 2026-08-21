namespace Content.Shared._Fish.Achievements;

/// <summary>
/// Антиабуз-настройки достижения. Без gameplay-наград, только защита от фарма.
/// </summary>
public sealed partial class AchievementPrototype
{
    /// <summary>
    /// Не чаще одного прогресса/unlock за раунд.
    /// </summary>
    [DataField]
    public bool OncePerRound = true;

    /// <summary>
    /// Минимальное время в раунде до прогресса (сек). 0 = без ограничения.
    /// </summary>
    [DataField]
    public float MinRoundSeconds;

    /// <summary>
    /// Минимальный интервал между тиками прогресса (сек).
    /// </summary>
    [DataField]
    public float ProgressCooldownSeconds = 2f;

    /// <summary>
    /// Для kill: засчитывать только humanoid-жертв с игроком.
    /// </summary>
    [DataField]
    public bool RequirePlayerVictim = true;

    /// <summary>
    /// Игнорировать самоубийства для death/kill условий.
    /// </summary>
    [DataField]
    public bool IgnoreSuicide = true;

    /// <summary>
    /// Разрешить бинарный unlock без conditionParams (иначе только progress или фильтр).
    /// </summary>
    [DataField]
    public bool AllowGenericTrigger;
}

/// <summary>
/// Контекст события для фильтрации conditionParams.
/// </summary>
public readonly record struct AchievementTriggerContext(
    string? JobId = null,
    string? EventId = null,
    string? CounterKey = null,
    bool IsSuicide = false,
    bool VictimIsPlayerHumanoid = false,
    bool OnEmergencyShuttle = false);
