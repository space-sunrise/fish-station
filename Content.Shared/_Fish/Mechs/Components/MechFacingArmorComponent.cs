using Robust.Shared.GameStates;

namespace Content.Shared._Fish.Mechs.Components;

/// <summary>
/// Направленная броня: фронт крепче, корма слабее; шанс deflect.
/// Коэффициенты — множители входящего урона (фронт &lt; 1, корма &gt; 1).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechFacingArmorComponent : Component
{
    [DataField]
    public float FrontCoefficient = 0.67f;

    [DataField]
    public float SideCoefficient = 1f;

    [DataField]
    public float BackCoefficient = 2f;

    /// <summary>
    /// Базовый шанс полностью отклонить удар (0–1). Умножается на facing-бонус.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DeflectChance = 0.1f;

    [DataField]
    public float FrontDeflectMultiplier = 1.5f;

    [DataField]
    public float SideDeflectMultiplier = 1f;

    [DataField]
    public float BackDeflectMultiplier = 0.5f;

    /// <summary>
    /// Доп. шанс deflect в defence mode (суммируется системой defence).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float DefenceDeflectBonus;
}
