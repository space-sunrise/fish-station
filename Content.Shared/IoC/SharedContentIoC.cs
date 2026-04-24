using Content.Shared._Sunrise.BloodCult.Systems;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register(IDependencyCollection deps)
        {
            try
            {
                deps.Register<MarkingManager, MarkingManager>();
            }
            catch (InvalidOperationException)
            {
                // Already registered, skip
            }
            deps.Register<ContentLocalizationManager, ContentLocalizationManager>();
            deps.Register<CultistWordGeneratorManager, CultistWordGeneratorManager>(); // Sunrise-Edit
        }
    }
}
