# Performance Guardian — Architecture

## Layout

```
Content.Shared/_Fish/PerformanceGuardian/   CVars, ring buffer, DTOs, net events
Content.Server/_Fish/PerformanceGuardian/   sampler, collector, aggregator, analyzer, net gate
Content.Client/_Fish/PerformanceGuardian/   window, lazy tabs, UIController, F7 launcher
Resources/Locale/{en-US,ru-RU}/_fish/performance-guardian.ftl
```

Vanilla hook (minimal):

- `Content.Client/Administration/UI/AdminMenuWindow.xaml(.cs)` — one launcher tab with `FIsh edit` markers

## Pipeline

```
Gameplay events ──O(1)──► PgCollectorSystem ──► PgCounterBag / PgPlayerProfiles
                                                      │
Periodic Update ──► PgServerSampler ──► AdaptiveLoad ──► PgAggregator
                                                      │
                              CPU-budgeted ◄──────────┘
                              PgAnalyzer ──► alerts / timeline / reports / heat
                                      │
                                      └──► PgBlackBox (freeze on Critical)
```

UI path: subscribe while open → `PgSnapshotRequest` (section) → `PgSnapshotResponse`. Rare `PgAlertPush` to eligible admins.

## Sampling (no engine patch)

| Gauge | Source |
|-------|--------|
| Tick budget | `IGameTiming.TickPeriod` |
| Tick pressure proxy | sampler stopwatch + awake/atmos heuristic |
| Entities / grids | `IEntityManager.EntityCount` / `Count<MapGridComponent>()` |
| Atmos | `GridAtmosphereComponent` ActiveTiles/Hotspots/ExcitedGroups (O(grids)) |
| Physics | `SharedPhysicsSystem.AwakeBodies.Count` |
| Memory | `GC.GetTotalMemory` / `CollectionCount` |
| Players | `ISharedPlayerManager.PlayerCount` |
| Event heat | collector category counters |

## Collectors

Essential (Full/Reduced): melee hit, projectile hit, explosion, FTL, collision proxy via attack/projectile.

Secondary (Full/Reduced only): throw, damage increased, dock, construction start net messages.

Handlers only increment counters / profile slots — no analysis.

## Analyzer CPU budget

`Stopwatch` around phased work (risk → anomaly → correlation → heat → tops). If elapsed ≥ `pg.cpu_budget_ms`, exit and resume from cursor next cycle.

## Extension points

1. New metric category: add `PgMetricCategory`, increment in collector, heat map picks it up.
2. New gauge: extend `PgSamplePoint` + sampler + snapshot formatters.
3. New tab: add `PgSnapshotSection`, window tab, section filter in `BuildSnapshot`.
4. Prefer existing shared/broadcast events; if a vanilla hook is unavoidable, one-line raise with `FIsh edit` markers.

## Perf decisions

- Custom `PgRingBuffer<T>` — fixed capacity, no LINQ on hot path
- Fixed player slots with recycle-by-lowest-risk
- Cap alerts/reports/timeline
- No work for UI when no subscribers / window closed
- Guardian never wraps the full gameplay tick
