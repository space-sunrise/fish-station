using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Fish;

[TestFixture]
public sealed class FishCraftingTests : InteractionTest
{
    private static readonly ProtoId<ConstructionPrototype> ModularGrenadeRecipeProto = "ModularGrenadeRecipe";
    private static readonly ProtoId<ConstructionPrototype> MakeshiftPowerCageProto = "MakeshiftPowerCage";
    private static readonly ProtoId<ConstructionPrototype> TrashgunProto = "Trashgun";

    [Test]
    public async Task CraftGrenadeFromHeldBagOnly()
    {
        Assert.That(ProtoMan.HasIndex<ConstructionPrototype>(ModularGrenadeRecipeProto));

        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<Content.Shared.Hands.EntitySystems.SharedHandsSystem>();
            var containers = SEntMan.System<SharedContainerSystem>();
            var stacks = SEntMan.System<SharedStackSystem>();
            var player = SEntMan.GetEntity(Player);

            var bag = SEntMan.SpawnEntity("ClothingBackpack", SEntMan.GetCoordinates(PlayerCoords));
            Assert.That(hands.TryPickupAnyHand(player, bag, checkActionBlocker: false));

            var steel = SEntMan.SpawnEntity("SheetSteel", SEntMan.GetCoordinates(PlayerCoords));
            stacks.SetCount((steel, null), 5);

            Assert.That(SEntMan.TryGetComponent(bag, out StorageComponent storage));
            Assert.That(containers.Insert(steel, storage.Container));
        });

        await CraftItem("ModularGrenadeRecipe");
        await FindEntity("ModularGrenade");
    }

    [Test]
    public async Task CraftGrenadeFromSplitStacks()
    {
        await SpawnEntity((Steel, 3), SEntMan.GetCoordinates(PlayerCoords));
        await SpawnEntity((Steel, 3), SEntMan.GetCoordinates(PlayerCoords));
        await CraftItem("ModularGrenadeRecipe");
        await FindEntity("ModularGrenade");
    }

    [Test]
    public async Task CraftMakeshiftPowerCageFromFloor()
    {
        Assert.That(ProtoMan.HasIndex<ConstructionPrototype>(MakeshiftPowerCageProto));

        var coords = SEntMan.GetCoordinates(PlayerCoords);
        await SpawnEntity((Steel, 5), coords);
        await SpawnEntity((Cable, 5), coords);
        await SpawnEntity(("CableHV", 2), coords);
        await SpawnEntity((Glass, 2), coords);
        await SpawnTarget("PowerCellSmall");
        await SpawnTarget("PowerCellSmall");

        await CraftItem("MakeshiftPowerCage");
        await FindEntity("MakeshiftPowerCage");
    }

    [Test]
    public async Task CraftTrashgunFromFloor()
    {
        Assert.That(ProtoMan.HasIndex<ConstructionPrototype>(TrashgunProto));

        var coords = SEntMan.GetCoordinates(PlayerCoords);
        await SpawnEntity(("Plasteel", 1), coords);
        await SpawnEntity((Cable, 10), coords);
        await SpawnEntity((Steel, 5), coords);
        await SpawnTarget("EmergencyOxygenTankFilled");
        await SpawnTarget("PowerCellSmall");

        await Server.WaitAssertion(() =>
        {
            var xformSys = SEntMan.System<SharedTransformSystem>();
            var pipe = SEntMan.CreateEntityUninitialized("GasPipeStraight", coords);
            var xform = SEntMan.GetComponent<TransformComponent>(pipe);
            xform.Anchored = false;
            SEntMan.InitializeAndStartEntity(pipe);
            xformSys.AttachToGridOrMap(pipe);
            xformSys.SetCoordinates(pipe, coords);
        });

        await CraftItem("Trashgun");
        await FindEntity("WeaponMechIndustrialTrashgun");
    }
}
