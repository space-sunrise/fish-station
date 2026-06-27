using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Actions;

namespace Content.Shared._Fish.Weapons.Guns;

[RegisterComponent]
public sealed partial class WeaponTransformComponent : Component
{
    [DataField("resulting", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? TargetPrototype; 

    [DataField("initial", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? StockPrototype;

    [DataField("detachAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DetachAction;

    [DataField]
    public EntityUid? DetachActionEntity;
    
    [DataField("isTransformed")]
    public bool IsStocked = false;

    [DataField]
    public string AttachedPopup = "weapon-transform-attached";

    [DataField]
    public string DetachedPopup = "weapon-transform-detached";
}

public sealed partial class ActionWeaponTransformDetachEvent : InstantActionEvent { }
