using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class FishAtmosphereCVarsSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public override void Initialize()
        {
            base.Initialize();

            _cfg.SetCVar(SunriseCCVars.GasPrices, new Dictionary<string, double>(){
                { "Tritium", 2.5 },
                { "NitrousOxide", 0.1 },
                { "Frezon", 1 },
                { "BZ", 1 },
                { "Healium", 12 },
                { "Nitrium", 2 },
            });
        }
    }
}
