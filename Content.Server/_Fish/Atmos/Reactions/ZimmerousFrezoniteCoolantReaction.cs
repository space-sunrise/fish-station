using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ZimmerousFrezoniteCoolantReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var temperature = mixture.Temperature;

        if (temperature <= 15f)
            return ReactionResult.NoReaction;

        var nitrogen = mixture.GetMoles(Gas.Nitrogen);
        var zmf = mixture.GetMoles(Gas.ZimmerousFrezonite);

        if (nitrogen < 0.25f || zmf < 0.12f)
            return ReactionResult.NoReaction;

        var baseEfficiency = 0.2f;

        // охлаждение максимально при T >= 120 K, потом резко падает
        float coolingFactor;
        if (temperature >= 120f)
            coolingFactor = 1.0f;
        else
            coolingFactor = temperature / 120f;

        var efficiency = baseEfficiency * coolingFactor;

        // Очень медленный burnRate
        var burnRate = zmf * efficiency / 5f;

        if (burnRate < 0.004f)
            return ReactionResult.NoReaction;

        // расход реагентов
        var nitConsumed = burnRate * 5.5f;
        var zmfConsumed = burnRate;

        mixture.AdjustMoles(Gas.Nitrogen, -nitConsumed);
        mixture.AdjustMoles(Gas.ZimmerousFrezonite, -zmfConsumed);

        // выработка N2O
        var n2oProduced = (nitConsumed + zmfConsumed) * 0.5f * coolingFactor;
        mixture.AdjustMoles(Gas.NitrousOxide, n2oProduced);

        var energyAbsorbed = burnRate * 12800f * coolingFactor;

        var HeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);

        if (HeatCapacity > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature -= energyAbsorbed / HeatCapacity;
        }

        return ReactionResult.Reacting;
    }
}