using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;
using System.Linq;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ZimmeraSynthesisReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var temperature = mixture.Temperature;
        var pressure = mixture.Pressure;

        if (temperature > 70f || pressure < 1000f)
            return ReactionResult.NoReaction;

        var oxygen = mixture.GetMoles(Gas.Oxygen);
        var garodin = mixture.GetMoles(Gas.Garodin);
        var co2 = mixture.GetMoles(Gas.CarbonDioxide);        // ← Frezon заменён на CO2

        if (oxygen < 0.15f || garodin < 0.15f || co2 < 0.15f)
            return ReactionResult.NoReaction;

        // Чем больше доля CO2 — тем медленнее реакция
        var totalReactants = oxygen + garodin + co2;
        var co2Ratio = co2 / totalReactants;

        // Сильно уменьшил скорость реакции
        var efficiency = 0.085f * (1f - co2Ratio * 0.75f);   // теперь заметно медленнее

        var produceZimmera = new[] 
        { 
            oxygen * efficiency, 
            garodin * efficiency, 
            co2 * efficiency * 1.25f 
        }.Min();

        if (produceZimmera < 0.06f)
            return ReactionResult.NoReaction;

        // Расход реагентов (сильно урезан)
        mixture.AdjustMoles(Gas.Oxygen,     -produceZimmera * 0.75f);
        mixture.AdjustMoles(Gas.Garodin,    -produceZimmera * 0.75f);
        mixture.AdjustMoles(Gas.CarbonDioxide, -produceZimmera * 0.85f);

        // Производим Zimmera
        mixture.AdjustMoles(Gas.Zimmera, produceZimmera);

        // Выработка BZ зависит от количества CO2 (чем больше CO2 — тем больше BZ)
        var bzProduced = produceZimmera * 0.1f * (0.8f + co2Ratio * 1.8f);
        mixture.AdjustMoles(Gas.BZ, bzProduced);

        // Экзотермическая реакция (тепло)
        var energyReleased = produceZimmera * 3800f + bzProduced * 650f;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);

        if (heatCap > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature += energyReleased / heatCap;
        }

        return ReactionResult.Reacting;
    }
}