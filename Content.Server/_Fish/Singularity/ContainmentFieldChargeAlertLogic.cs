using Robust.Shared.GameObjects;

namespace Content.Server._Fish.Singularity;

/// <summary>
/// Hysteresis helpers for containment field generator low-charge admin alerts.
/// Alert only after the generator was above the threshold and then dropped to it or below.
/// </summary>
public static class ContainmentFieldChargeAlertLogic
{
    /// <summary>
    /// Must match the clamp max in <see cref="Content.Shared.Singularity.Components.ContainmentFieldGeneratorComponent.PowerBuffer"/>.
    /// </summary>
    public const int MaxPowerBuffer = 25;

    public const float ThresholdPercent = 50f;

    public static float GetChargePercent(int powerBuffer)
    {
        return powerBuffer / (float)MaxPowerBuffer * 100f;
    }

    public static bool IsAtOrBelowThreshold(int powerBuffer)
    {
        return GetChargePercent(powerBuffer) <= ThresholdPercent;
    }

    /// <summary>
    /// Updates hysteresis tracking. Returns true when a new admin alert should be sent.
    /// </summary>
    public static bool TryRaiseAlert(
        EntityUid uid,
        int powerBuffer,
        HashSet<EntityUid> wasAboveThreshold,
        HashSet<EntityUid> alertedLowCharge)
    {
        if (!IsAtOrBelowThreshold(powerBuffer))
        {
            wasAboveThreshold.Add(uid);
            alertedLowCharge.Remove(uid);
            return false;
        }

        // Never reached a healthy charge — skip to avoid roundstart spam at 0%.
        if (!wasAboveThreshold.Contains(uid))
            return false;

        return alertedLowCharge.Add(uid);
    }
}
