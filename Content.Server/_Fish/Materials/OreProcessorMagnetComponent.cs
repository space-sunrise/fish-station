namespace Content.Server._Fish.Materials;

/// <summary>
/// Позволяет переработчику собирать руду с пола вокруг себя.
/// </summary>
[RegisterComponent]
public sealed partial class OreProcessorMagnetComponent : Component
{
    /// <summary>
    /// Радиус сбора руды в тайлах.
    /// </summary>
    [DataField]
    public float Range = 3f;

    /// <summary>
    /// Продолжительность одного импульса магнита.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Задержка между попытками собрать руду во время импульса.
    /// </summary>
    [DataField]
    public TimeSpan ScanInterval = TimeSpan.FromSeconds(0.1);
}
