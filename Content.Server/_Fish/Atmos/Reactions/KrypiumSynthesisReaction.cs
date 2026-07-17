using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;
using System.Linq;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class KrypiumSynthesisReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var temperature = mixture.Temperature;
        var pressure = mixture.Pressure;

        // Реакция идёт ТОЛЬКО при очень низкой температуре (~150 K) и низком давлении
        if (temperature > 100f || pressure >= 100f)
            return ReactionResult.NoReaction;

        var healium = mixture.GetMoles(Gas.Healium);
        var bz = mixture.GetMoles(Gas.BZ);
        var plasma = mixture.GetMoles(Gas.Plasma);

        // Минимальные пороги запуска
        if (healium < 0.3f || bz < 0.3f || plasma < 0.9f)
            return ReactionResult.NoReaction;

        var efficiency = 3.5f;  

        // Соотношение: Healium : BZ : Plasma ≈ 1 : 1 : 3
        var maxFromHealium = healium * efficiency;
        var maxFromBz      = bz      * efficiency;
        var maxFromPlasma  = plasma  * (efficiency / 3f);  // Plasma тратится в 3 раза больше, поэтому делим

        var produceKrypium = new[] { maxFromHealium, maxFromBz, maxFromPlasma }.Min();

        if (produceKrypium < 0.05f)
            return ReactionResult.NoReaction;

        // Потребляем реагенты
        var consumedHealium = produceKrypium / efficiency;
        var consumedBz      = produceKrypium / efficiency;
        var consumedPlasma  = (produceKrypium / efficiency) * 3f;

        mixture.AdjustMoles(Gas.Healium, -consumedHealium);
        mixture.AdjustMoles(Gas.BZ,      -consumedBz);
        mixture.AdjustMoles(Gas.Plasma,  -consumedPlasma);

        // Производим Krypium
        mixture.AdjustMoles(Gas.Krypium, produceKrypium);

        // + небольшое количество Carbon Dioxide (как ты просил)
        var co2Produced = produceKrypium * 10f;
        mixture.AdjustMoles(Gas.CarbonDioxide, co2Produced);

        // Экзотермическая реакция, но НЕ сильно (нагрев примерно на 150–250 K в зависимости от heat capacity)
        var energyReleased = produceKrypium * 2000f;   // можно подкрутить (меньше = слабее нагрев)
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);

        if (heatCap > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature += energyReleased / heatCap;
        }

        return ReactionResult.Reacting;
    }
}