using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Kitsune;

/// <summary>
/// Component that tracks Kitsune transformation state.
/// Attached to humanoid entities that are Kitsune species.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KitsuneTransformComponent : Component
{
    /// <summary>
    /// The stashed humanoid entity when transformed into fox form.
    /// </summary>
    [ViewVariables]
    public EntityUid? StashedHumanoid = null;
}
