using Robust.Shared.Configuration;

namespace Content.Shared._Fish.PerformanceGuardian;

public sealed partial class FishCCVars
{
    /// <summary>
    /// Главный выключатель Performance Guardian.
    /// </summary>
    public static readonly CVarDef<bool> PgEnabled =
        CVarDef.Create("pg.enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Интервал дешёвого idle-сэмпла (секунды).
    /// </summary>
    public static readonly CVarDef<float> PgSampleIntervalSeconds =
        CVarDef.Create("pg.sample_interval_seconds", 2.0f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Порог «давления тика» относительно бюджета (Measured/Budget), выше — диагностика.
    /// </summary>
    public static readonly CVarDef<float> PgIncidentPressureThreshold =
        CVarDef.Create("pg.incident_pressure_threshold", 1.35f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Рост активных atmos-тайлов относительно baseline, выше — диагностика.
    /// </summary>
    public static readonly CVarDef<float> PgIncidentAtmosSpike =
        CVarDef.Create("pg.incident_atmos_spike", 1.6f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Рост awake physics-тел относительно baseline, выше — диагностика.
    /// </summary>
    public static readonly CVarDef<float> PgIncidentPhysicsSpike =
        CVarDef.Create("pg.incident_physics_spike", 1.6f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Радиус поиска игроков около очага (тайлы).
    /// </summary>
    public static readonly CVarDef<float> PgNearbyPlayerRange =
        CVarDef.Create("pg.nearby_player_range", 16f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Лимит сущностей в отчёте «самые нагружающие».
    /// </summary>
    public static readonly CVarDef<int> PgTopEntityLimit =
        CVarDef.Create("pg.top_entity_limit", 8, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Бюджет одной диагностики (мс). По исчерпании — ранний выход.
    /// </summary>
    public static readonly CVarDef<float> PgDiagnoseBudgetMs =
        CVarDef.Create("pg.diagnose_budget_ms", 3.0f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Интервал обновления UI, пока окно открыто (секунды).
    /// </summary>
    public static readonly CVarDef<float> PgUiRefreshSeconds =
        CVarDef.Create("pg.ui_refresh_seconds", 2.0f, CVar.REPLICATED | CVar.ARCHIVE);
}
