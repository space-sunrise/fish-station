using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class PermafrostDecompositionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var temperature = mixture.Temperature;
        var permafrost = mixture.GetMoles(Gas.Permafrost);
        var tritium = mixture.GetMoles(Gas.Tritium);

        if (temperature >= 50f || permafrost < 0.05f)
            return ReactionResult.NoReaction;

        // Медленное разложение
        var decompRate = (50f - temperature) * 0.102f;   // очень медленно

        if (tritium > 0.04f)
        {
            // При наличии трития — расходуется тритий вместо Permafrost
            var tritConsumed = Math.Min(decompRate * 0.75f, tritium);
            mixture.AdjustMoles(Gas.Tritium, -tritConsumed);
        }
        else
        {
            // Разложение самого Permafrost
            mixture.AdjustMoles(Gas.Permafrost, -decompRate);
            mixture.AdjustMoles(Gas.Frezon, decompRate * 0.88f);
        }

        // Очень слабый нагрев (чтобы не была самоподдерживающейся)
        var energyReleased = decompRate * 250f;

        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature += energyReleased / heatCap;
        }

        return ReactionResult.Reacting;
    }
}