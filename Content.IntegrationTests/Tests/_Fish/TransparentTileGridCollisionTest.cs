using System.Linq;
using System.Numerics;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests._Fish;

/// <summary>
/// Verifies that tiles with enableGridCollision: false do not create grid fixtures
/// unless a dense anchored blocker (wall / closed airlock) occupies the cell.
/// </summary>
[TestFixture]
public sealed class TransparentTileGridCollisionTest
{
    private static int GridChunkFixtureCount(EntityManager entMan, EntityUid grid)
    {
        if (!entMan.TryGetComponent(grid, out FixturesComponent? fixtures))
            return 0;

        return fixtures.Fixtures.Keys.Count(id => id.StartsWith("grid_chunk-", StringComparison.Ordinal));
    }

    [Test]
    public async Task TransparentTilePassesUntilBlocked()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var mapMan = server.MapMan;
        var mapSys = entMan.System<SharedMapSystem>();
        var physSys = entMan.System<SharedPhysicsSystem>();
        var doorSys = entMan.System<SharedDoorSystem>();
        var tileMan = server.ResolveDependency<ITileDefinitionManager>();
        var xformSys = entMan.System<SharedTransformSystem>();

        var steelId = tileMan["FloorSteel"].TileId;
        var transparentId = tileMan["FloorTransparent"].TileId;

        var testMap = await pair.CreateTestMap();

        Entity<MapGridComponent> gridA = default;
        Entity<MapGridComponent> gridB = default;
        EntityUid wall = default;
        EntityUid airlock = default;

        await server.WaitAssertion(() =>
        {
            var mapId = testMap.MapId;
            gridA = mapMan.CreateGridEntity(mapId);
            gridB = mapMan.CreateGridEntity(mapId);

            mapSys.SetTile(gridA, gridA, Vector2i.Zero, new Tile(steelId));
            mapSys.SetTile(gridB, gridB, Vector2i.Zero, new Tile(transparentId));

            xformSys.SetWorldPosition(gridA, Vector2.Zero);
            xformSys.SetWorldPosition(gridB, Vector2.Zero);

            var physicsA = entMan.GetComponent<PhysicsComponent>(gridA);
            var physicsB = entMan.GetComponent<PhysicsComponent>(gridB);
            physSys.SetBodyType(gridA, BodyType.Dynamic, body: physicsA);
            physSys.SetBodyType(gridB, BodyType.Dynamic, body: physicsB);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(GridChunkFixtureCount(entMan, gridA), Is.GreaterThan(0),
                "Обычный тайл должен создавать grid fixtures");
            Assert.That(GridChunkFixtureCount(entMan, gridB), Is.EqualTo(0),
                "Прозрачный тайл без препятствий не должен создавать grid fixtures");
        });

        await server.WaitAssertion(() =>
        {
            wall = entMan.SpawnEntity("WallSolid", new EntityCoordinates(gridB, 0.5f, 0.5f));
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(GridChunkFixtureCount(entMan, gridB), Is.GreaterThan(0),
                "Стена на прозрачном тайле должна восстанавливать grid fixtures");
            entMan.DeleteEntity(wall);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(GridChunkFixtureCount(entMan, gridB), Is.EqualTo(0),
                "После удаления стены прозрачный тайл снова без fixtures");

            airlock = entMan.SpawnEntity("Airlock", new EntityCoordinates(gridB, 0.5f, 0.5f));
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(GridChunkFixtureCount(entMan, gridB), Is.GreaterThan(0),
                "Закрытый гермозатвор на прозрачном тайле должен создавать fixtures");

            Assert.That(entMan.TryGetComponent(airlock, out DoorComponent? door), Is.True);
            doorSys.StartOpening(airlock, door);
            doorSys.OnPartialOpen(airlock, door);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(GridChunkFixtureCount(entMan, gridB), Is.EqualTo(0),
                "Открытый гермозатвор снова убирает fixtures");

            physSys.SetCanCollide(airlock, true);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(GridChunkFixtureCount(entMan, gridB), Is.GreaterThan(0),
                "Повторное закрытие гермозатвора снова создаёт fixtures");

            mapSys.SetTile(gridB, gridB, Vector2i.Zero, new Tile(steelId));
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(GridChunkFixtureCount(entMan, gridB), Is.GreaterThan(0),
                "Обычный тайл сохраняет grid fixtures");
        });

        await pair.CleanReturnAsync();
    }
}
