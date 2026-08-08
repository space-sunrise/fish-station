using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.ObrCall;

/// <summary>
/// Настройки дистанционного прибытия ОБР. Одна точка конфигурации для дистанции и поиска.
/// </summary>
[Prototype("obrCallSettings")]
public sealed partial class ObrCallSettingsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Целевая дистанция прибытия от центра станции (метры / тайлы).
    /// </summary>
    [DataField]
    public float ArrivalDistance = 1500f;

    /// <summary>
    /// Шаг увеличения дистанции, если на текущем радиусе нет безопасной точки.
    /// </summary>
    [DataField]
    public float DistanceStep = 100f;

    /// <summary>
    /// Максимальная дистанция поиска.
    /// </summary>
    [DataField]
    public float MaxArrivalDistance = 2500f;

    /// <summary>
    /// Сколько случайных направлений проверять на каждом радиусе.
    /// </summary>
    [DataField]
    public int AttemptsPerRadius = 16;

    /// <summary>
    /// Дополнительный запас вокруг AABB шаттла при проверках (метры).
    /// </summary>
    [DataField]
    public float ClearancePadding = 4f;
}
