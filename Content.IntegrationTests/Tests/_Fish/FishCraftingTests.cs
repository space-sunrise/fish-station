using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Fish;

/// <summary>
/// Регрессии начального крафта Fish: дедуп стаков в сумке, multi-stack, component-insert.
/// </summary>
[TestFixture]
public sealed class FishCraftingTests : InteractionTest
{
    /// <summary>
    /// Сталь только в сумке в руках — раньше EnumerateNearby мог считать её дважды
    /// (hands storage + nearby lookup) и крафт падал после «телепорта» материалов под ноги.
    /// </summary>
    [Test]
    public async Task CraftGrenadeFromHeldBagOnly()
    {
        Assert.That(ProtoMan.HasIndex<ConstructionPrototype>("ModularGrenadeRecipe"));

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

    /// <summary>
    /// Два стака по 3 стали на рецепт на 5 — раньше требовался один стак >= amount.
    /// </summary>
    [Test]
    public async Task CraftGrenadeFromSplitStacks()
    {
        await SpawnEntity((Steel, 3), SEntMan.GetCoordinates(PlayerCoords));
        await SpawnEntity((Steel, 3), SEntMan.GetCoordinates(PlayerCoords));
        await CraftItem("ModularGrenadeRecipe");
        await FindEntity("ModularGrenade");
    }

    /// <summary>
    /// MakeshiftPowerCage: материалы + component PowerCell x2.
    /// </summary>
    [Test]
    public async Task CraftMakeshiftPowerCageFromFloor()
    {
        Assert.That(ProtoMan.HasIndex<ConstructionPrototype>("MakeshiftPowerCage"));

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
}
