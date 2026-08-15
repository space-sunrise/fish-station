using System.IO;
using Content.Shared._Fish.ObrCall;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared._Fish.ObrCall;

[TestFixture]
[TestOf(typeof(ObrTeamPrototype))]
public sealed class ObrTeamPrototypeTests : ContentUnitTest
{
    private static readonly ProtoId<ObrTeamPrototype> AmberId = "ObrAmber";
    private static readonly ProtoId<ObrTeamPrototype> RedId = "ObrRed";
    private static readonly ProtoId<ObrTeamPrototype> GammaId = "ObrGamma";
    private static readonly ProtoId<ObrTeamPrototype> CburnId = "ObrCburn";

    private const string Prototypes = @"
- type: entity
  id: TestFishObrRuleAmber
  abstract: true

- type: entity
  id: TestFishObrRuleRed
  abstract: true

- type: entity
  id: TestFishObrRuleGamma
  abstract: true

- type: entity
  id: TestFishObrRuleCburn
  abstract: true

- type: obrTeam
  id: ObrAmber
  name: obr-team-amber-name
  gameRule: TestFishObrRuleAmber
  stationCost: 50000
  centCommAvailable: true
  stationAvailable: true
  sortOrder: 10

- type: obrTeam
  id: ObrRed
  name: obr-team-red-name
  gameRule: TestFishObrRuleRed
  stationCost: 100000
  centCommAvailable: true
  stationAvailable: true
  sortOrder: 20

- type: obrTeam
  id: ObrGamma
  name: obr-team-gamma-name
  gameRule: TestFishObrRuleGamma
  centCommAvailable: true
  stationAvailable: false
  sortOrder: 30

- type: obrTeam
  id: ObrCburn
  name: obr-team-cburn-name
  gameRule: TestFishObrRuleCburn
  stationCost: 100000
  centCommAvailable: true
  stationAvailable: true
  sortOrder: 40
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
    public void StationPurchasePricesMatchSpec()
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        Assert.That(protoMan.TryIndex(AmberId, out var amber), Is.True);
        Assert.That(amber!.StationCost, Is.EqualTo(50000));
        Assert.That(amber.StationAvailable, Is.True);
        Assert.That(amber.CentCommAvailable, Is.True);

        Assert.That(protoMan.TryIndex(RedId, out var red), Is.True);
        Assert.That(red!.StationCost, Is.EqualTo(100000));
        Assert.That(red.StationAvailable, Is.True);

        Assert.That(protoMan.TryIndex(CburnId, out var cburn), Is.True);
        Assert.That(cburn!.StationCost, Is.EqualTo(100000));
        Assert.That(cburn.StationAvailable, Is.True);

        Assert.That(protoMan.TryIndex(GammaId, out var gamma), Is.True);
        Assert.That(gamma!.StationAvailable, Is.False);
        Assert.That(gamma.StationCost, Is.Null);
        Assert.That(gamma.CentCommAvailable, Is.True);
    }

    [Test]
    public void GammaNotPurchasableByStation()
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        Assert.That(protoMan.Index(GammaId).StationAvailable, Is.False);
        Assert.That(protoMan.Index(GammaId).StationCost, Is.Null);
    }
}
