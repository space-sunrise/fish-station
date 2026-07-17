using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using System;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ZimmerousFrezoniteSynthesisReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var temperature = mixture.Temperature;
        if (temperature > 50f)
            return ReactionResult.NoReaction;

        var frezon = mixture.GetMoles(Gas.Frezon);
        var zimmera = mixture.GetMoles(Gas.Zimmera);

        if (frezon < 0.2f || zimmera < 0.2f)
            return ReactionResult.NoReaction;

        // 50/50 смесь
        var efficiency = 0.1f;

        var produce = MathF.Min(frezon, zimmera) * efficiency;

        if (produce < 0.08f)
            return ReactionResult.NoReaction;

        // Расход 50/50
        mixture.AdjustMoles(Gas.Frezon, -produce * 0.5f);
        mixture.AdjustMoles(Gas.Zimmera, -produce * 0.5f);

        // Производим ZimmerousFrezonite
        mixture.AdjustMoles(Gas.ZimmerousFrezonite, produce);

        // КРАЙНЕ ЭНДОТЕРМИЧЕСКАЯ реакция — очень сильное охлаждение
        var energyAbsorbed = produce * 28500f;     // сильно увеличил по сравнению с обычным фрезоном

        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
        {
            mixture.Temperature = Math.Max(mixture.Temperature - (energyAbsorbed / heatCap), Atmospherics.TCMB);
        }

        return ReactionResult.Reacting;
    }
}