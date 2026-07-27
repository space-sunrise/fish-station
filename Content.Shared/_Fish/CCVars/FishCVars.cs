using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar
{
    [CVarDefs]
    public sealed class FishCVars
    {
        /// <summary>
        /// Whether the EORG popup is enabled.
        /// </summary>
        public static readonly CVarDef<bool> EorgPopupEnabled =
            CVarDef.Create("fish.eorg_popup_enabled", true, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// How long to display the EORG popup for.
        /// </summary>
        public static readonly CVarDef<float> EorgPopupTime =
            CVarDef.Create("fish.eorg_popup_time", 5f, CVar.SERVER | CVar.REPLICATED);
    }
}
