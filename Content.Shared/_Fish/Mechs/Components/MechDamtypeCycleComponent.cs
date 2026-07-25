using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Mechs.Components;

/// <summary>
/// Phazon: цикл типа урона рукопашной (BRUTE → BURN → TOX).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechDamtypeCycleComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ModeIndex;

    [DataField]
    public List<string> DamageTypes = ["Blunt", "Heat", "Poison"];

    [DataField]
    public float DamageAmount = 20f;

    [DataField]
    public EntProtoId CycleAction = "ActionMechCycleDamtype";

    [DataField]
    public EntityUid? CycleActionEntity;
}
