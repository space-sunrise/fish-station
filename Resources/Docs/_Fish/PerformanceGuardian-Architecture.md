# Performance Guardian — Architecture

## Layout

```
Content.Shared/_Fish/PerformanceGuardian/   CVars, PgReport DTO, net events
Content.Server/_Fish/PerformanceGuardian/   idle monitor, diagnostics, collector, facade
Content.Client/_Fish/PerformanceGuardian/   одно окно + F7 launcher
```

Vanilla hook: `AdminMenuWindow.xaml(.cs)` — одна вкладка-launcher с `FIsh edit`.

## Pipeline

```
Idle (~2s):  gauges (entities, grids, awake, atmos O(grids), event counters)
                │
                ├─ threshold? ──► Diagnostics (budgeted, once)
                │                    ├─ primary source
                │                    ├─ hottest grid (atmos / awake)
                │                    ├─ top awake entities on grid
                │                    └─ nearby Actor via EntityLookup
                │
Manual button ──┘
                │
UI open only ──► PgReportRequest / Response (no work if closed)
```

## Why this shape

Администратору нужны 5 ответов за ~5 секунд. Всё остальное (risk score, heatmap, continuous player profiles, 12 tabs) удалено как шум.

## Extension

1. Новый источник нагрузки → `PgLoadSource` + ветка в `PickSource` / `BuildRecommendation`.
2. Новый gauge → `PgIdleMonitor.Sample` + поле в `PgReport`.
3. Не добавлять вкладки без явной пользы для обычного админа.
