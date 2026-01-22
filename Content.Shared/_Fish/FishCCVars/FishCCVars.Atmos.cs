using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Fish.FishCCVars;


public sealed partial class FishCCVars : CVars
{
    /// <summary>
    /// DeltaP threshold overrides for pressure window shattering
    /// </summary>
    public static readonly CVarDef<float> DeltaPReinforcedPlasma =
        CVarDef.Create("atmos.reinforced_plasma_window_deltaP_threshold", 300000f, CVar.SERVER);

    public static readonly CVarDef<float> DeltaPReinforced =
        CVarDef.Create("atmos.reinforced_window_deltaP_threshold", 80000f, CVar.SERVER);
}
