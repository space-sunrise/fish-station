using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;
using System.Linq;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class FramelSynthesisReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var temperature = mixture.Temperature;
        if (temperature < 100f || temperature > 200f)
            return ReactionResult.NoReaction;

        var garodin = mixture.GetMoles(Gas.Garodin);
        var healium = mixture.GetMoles(Gas.Healium);
        var nitrium = mixture.GetMoles(Gas.Nitrium);
        var bz = mixture.GetMoles(Gas.BZ);

        if (garodin < 0.15f || healium < 0.3f || nitrium < 0.3f)
            return ReactionResult.NoReaction;

        // Медленная реакция
        var efficiency = 0.25f;

        var produceFramel = new[] 
        { 
            garodin * efficiency,
            healium * efficiency,
            nitrium * efficiency 
        }.Min();

        if (produceFramel < 0.08f)
            return ReactionResult.NoReaction;

        // Расход реагентов
        mixture.AdjustMoles(Gas.Garodin, -produceFramel * 1.05f);
        mixture.AdjustMoles(Gas.Healium, -produceFramel * 1.25f);
        mixture.AdjustMoles(Gas.Nitrium, -produceFramel * 1.25f);
        // BZ НЕ расходуется

        // Производим Framel
        mixture.AdjustMoles(Gas.Framel, produceFramel);

        // === ИСПРАВЛЕНИЕ ===
        // Ammonia вырабатывается ТОЛЬКО если в смеси есть минимум 5% BZ
        var ammoniaProduced = 0f;

        if (bz >= 0.05f)
        {
            var bzRatio = Math.Clamp(bz / mixture.TotalMoles, 0.05f, 0.10f);
            // Чем больше BZ (до 10%) — тем больше Ammonia
            ammoniaProduced = produceFramel * 1.95f * (1f + bzRatio * 6f);
        }

        mixture.AdjustMoles(Gas.Ammonia, ammoniaProduced);

        // Слабый нагрев (экзотермическая)
        var energyReleased = produceFramel * 1450f + ammoniaProduced * 600f;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);

        if (heatCap > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature += energyReleased / heatCap;
        }

        return ReactionResult.Reacting;
    }
}