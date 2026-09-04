using Robust.Shared.GameStates;

namespace Content.Shared._Fish.Clothing;

/// <summary>
/// Networked visor mask state attached to a wearer while the visor is closed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VisorOverlayComponent : Component
{
    /// <summary>
    /// Width of the clear view area relative to the screen.
    /// </summary>
    [AutoNetworkedField]
    public float OpeningWidth = 0.84f;

    /// <summary>
    /// Height of the clear view area relative to the screen.
    /// </summary>
    [AutoNetworkedField]
    public float OpeningHeight = 0.62f;

    /// <summary>
    /// Softness of the visor frame edge.
    /// </summary>
    [AutoNetworkedField]
    public float EdgeSoftness = 0.055f;

    /// <summary>
    /// Corner radius of the clear view area.
    /// </summary>
    [AutoNetworkedField]
    public float CornerRadius = 0.085f;

    /// <summary>
    /// Maximum opacity of the visor frame.
    /// </summary>
    [AutoNetworkedField]
    public float Darkness = 0.72f;

    /// <summary>
    /// Duration of the overlay fade transition.
    /// </summary>
    [AutoNetworkedField]
    public float FadeDuration = 0.18f;

    /// <summary>
    /// Server-only source helmet used to remove the correct overlay state.
    /// </summary>
    [NonSerialized]
    public EntityUid? Source;
}
