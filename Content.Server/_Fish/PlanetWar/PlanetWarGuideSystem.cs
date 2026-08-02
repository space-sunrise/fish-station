using Content.Shared._Fish.PlanetWar;

namespace Content.Server._Fish.PlanetWar;

public sealed class PlanetWarGuideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OpenPlanetWarGuideActionEvent>(OnOpenPlanetWarGuide);
    }

    private void OnOpenPlanetWarGuide(OpenPlanetWarGuideActionEvent args)
    {
        args.Handled = true;
    }
}
