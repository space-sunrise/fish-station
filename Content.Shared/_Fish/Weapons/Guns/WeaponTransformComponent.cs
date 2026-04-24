using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Actions;

namespace Content.Shared._Fish.Weapons.Guns;

[RegisterComponent]
public sealed partial class WeaponTransformComponent : Component
{
    [DataField("targetPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? TargetPrototype;

    [DataField("stockPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? StockPrototype;

    [DataField("detachAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DetachAction;

    [DataField("detachActionEntity")]
    public EntityUid? DetachActionEntity;
    
    [DataField("isStocked")]
    public bool IsStocked = false;

    [DataField("attachedPopup")]
    public string AttachedPopup = "weapon-transform-attached";

    [DataField("detachedPopup")]
    public string DetachedPopup = "weapon-transform-detached";
}

public sealed partial class ActionWeaponTransformDetachEvent : InstantActionEvent { }
