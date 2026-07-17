using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ProtoUltimiumDecompositionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var pressure = mixture.Pressure;

        // Реакция разложения идёт ТОЛЬКО при низком давлении (< 200 кПа)
        if (pressure >= 200f)
            return ReactionResult.NoReaction;

        var protoMoles = mixture.GetMoles(Gas.ProtoUltimium);

        // Используем ту же механику, что и у N2O, но чуть медленнее
        var burnedFuel = protoMoles / 2.8f;   // 2.8f — скорость разложения (можно подкрутить)

        if (burnedFuel <= 0 || protoMoles - burnedFuel < 0)
            return ReactionResult.NoReaction;

        // Убираем Protoultimium
        mixture.AdjustMoles(Gas.ProtoUltimium, -burnedFuel);

        // Разложение: азот + кислород + МНОГО плазмы
        mixture.AdjustMoles(Gas.Nitrogen, burnedFuel * 0.85f);
        mixture.AdjustMoles(Gas.Oxygen,   burnedFuel * 0.65f);
        mixture.AdjustMoles(Gas.Plasma,   burnedFuel * 2.4f);   // очень много плазмы, как ты хотел

        return ReactionResult.Reacting;
    }
}