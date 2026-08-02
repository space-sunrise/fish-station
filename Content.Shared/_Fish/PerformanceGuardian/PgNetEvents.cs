using Robust.Shared.Serialization;

namespace Content.Shared._Fish.PerformanceGuardian;

[Serializable, NetSerializable]
public sealed class PgSubscribeRequest : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class PgUnsubscribeRequest : EntityEventArgs;

/// <summary>
/// Лёгкий запрос текущего отчёта (без новой тяжёлой диагностики).
/// </summary>
[Serializable, NetSerializable]
public sealed class PgReportRequest : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class PgReportResponse : EntityEventArgs
{
    public PgReport Report;

    public PgReportResponse(PgReport report)
    {
        Report = report;
    }
}

/// <summary>
/// Ручной запуск полной диагностики (кнопка в UI).
/// </summary>
[Serializable, NetSerializable]
public sealed class PgDiagnoseRequest : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class PgOpenWindowHint : EntityEventArgs;
