using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.KillTome;

/// <summary>
/// Paper with that component is KillTome.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KillTomeComponent : Component
{
    /// <summary>
    /// if delay is not specified, it will use this default value
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DefaultKillDelay = TimeSpan.FromSeconds(40);

    /// <summary>
    /// Damage specifier that will be used to kill the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Cellular", 300 }
        }
    };

    /// <summary>
    /// Maximum number of kills this Kill Tome can perform
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxKillCount = 999;

    /// <summary>
    /// Current number of kills performed by this Kill Tome
    /// </summary>
    [DataField, AutoNetworkedField]
    public int KillCount = 0;

    /// <summary>
    /// to keep a track of already killed people so they won't be killed again
    /// </summary>
    [DataField]
    public HashSet<EntityUid> KilledEntities = [];
}
