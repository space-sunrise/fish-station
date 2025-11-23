using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Marker component to indicate this entity caused a gas tank explosion, used for damage capping.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GasTankExplosionComponent : Component
{
}

