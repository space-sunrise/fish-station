using Robust.Shared.Serialization;

namespace Content.Shared._Fish.Artillery;

[Serializable, NetSerializable]
public sealed class BluespaceArtilleryConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public ArtilleryVector2 TargetCoordinates;
    public string ExplosionType;
    public float TotalIntensity;
    public float Slope;
    public float MaxIntensity;
    public bool PreviewEnabled;
    public bool IsLinked;
    public bool IsCharging;
    public bool IsOnCooldown;
    public float CooldownRemaining;

    public BluespaceArtilleryConsoleBoundUserInterfaceState(
        ArtilleryVector2 targetCoordinates,
        string explosionType,
        float totalIntensity,
        float slope,
        float maxIntensity,
        bool previewEnabled,
        bool isLinked,
        bool isCharging,
        bool isOnCooldown,
        float cooldownRemaining)
    {
        TargetCoordinates = targetCoordinates;
        ExplosionType = explosionType;
        TotalIntensity = totalIntensity;
        Slope = slope;
        MaxIntensity = maxIntensity;
        PreviewEnabled = previewEnabled;
        IsLinked = isLinked;
        IsCharging = isCharging;
        IsOnCooldown = isOnCooldown;
        CooldownRemaining = cooldownRemaining;
    }
}