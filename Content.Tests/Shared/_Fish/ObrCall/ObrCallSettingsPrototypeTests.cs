using System.IO;
using Content.Shared._Fish.ObrCall;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared._Fish.ObrCall;

[TestFixture]
[TestOf(typeof(ObrCallSettingsPrototype))]
public sealed class ObrCallSettingsPrototypeTests : ContentUnitTest
{
    private static readonly ProtoId<ObrCallSettingsPrototype> SettingsId = "DefaultObrCallSettings";

    private const string Prototypes = @"
- type: obrCallSettings
  id: DefaultObrCallSettings
  arrivalDistance: 1500
  distanceStep: 100
  maxArrivalDistance: 2500
  attemptsPerRadius: 16
  clearancePadding: 4
  earliestCallTime: 1800
  centCommArrivalDistance: 100
  centCommDistanceStep: 10
  centCommMaxArrivalDistance: 300
  centCommAttemptsPerRadius: 24
";

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        prototypeManager.Initialize();
        prototypeManager.LoadFromStream(new StringReader(Prototypes));
        prototypeManager.ResolveResults();
    }

    [Test]
    public void DefaultArrivalDistanceIs1500()
    {
        var settings = IoCManager.Resolve<IPrototypeManager>().Index(SettingsId);
        Assert.That(settings.ArrivalDistance, Is.EqualTo(1500f));
        Assert.That(settings.DistanceStep, Is.EqualTo(100f));
        Assert.That(settings.MaxArrivalDistance, Is.GreaterThanOrEqualTo(settings.ArrivalDistance));
        Assert.That(settings.AttemptsPerRadius, Is.GreaterThan(0));
        Assert.That(settings.EarliestCallTime, Is.EqualTo(System.TimeSpan.FromMinutes(30)));
        Assert.That(settings.CentCommArrivalDistance, Is.EqualTo(100f));
        Assert.That(settings.CentCommMaxArrivalDistance, Is.EqualTo(300f));
    }
}
