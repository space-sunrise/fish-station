using Content.Shared._Fish.Weapons.Guns;
using Content.Shared.Interaction;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Server._Fish.Weapons.Guns;

public sealed class WeaponTransformSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<WeaponTransformComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WeaponTransformComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WeaponTransformComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WeaponTransformComponent, ActionWeaponTransformDetachEvent>(OnDetachAction);
        SubscribeLocalEvent<WeaponTransformComponent, GetItemActionsEvent>(OnGetItemActions);
    }

    private void OnGetItemActions(EntityUid uid, WeaponTransformComponent component, ref GetItemActionsEvent args)
    {
        if (component.IsStocked && component.DetachActionEntity != null)
        {
            args.AddAction(component.DetachActionEntity.Value);
        }
    }

    private void OnMapInit(EntityUid uid, WeaponTransformComponent component, MapInitEvent args)
    {
        if (component.IsStocked && component.DetachAction != null)
        {
            _actions.AddAction(uid, ref component.DetachActionEntity, component.DetachAction, uid);
        }
    }

    private void OnShutdown(EntityUid uid, WeaponTransformComponent component, ComponentShutdown args)
    {
        if (component.DetachActionEntity != null)
        {
            _actions.RemoveAction(uid, component.DetachActionEntity);
        }
    }

    private void OnInteractUsing(EntityUid uid, WeaponTransformComponent component, InteractUsingEvent args)
    {
        if (args.Handled || component.IsStocked || component.TargetPrototype == null || component.StockPrototype == null)
            return;

        if (MetaData(args.Used).EntityPrototype?.ID != component.StockPrototype)
            return;

        // Start transform
        args.Handled = true;

        var user = args.User;
        var ammoEntities = ExtractState(uid);
        
        var newGun = Spawn(component.TargetPrototype, Transform(user).Coordinates);
        
        InsertState(newGun, ammoEntities);

        QueueDel(args.Used);
        QueueDel(uid);

        _hands.TryPickupAnyHand(user, newGun);
        _popups.PopupEntity(Loc.GetString(component.AttachedPopup), user, user);
    }

    private void OnDetachAction(EntityUid uid, WeaponTransformComponent component, ActionWeaponTransformDetachEvent args)
    {
        args.Handled = true;

        if (!component.IsStocked || component.TargetPrototype == null || component.StockPrototype == null)
            return;

        var user = args.Performer;
        var ammoEntities = ExtractState(uid);

        var newGun = Spawn(component.TargetPrototype, Transform(user).Coordinates);
        var stock = Spawn(component.StockPrototype, Transform(user).Coordinates);

        InsertState(newGun, ammoEntities);

        QueueDel(uid);

        _hands.TryPickupAnyHand(user, newGun);
        _hands.TryPickupAnyHand(user, stock);
        
        _popups.PopupEntity(Loc.GetString(component.DetachedPopup), user, user);
    }

    private Dictionary<string, EntityUid> ExtractState(EntityUid gun)
    {
        var entities = new Dictionary<string, EntityUid>();
        if (TryComp<ContainerManagerComponent>(gun, out var containerManager))
        {
            // Specifically transfer typical gun slots
            foreach (var (id, container) in containerManager.Containers)
            {
                if (id == "gun_magazine" || id == "gun_chamber" || id.StartsWith("item_slot"))
                {
                    if (container.ContainedEntities.Count > 0)
                    {
                        var item = container.ContainedEntities[0];
                        _container.Remove(item, container);
                        entities[id] = item;
                    }
                }
            }
        }
        return entities;
    }

    private void InsertState(EntityUid gun, Dictionary<string, EntityUid> entities)
    {
        if (TryComp<ContainerManagerComponent>(gun, out var containerManager))
        {
            foreach (var (id, container) in containerManager.Containers)
            {
                if (entities.ContainsKey(id))
                {
                    // Clean out pre-existing default items in the new gun first
                    if (container.ContainedEntities.Count > 0)
                    {
                        foreach(var oldItem in container.ContainedEntities.ToArray())
                        {
                            _container.Remove(oldItem, container);
                            Del(oldItem);
                        }
                    }
                    
                    // Insert preserved item
                    _container.Insert(entities[id], container);
                    entities.Remove(id);
                }
            }
        }
        
        // Delete anything remaining that couldn't fit
        foreach(var ent in entities.Values) 
        {
            Del(ent);
        }
    }
}
