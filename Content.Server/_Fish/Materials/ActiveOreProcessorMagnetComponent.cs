namespace Content.Server._Fish.Materials;

/// <summary>
/// Хранит состояние работающего импульса магнита переработчика руды.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveOreProcessorMagnetComponent : Component
{
    /// <summary>
    /// Время окончания текущего импульса.
    /// </summary>
    public TimeSpan EndTime;

    /// <summary>
    /// Время следующего поиска руды.
    /// </summary>
    public TimeSpan NextScan;

    /// <summary>
    /// Пользователь, включивший текущий импульс.
    /// </summary>
    public EntityUid User;

    /// <summary>
    /// Была ли собрана хотя бы одна руда за текущий импульс.
    /// </summary>
    public bool CollectedAny;
}
