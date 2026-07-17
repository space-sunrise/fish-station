using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;
using System.Linq;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class GarodinSynthesisReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var temperature = mixture.Temperature;
        if (temperature < 10000f || temperature > 20000f)
            return ReactionResult.NoReaction;

        var plasma = mixture.GetMoles(Gas.Plasma);
        var frezon = mixture.GetMoles(Gas.Frezon);

        // низкий порог запуска 
        if (plasma < 0.05f || frezon < 0.05f)
            return ReactionResult.NoReaction;

        // Соотношение примерно 75% Plasma + 25% Frezon
        var efficiency = 2.8f;

        var maxFromPlasma = plasma * efficiency * 0.75f;
        var maxFromFrezon = frezon * efficiency * 0.25f;

        var produce = new[] { maxFromPlasma, maxFromFrezon }.Min();

        if (produce < 0.08f)
            return ReactionResult.NoReaction;

        // Потребляем
        var consPlasma = produce / efficiency * 0.75f * 1.15f; // чуть больше расход плазмы
        var consFrezon = produce / efficiency * 0.25f;

        mixture.AdjustMoles(Gas.Plasma, -consPlasma);
        mixture.AdjustMoles(Gas.Frezon, -consFrezon);

        mixture.AdjustMoles(Gas.Garodin, produce);

        // сильно эндотермическая реакция — температура резко падает
        var energyAbsorbed = produce * 12000f;   // очень сильное охлаждение
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);

        if (heatCap > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature = Math.Max(mixture.Temperature - (energyAbsorbed / heatCap), Atmospherics.TCMB);
        }

        return ReactionResult.Reacting;
    }
}