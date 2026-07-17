using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ZenthiumSynthesisReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var temperature = mixture.Temperature;

        // Минимальная температура 500 K
        if (temperature < 500f)
            return ReactionResult.NoReaction;

        var healiumMoles = mixture.GetMoles(Gas.Healium);
        var frezonMoles = mixture.GetMoles(Gas.Frezon);

        // Минимальные количества для запуска реакции
        if (healiumMoles < 0.2f || frezonMoles < 0.8f)
            return ReactionResult.NoReaction;

        // Соотношение примерно 1 Healium : 4 Frezon → производим ~1.5–2 Zenthium
        var maxFromHealium = healiumMoles * 1.8f;           // Healium — лимитирующий реагент
        var maxFromFrezon  = frezonMoles   / 4f * 1.8f;     // Frezon даёт больше "массы"

        var produceAmount = MathF.Min(maxFromHealium, maxFromFrezon);

        // Не производим слишком мало
        if (produceAmount < 0.05f)
            return ReactionResult.NoReaction;

        // Сколько реально потребляем
        var healiumConsumed = produceAmount / 1.8f;
        var frezonConsumed  = produceAmount * 4f / 1.8f;

        // Убираем реагенты
        mixture.AdjustMoles(Gas.Healium, -healiumConsumed);
        mixture.AdjustMoles(Gas.Frezon,  -frezonConsumed);

        // Производим Zenthium
        mixture.AdjustMoles(Gas.Zenthium, produceAmount);

        // Реакция слегка охлаждает смесь (эндотермическая)
        var energyAbsorbed = produceAmount * 1200f;         // подбери значение под баланс
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);

        if (heatCap > Atmospherics.MinimumHeatCapacity)
        {
            var deltaTemp = -energyAbsorbed / heatCap;
            mixture.Temperature = Math.Max(mixture.Temperature + deltaTemp, Atmospherics.TCMB);
        }

        return ReactionResult.Reacting;
    }
}