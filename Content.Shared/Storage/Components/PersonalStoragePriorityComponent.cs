using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Tracks personal storage priorities for a player.
/// Maps storage entity to the prioritized item entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PersonalStoragePriorityComponent  : Component 
{
    /// <summary>
    /// Dictionary of storage entity to prioritized item entity.
    /// </summary>
    [DataField, AutoNetworkedField] 
    public Dictionary<EntityUid, EntityUid> Priorities = new();
}