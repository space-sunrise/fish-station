using Content.Client._Fish.HealthAnalyzer;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.EntityConditions;
using Content.Shared.EntityConditions.Conditions;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Damage;
using Content.Shared.FixedPoint;
using NUnit.Framework;

namespace Content.Tests._Fish.HealthAnalyzer;

[TestFixture]
public sealed class HealthAnalyzerMedicationSafetyTest
{
    [Test]
    public void ReagentVariantsAreSummed()
    {
        var amounts = HealthAnalyzerMedicationSafety.GetAmounts(new ReagentQuantity[]
        {
            new("Bicaridine", 7),
            new("Bicaridine", 8),
            new("Kelotane", 3),
            new("Bruizine", 0),
        });

        Assert.That(amounts["Bicaridine"], Is.EqualTo(FixedPoint2.New(15)));
        Assert.That(amounts["Kelotane"], Is.EqualTo(FixedPoint2.New(3)));
        Assert.That(amounts.Count, Is.EqualTo(2));
    }

    [Test]
    public void FirstHarmfulThresholdWins()
    {
        var effects = new EntityEffect[] { DamageAt(20), DamageAt(15), DamageAt(30) };
        Assert.That(HealthAnalyzerMedicationSafety.FindThreshold("Bicaridine", effects), Is.EqualTo(FixedPoint2.New(15)));
    }

    [Test]
    public void HealingAndUnconditionalSideEffectsAreNotOverdoses()
    {
        var healing = DamageAt(5);
        healing.Damage.DamageDict["Poison"] = -2;
        var sideEffect = DamageAt(0);
        sideEffect.Conditions = null;

        Assert.That(HealthAnalyzerMedicationSafety.FindThreshold("Bicaridine", new[] { healing, sideEffect }), Is.Null);
    }

    [Test]
    public void AnotherReagentsThresholdIsNotUsed()
    {
        var effect = DamageAt(10);
        effect.Conditions = new EntityCondition[] { new ReagentCondition { Reagent = "Kelotane", Min = 10 } };
        Assert.That(HealthAnalyzerMedicationSafety.FindThreshold("Bicaridine", new[] { effect }), Is.Null);
    }

    [Test]
    public void MultipleDoseConditionsUseTheirIntersection()
    {
        var effect = DamageAt(10);
        effect.Conditions = new EntityCondition[]
        {
            new ReagentCondition { Reagent = "Bicaridine", Min = 10 },
            new ReagentCondition { Reagent = "Bicaridine", Min = 20 },
        };
        Assert.That(HealthAnalyzerMedicationSafety.FindThreshold("Bicaridine", new[] { effect }), Is.EqualTo(FixedPoint2.New(20)));
    }

    [Test]
    public void ConditionalAndBoundedEffectsAreNotUniversalThresholds()
    {
        var conditional = DamageAt(10);
        conditional.Conditions = new EntityCondition[]
        {
            new ReagentCondition { Reagent = "Bicaridine", Min = 10 },
            new NestedCondition { Proto = "FishTestCondition" },
        };
        var bounded = DamageAt(10);
        bounded.Conditions = new EntityCondition[] { new ReagentCondition { Reagent = "Bicaridine", Min = 10, Max = 20 } };
        Assert.That(HealthAnalyzerMedicationSafety.FindThreshold("Bicaridine", new[] { conditional, bounded }), Is.Null);
    }

    [Test]
    public void ImpossibleEffectIsIgnored()
    {
        var effect = DamageAt(5);
        effect.Probability = 0;
        Assert.That(HealthAnalyzerMedicationSafety.FindThreshold("Bicaridine", new[] { effect }), Is.Null);
    }

    [TestCase(0, MedicationRisk.None)]
    [TestCase(8.39, MedicationRisk.None)]
    [TestCase(8.4, MedicationRisk.NearThreshold)]
    [TestCase(10.49, MedicationRisk.NearThreshold)]
    [TestCase(10.5, MedicationRisk.ThresholdReached)]
    [TestCase(20, MedicationRisk.ThresholdReached)]
    public void FractionalThresholdBoundaries(double amount, MedicationRisk expected)
    {
        Assert.That(HealthAnalyzerMedicationSafety.GetRisk(FixedPoint2.New(amount), FixedPoint2.New(10.5)), Is.EqualTo(expected));
    }

    [Test]
    public void UnknownThresholdDoesNotInventAnOverdose()
    {
        Assert.That(HealthAnalyzerMedicationSafety.GetRisk(100, null), Is.EqualTo(MedicationRisk.None));
    }

    private static HealthChange DamageAt(int threshold)
    {
        return new HealthChange
        {
            Damage = new DamageSpecifier { DamageDict = { ["Poison"] = 1 } },
            Conditions = new EntityCondition[] { new ReagentCondition { Reagent = "Bicaridine", Min = threshold } },
        };
    }
}
