using System.Collections.Generic;
using Content.Server._Fish.Singularity;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.Tests.Shared._Fish.Singularity;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(ContainmentFieldChargeAlertLogic))]
public sealed class ContainmentFieldChargeAlertLogicTests
{
    [Test]
    public void GetChargePercent_UsesMaxPowerBuffer()
    {
        Assert.That(ContainmentFieldChargeAlertLogic.GetChargePercent(0), Is.EqualTo(0f));
        Assert.That(ContainmentFieldChargeAlertLogic.GetChargePercent(25), Is.EqualTo(100f));
        Assert.That(ContainmentFieldChargeAlertLogic.GetChargePercent(12), Is.EqualTo(48f).Within(0.001f));
        Assert.That(ContainmentFieldChargeAlertLogic.GetChargePercent(13), Is.EqualTo(52f).Within(0.001f));
    }

    [Test]
    public void IsAtOrBelowThreshold_AtFiftyPercentOrLower()
    {
        Assert.That(ContainmentFieldChargeAlertLogic.IsAtOrBelowThreshold(12), Is.True);  // 48%
        Assert.That(ContainmentFieldChargeAlertLogic.IsAtOrBelowThreshold(13), Is.False); // 52%
        Assert.That(ContainmentFieldChargeAlertLogic.IsAtOrBelowThreshold(0), Is.True);
        Assert.That(ContainmentFieldChargeAlertLogic.IsAtOrBelowThreshold(25), Is.False);
    }

    [Test]
    public void TryRaiseAlert_DoesNotSpamAtRoundstartZeroCharge()
    {
        var uid = new EntityUid(1);
        var wasAbove = new HashSet<EntityUid>();
        var alerted = new HashSet<EntityUid>();

        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 0, wasAbove, alerted), Is.False);
        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 0, wasAbove, alerted), Is.False);
        Assert.That(alerted, Is.Empty);
    }

    [Test]
    public void TryRaiseAlert_AlertsOnceOnCrossingBelowThreshold()
    {
        var uid = new EntityUid(1);
        var wasAbove = new HashSet<EntityUid>();
        var alerted = new HashSet<EntityUid>();

        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 25, wasAbove, alerted), Is.False);
        Assert.That(wasAbove.Contains(uid), Is.True);

        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 12, wasAbove, alerted), Is.True);
        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 10, wasAbove, alerted), Is.False);
        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 0, wasAbove, alerted), Is.False);
    }

    [Test]
    public void TryRaiseAlert_RealertsOnlyAfterRecoveryAboveThreshold()
    {
        var uid = new EntityUid(1);
        var wasAbove = new HashSet<EntityUid>();
        var alerted = new HashSet<EntityUid>();

        ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 20, wasAbove, alerted);
        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 10, wasAbove, alerted), Is.True);
        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 8, wasAbove, alerted), Is.False);

        // Recover above threshold.
        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 13, wasAbove, alerted), Is.False);
        Assert.That(alerted.Contains(uid), Is.False);

        // Discharge again.
        Assert.That(ContainmentFieldChargeAlertLogic.TryRaiseAlert(uid, 12, wasAbove, alerted), Is.True);
    }
}
