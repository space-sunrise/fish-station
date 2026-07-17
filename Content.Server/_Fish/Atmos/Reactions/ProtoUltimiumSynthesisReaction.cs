using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;
using System.Linq;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ProtoUltimiumSynthesisReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var pressure = mixture.Pressure;
        if (pressure < 8000f)
            return ReactionResult.NoReaction;

        var vapor = mixture.GetMoles(Gas.WaterVapor);
        var garodin = mixture.GetMoles(Gas.Garodin);
        var ammonia = mixture.GetMoles(Gas.Ammonia);

        if (vapor < 0.4f || garodin < 0.25f || ammonia < 0.15f)
            return ReactionResult.NoReaction;

        // Медленная реакция
        var efficiency = 0.45f;

        var produce = new[] 
        { 
            vapor * efficiency * 0.9f,
            garodin * efficiency,
            ammonia * efficiency 
        }.Min();

        if (produce < 0.09f)
            return ReactionResult.NoReaction;

        // Расход
        mixture.AdjustMoles(Gas.WaterVapor, -produce * 2.1f);
        mixture.AdjustMoles(Gas.Garodin,    -produce * 0.85f);
        mixture.AdjustMoles(Gas.Ammonia,    -produce * 0.95f);

        // Производим Protoultimium
        mixture.AdjustMoles(Gas.ProtoUltimium, produce);

        // КРАЙНЕ ЭНДОТЕРМИЧЕСКАЯ реакция
        var energyAbsorbed = produce * 1850f;   // сильное охлаждение
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);

        if (heatCap > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature = Math.Max(mixture.Temperature - (energyAbsorbed / heatCap), Atmospherics.TCMB);
        }

        return ReactionResult.Reacting;
    }
}