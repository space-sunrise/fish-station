using Robust.Shared.Serialization;

namespace Content.Shared._Fish.Mechs;

/// <summary>
/// Флаги внутренних повреждений меха и связанные эффекты.
/// </summary>
[Flags, Serializable, NetSerializable]
public enum MechInternalDamageFlags : byte
{
    None = 0,
    Fire = 1 << 0,
    TempControl = 1 << 1,
    TankBreach = 1 << 2,
    ControlLost = 1 << 3,
    ShortCircuit = 1 << 4,
}
