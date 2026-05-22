using Robust.Shared.GameStates;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Component for the Loyalty Implant.
/// Periodically sends loyalty-affirming messages to the implanted entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LoyaltyImplantComponent : Component
{
    /// <summary>
    /// The time interval between messages in seconds.
    /// </summary>
    [DataField("interval")]
    public float Interval = 300f; // 5 minutes default

    /// <summary>
    /// The next time a message will be sent.
    /// Not saved - this is set at runtime by LoyaltyImplantSystem.
    /// </summary>
    // removed [DataField] - this is runtime-only and changes every spawn, causing PrototypeSaveTest failure
    public TimeSpan NextMessageTime;
}
