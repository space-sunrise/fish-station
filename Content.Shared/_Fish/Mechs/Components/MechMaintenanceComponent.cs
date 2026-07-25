using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Fish.Mechs.Components;

/// <summary>
/// Состояние техобслуживания меха (Locked → Bolts → Hatch → Cell).
/// Любое ненулевое состояние блокирует движение и использование модулей.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechMaintenanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public MechMaintenanceState State = MechMaintenanceState.Locked;

    /// <summary>
    /// Можно ли пилоту/ID включать protocols (combat часто false).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool MaintAccess = true;
}

[Serializable, NetSerializable]
public enum MechMaintenanceState : byte
{
    Locked = 0,
    SecureBolts = 1,
    LooseBolts = 2,
    OpenHatch = 3,
    UnsecureCell = 4,
}
