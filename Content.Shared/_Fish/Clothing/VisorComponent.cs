using Content.Shared.Damage;
using Content.Shared.Foldable;

namespace Content.Shared._Fish.Clothing;

/// <summary>
/// Configures closed-visor protection and screen mask parameters.
/// Visor state is provided by <see cref="FoldableComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class VisorComponent : Component
{
    /// <summary>
    /// Damage modifiers applied while the visor is closed.
    /// </summary>
    [DataField]
    public DamageModifierSet ClosedDamageModifiers = new();

    /// <summary>
    /// Explosion damage coefficient applied while the visor is closed.
    /// </summary>
    [DataField]
    public float ClosedExplosionCoefficient = 1f;

    /// <summary>
    /// Width of the clear view area relative to the screen.
    /// </summary>
    [DataField]
    public float OpeningWidth = 0.84f;

    /// <summary>
    /// Height of the clear view area relative to the screen.
    /// </summary>
    [DataField]
    public float OpeningHeight = 0.62f;

    /// <summary>
    /// Softness of the transition into the visor frame.
    /// </summary>
    [DataField]
    public float EdgeSoftness = 0.055f;

    /// <summary>
    /// Corner radius of the clear view area.
    /// </summary>
    [DataField]
    public float CornerRadius = 0.085f;

    /// <summary>
    /// Maximum opacity of the visor frame.
    /// </summary>
    [DataField]
    public float Darkness = 0.72f;

    /// <summary>
    /// Duration of the overlay fade transition.
    /// </summary>
    [DataField]
    public float FadeDuration = 0.18f;

    /// <summary>
    /// Wearer cached by the server for lifecycle cleanup.
    /// </summary>
    [NonSerialized]
    public EntityUid? CurrentWearer;
}
