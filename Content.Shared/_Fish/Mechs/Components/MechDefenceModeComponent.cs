using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Mechs.Components;

/// <summary>
/// Режим обороны (Durand): якорь + повышенный deflect.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechDefenceModeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField]
    public float DeflectChanceBonus = 0.25f;

    [DataField]
    public EntProtoId ToggleAction = "ActionMechToggleDefence";

    [DataField]
    public EntityUid? ToggleActionEntity;
}
