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

    /// <summary>
    /// Задержка между покупкой и развертыванием отряда ОБР.
    /// </summary>
    [DataField]
    public TimeSpan CallDelay = TimeSpan.FromMinutes(15);
}
