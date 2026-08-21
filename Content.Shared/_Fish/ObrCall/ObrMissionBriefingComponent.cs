using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Fish.ObrCall;

/// <summary>
/// Свободный текст миссии ОБР в меню персонажа (в отличие от LocId у RoleBriefing).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ObrMissionBriefingComponent : BaseMindRoleComponent
{
    [DataField, AutoNetworkedField]
    public string Mission = string.Empty;
}
