using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Fish.Mechs.Components;

/// <summary>
/// Внутренние повреждения шасси: флаги, порог и шанс при уроне.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechInternalDamageComponent : Component
{
    [DataField, AutoNetworkedField]
    public MechInternalDamageFlags Damage = MechInternalDamageFlags.None;

    /// <summary>
    /// Доля Integrity/MaxIntegrity, выше которой возможен ролл внутреннего урона.
    /// </summary>
    [DataField]
    public float IntegrityThreshold = 0.5f;

    /// <summary>
    /// Шанс получить внутреннее повреждение при превышении порога (0–1).
    /// </summary>
    [DataField]
    public float DamageChance = 0.35f;

    /// <summary>
    /// Энергия, теряемая в секунду при ShortCircuit.
    /// </summary>
    [DataField]
    public float ShortCircuitDrainPerSecond = 8f;

    /// <summary>
    /// Урон Heat/сек при Fire.
    /// </summary>
    [DataField]
    public float FireDamagePerSecond = 2f;
}
