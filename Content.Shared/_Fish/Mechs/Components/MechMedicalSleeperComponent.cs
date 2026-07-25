using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Fish.Mechs.Components;

/// <summary>
/// Медицинский sleeper-модуль: лечит сущности в контейнере grabber.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MechMedicalSleeperComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier HealPerSecond = new();
}
