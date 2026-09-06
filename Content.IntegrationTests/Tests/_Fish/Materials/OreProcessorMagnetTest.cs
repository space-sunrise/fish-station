using System.Linq;
using System.Numerics;
using Content.Server._Fish.Materials;
using Content.Server.Verbs;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Fish.Materials;

[TestFixture]
[TestOf(typeof(OreProcessorMagnetSystem))]
public sealed class OreProcessorMagnetTest
{
    private const string MagnetTargetId = "FishOreProcessorMagnetTestTarget";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {MagnetTargetId}
  components:
  - type: OreProcessorMagnet
";

    /// <summary>
    /// Проверяет регрессию, при которой сохранённое действие из уже открытого меню
    /// активировало магнит после выхода пользователя из радиуса взаимодействия.
    /// </summary>
    [Test]
    public async Task ActivationVerbAfterUserLeavesRangeDoesNotActivate()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var actionBlocker = entityManager.System<ActionBlockerSystem>();
        var interaction = entityManager.System<SharedInteractionSystem>();
        var transform = entityManager.System<SharedTransformSystem>();
        var verbSystem = entityManager.System<VerbSystem>();

        var map = await pair.CreateTestMap();
        EntityUid user = default;
        EntityUid target = default;
        ActivationVerb activationVerb = null!;

        await server.WaitPost(() =>
        {
            user = entityManager.SpawnEntity("MobHuman", map.GridCoords);
            target = entityManager.SpawnEntity(MagnetTargetId, map.GridCoords);
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(actionBlocker.CanInteract(user, target), Is.True);
            Assert.That(interaction.InRangeAndAccessible(user, target), Is.True);

            // Сохраняем действие так же, как уже открытое контекстное меню.
            var verbs = verbSystem.GetLocalVerbs(target, user, typeof(ActivationVerb));
            activationVerb = verbs.OfType<ActivationVerb>().Single();
        });

        await server.WaitPost(() =>
        {
            // Имитируем отход игрока до нажатия на ранее полученное действие.
            transform.SetLocalPosition(user, new Vector2(10f, 0f));
            activationVerb!.Act!.Invoke();
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(interaction.InRangeAndAccessible(user, target), Is.False);
            Assert.That(entityManager.HasComponent<ActiveOreProcessorMagnetComponent>(target), Is.False);
        });

        await pair.CleanReturnAsync();
    }
}
