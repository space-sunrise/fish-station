using Robust.Shared.GameStates;

namespace Content.Shared._Fish.ObrCall;

/// <summary>
/// Консоль ЦК: бесплатный вызов доступных отрядов ОБР.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ObrCentCommConsoleComponent : Component
{
    /// <summary>
    /// Максимальная длина текста миссии.
    /// </summary>
    [DataField]
    public int MaxMissionLength = 512;
}
