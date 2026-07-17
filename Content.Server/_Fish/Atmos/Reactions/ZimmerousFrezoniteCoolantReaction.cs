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

        // === СИЛЬНО ЗАМЕДЛИЛИ РЕАКЦИЮ ===
        var baseEfficiency = 0.28f;                    // было 0.75–4.5, теперь сильно ниже

        // Охлаждение максимально при T >= 120 K, потом резко падает
        float coolingFactor;
        if (temperature >= 120f)
            coolingFactor = 1.0f;
        else
            coolingFactor = Math.Clamp((temperature - 15f) / 105f, 0.15f, 1.0f);

        var efficiency = baseEfficiency * coolingFactor;

        // Очень медленный burnRate
        var burnRate = zmf * efficiency / 4.8f;        // сильно замедлили

        if (burnRate < 0.04f)
            return ReactionResult.NoReaction;

        // Расход реагентов
        var nitConsumed = Math.Min(burnRate * 5.5f, nitrogen);
        var zmfConsumed = Math.Min(burnRate, zmf);

        mixture.AdjustMoles(Gas.Nitrogen, -nitConsumed);
        mixture.AdjustMoles(Gas.ZimmerousFrezonite, -zmfConsumed);

        // Выработка N2O
        var n2oProduced = (nitConsumed + zmfConsumed) * 0.68f * coolingFactor;
        mixture.AdjustMoles(Gas.NitrousOxide, n2oProduced);

        // Охлаждение — теперь максимально выше 120 K, потом слабеет
        var energyAbsorbed = burnRate * 12800f * coolingFactor;

        var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
        var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);

        if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature = (temperature * oldHeatCapacity - energyAbsorbed) / newHeatCapacity;
        }

        return ReactionResult.Reacting;
    }
}