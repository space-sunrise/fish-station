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

        /// <summary>
        /// Message shown in the EORG/volunteer popup.
        /// </summary>
        public static readonly CVarDef<string> EorgPopupMessage =
            CVarDef.Create("fish.eorg_popup_message", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Discord link shown in the EORG/volunteer popup.
        /// </summary>
        public static readonly CVarDef<string> EorgPopupLink =
            CVarDef.Create("fish.eorg_popup_link", "https://discord.com/channels/837289702369263676/1496182871562387667", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Клиентский CRT-тема для Fish UI (достижения и др.). Архив локально.
        /// </summary>
        public static readonly CVarDef<bool> FishCrtThemeEnabled =
            CVarDef.Create("fish.crt_theme_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// CRT-эффекты (scanlines и т.п.). Имеют смысл только при включённой CRT-теме.
        /// </summary>
        public static readonly CVarDef<bool> FishCrtEffectsEnabled =
            CVarDef.Create("fish.crt_effects_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);
    }
}
