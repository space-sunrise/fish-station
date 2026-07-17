using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;
using System.Linq;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class PralliumSynthesisReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var temperature = mixture.Temperature;
        var pressure = mixture.Pressure;
        if (temperature < 5000f || pressure >= 800f)
            return ReactionResult.NoReaction;

        var oxygen = mixture.GetMoles(Gas.Oxygen);
        var co2 = mixture.GetMoles(Gas.CarbonDioxide);
        var nitrium = mixture.GetMoles(Gas.Nitrium);

        if (oxygen < 0.4f || co2 < 0.2f || nitrium < 0.2f)
            return ReactionResult.NoReaction;

        // Соотношение 2 : 1 : 1 (Oxygen 50% / CO2 25% / Nitrium 25%)
        var efficiency = 2.6f;   // можно подкрутить

        var maxFromOxy = oxygen / 2f * efficiency;
        var maxFromCo2 = co2 * efficiency;
        var maxFromNit = nitrium * efficiency;

        var produce = new[] { maxFromOxy, maxFromCo2, maxFromNit }.Min();

        if (produce < 0.05f)
            return ReactionResult.NoReaction;

        // Потребляем реагенты
        var consOxy = produce / efficiency * 2f;
        var consCo2 = produce / efficiency;
        var consNit = produce / efficiency;

        mixture.AdjustMoles(Gas.Oxygen, -consOxy);
        mixture.AdjustMoles(Gas.CarbonDioxide, -consCo2);
        mixture.AdjustMoles(Gas.Nitrium, -consNit);

        // Производим Prallium
        mixture.AdjustMoles(Gas.Prallium, produce);

        // РЕЗКО ЭНДОТЕРМИЧЕСКАЯ — температура падает очень сильно
        var energyAbsorbed = produce * 10200f;   // можно сделать 6000–7000f, если хочешь ещё холоднее
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);

        if (heatCap > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature = Math.Max(mixture.Temperature - (energyAbsorbed / heatCap), Atmospherics.TCMB);
        }

        return ReactionResult.Reacting;
    }
}