// Fish edit - Vulpkanin PlanetWar Meme Mode
using Content.Server.GameTicking.Rules;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Server._Fish.PlanetWar;

public sealed class PlanetWarVulpkaninRuleSystem : GameRuleSystem<PlanetWarVulpkaninRuleComponent>
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Check if the Vulpkanin meme game rule is currently active
        var ruleQuery = EntityQueryEnumerator<PlanetWarVulpkaninRuleComponent>();
        bool ruleActive = ruleQuery.MoveNext(out _, out _);

        var query = EntityQueryEnumerator<PWVulpkaninCursePendingComponent>();
        while (query.MoveNext(out var uid, out var curse))
        {
            // If the meme rule is not active in this round, clean up the component and do nothing
            if (!ruleActive)
            {
                RemCompDeferred<PWVulpkaninCursePendingComponent>(uid);
                continue;
            }

            curse.Timer -= frameTime;
            if (curse.Timer > 0f)
                continue;

            // Timer expired: remove component and morph species to Vulpkanin
            RemCompDeferred<PWVulpkaninCursePendingComponent>(uid);

            if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
                continue;

            // Change species to Vulpkanin directly on the original entity
            _humanoid.SetSpecies(uid, "Vulpkanin", humanoid: humanoid);

            // Generate random Vulpkanin appearance (ears, snout, tail, markings, colors)
            var profile = HumanoidCharacterProfile.RandomWithSpecies("Vulpkanin");

            // Load profile while preserving the entity's custom role name
            var originalName = Name(uid);
            _humanoid.LoadProfile(uid, profile, humanoid);
            _metaData.SetEntityName(uid, originalName);
        }
    }
}
