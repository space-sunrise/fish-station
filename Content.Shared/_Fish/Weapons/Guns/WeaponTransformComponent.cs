using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Actions;

namespace Content.Shared._Fish.Weapons.Guns;

[RegisterComponent]
public sealed partial class WeaponTransformComponent : Component
{
    [DataField("resulting", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? TargetPrototype; // Fish edit - renamed from targetPrototype

    [DataField("initial", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? StockPrototype; // Fish edit - renamed from stockPrototype

    [DataField("detachAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DetachAction;

    [DataField("detachActionEntity")]
    public EntityUid? DetachActionEntity;
    
    [DataField("isTransformed")]
    public bool IsStocked = false; // Fish edit - renamed from isStocked

    [DataField("attachedPopup")]
    public string AttachedPopup = "weapon-transform-attached";

    [DataField("detachedPopup")]
    public string DetachedPopup = "weapon-transform-detached";
}

public sealed partial class ActionWeaponTransformDetachEvent : InstantActionEvent { }
