// Fish edit - Vulpkanin PlanetWar Meme Mode
using Content.Server.GameTicking.Rules;
using Content.Server.Polymorph.Systems;
using Content.Shared.Humanoid;
using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server._Fish.PlanetWar;

public sealed class PlanetWarVulpkaninRuleSystem : GameRuleSystem<PlanetWarVulpkaninRuleComponent>
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, HumanoidAppearanceComponent component, MindAddedMessage args)
    {
        // Only run if the Vulpkanin game rule is currently active
        var query = EntityQueryEnumerator<PlanetWarVulpkaninRuleComponent>();
        if (!query.MoveNext(out _, out _))
            return;

        // Prevent infinite loop since polymorphing spawns a Vulpkanin entity and transfers the mind (triggering MindAddedMessage again)
        if (component.Species == "Vulpkanin")
            return;

        // Polymorph the player permanently into a randomized Vulpkanin
        _polymorph.PolymorphEntity(uid, "PermanentlyVulpkanin");
    }
}
