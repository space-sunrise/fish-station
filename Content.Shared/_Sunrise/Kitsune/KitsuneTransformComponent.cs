using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid.Markings;

namespace Content.Shared._Sunrise.Kitsune;

/// <summary>
/// Component that tracks Kitsune transformation state.
/// Attached to humanoid entities that are Kitsune species.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class KitsuneTransformComponent : Component
{
    /// <summary>
    /// The stashed humanoid entity when transformed into fox form.
    /// </summary>
    [ViewVariables]
    public EntityUid? StashedHumanoid = null;

    /// <summary>
    /// Whether the Kitsune is currently in fox form.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public bool IsTransformed = false;

    /// <summary>
    /// Stashed Special marking (color) from original humanoid form, restored on revert.
    /// </summary>
    [ViewVariables]
    public List<Marking> StashedSpecialMarkings = new();
}
