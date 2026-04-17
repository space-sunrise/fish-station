using Content.Shared._Fish.Weapons.Guns;
using Content.Shared.Interaction;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Server._Fish.Weapons.Guns;

public sealed class N1984TransformSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<N1984TransformComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<N1984TransformComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<N1984TransformComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<N1984TransformComponent, ActionN1984DetachEvent>(OnDetachAction);
        SubscribeLocalEvent<N1984TransformComponent, GetItemActionsEvent>(OnGetItemActions);
    }

    private void OnGetItemActions(EntityUid uid, N1984TransformComponent component, ref GetItemActionsEvent args)
    {
        if (component.IsStocked && component.DetachActionEntity != null)
        {
            args.AddAction(component.DetachActionEntity.Value);
        }
    }

    private void OnMapInit(EntityUid uid, N1984TransformComponent component, MapInitEvent args)
    {
        if (component.IsStocked && component.DetachAction != null)
        {
            _actions.AddAction(uid, ref component.DetachActionEntity, component.DetachAction, uid);
        }
    }

    private void OnShutdown(EntityUid uid, N1984TransformComponent component, ComponentShutdown args)
    {
        if (component.DetachActionEntity != null)
        {
            _actions.RemoveAction(uid, component.DetachActionEntity);
        }
    }

    private void OnInteractUsing(EntityUid uid, N1984TransformComponent component, InteractUsingEvent args)
    {
        if (args.Handled || component.IsStocked || component.TargetPrototype == null || component.StockPrototype == null)
            return;

        if (MetaData(args.Used).EntityPrototype?.ID != component.StockPrototype)
            return;

        // Start transform
        args.Handled = true;

        var user = args.User;
        var ammoEntities = ExtractAmmo(uid);
        
        var newGun = Spawn(component.TargetPrototype, Transform(user).Coordinates);
        
        InsertAmmo(newGun, ammoEntities);

        QueueDel(args.Used);
        QueueDel(uid);

        _hands.TryPickupAnyHand(user, newGun);
        _popups.PopupEntity(Loc.GetString("n1984-transform-attached"), user, user);
    }

    private void OnDetachAction(EntityUid uid, N1984TransformComponent component, ActionN1984DetachEvent args)
    {
        args.Handled = true;

        if (!component.IsStocked || component.TargetPrototype == null || component.StockPrototype == null)
            return;

        var user = args.Performer;
        var ammoEntities = ExtractAmmo(uid);

        var newGun = Spawn(component.TargetPrototype, Transform(user).Coordinates);
        var stock = Spawn(component.StockPrototype, Transform(user).Coordinates);

        InsertAmmo(newGun, ammoEntities);

        QueueDel(uid);

        _hands.TryPickupAnyHand(user, newGun);
        _hands.TryPickupAnyHand(user, stock);
        
        _popups.PopupEntity(Loc.GetString("n1984-transform-detached"), user, user);
    }

    private Dictionary<string, EntityUid> ExtractAmmo(EntityUid gun)
    {
        var entities = new Dictionary<string, EntityUid>();
        if (TryComp<ItemSlotsComponent>(gun, out var slots))
        {
            foreach (var (id, slot) in slots.Slots)
            {
                if (slot.Item != null && slot.ContainerSlot is BaseContainer baseContainer)
                {
                    var item = slot.Item.Value;
                    _container.Remove(item, baseContainer);
                    entities[id] = item;
                }
            }
        }
        return entities;
    }

    private void InsertAmmo(EntityUid gun, Dictionary<string, EntityUid> entities)
    {
        if (TryComp<ItemSlotsComponent>(gun, out var slots))
        {
            foreach (var slot in slots.Slots.Values)
            {
                if (slot.Item != null && slot.ContainerSlot is BaseContainer baseContainer)
                {
                    var oldItem = slot.Item.Value;
                    _container.Remove(oldItem, baseContainer);
                    Del(oldItem);
                }
            }

            foreach (var (id, ent) in entities)
            {
                if (slots.Slots.TryGetValue(id, out var destSlot) && destSlot.ContainerSlot is BaseContainer destBase)
                {
                    _container.Insert(ent, destBase);
                }
                else
                {
                    Del(ent);
                }
            }
        }
        else
        {
            foreach(var ent in entities.Values) Del(ent);
        }
    }
}
