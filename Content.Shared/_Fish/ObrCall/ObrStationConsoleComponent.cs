using Robust.Shared.GameStates;

namespace Content.Shared._Fish.ObrCall;

/// <summary>
/// Станционная консоль покупки ОБР.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ObrStationConsoleComponent : Component
{
    /// <summary>
    /// Максимальная длина текста миссии.
    /// </summary>
    [DataField]
    public int MaxMissionLength = 512;
}
