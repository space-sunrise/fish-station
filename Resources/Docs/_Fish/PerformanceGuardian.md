# Performance Guardian

In-game tick-budget guardian for Fish Station. Event-driven collectors feed rolling aggregates and a CPU-budgeted analyzer. Admins with **Debug** open a lazy multi-tab window; F7 hosts only a launcher tab.

## How to open

1. F7 Admin Menu → **Perf Guardian** tab → **Open Performance Guardian**
2. Console (Debug): `perfguardian` or `pg`

The client subscribes only while the window is open. Closing unsubscribes. Tabs refresh only when selected.

## Adaptive load

| Level | Condition (tick vs budget) | Behavior |
|-------|----------------------------|----------|
| Full | OK | All collectors + analyzer |
| Reduced | mild overrun | Lower analyzer frequency, drop secondary collectors |
| Degraded | heavy overrun | Essential sampler gauges only |
| Critical | severe | Black-box append + essential gauges; analyzer off |

Load is derived from the sampler — the guardian must not amplify lag.

## CVars (`FishCCVars`)

| CVar | Default | Notes |
|------|---------|-------|
| `pg.enabled` | true | Master switch |
| `pg.sample_interval_seconds` | 1 | Cheap sample cadence |
| `pg.analyze_interval_seconds` | 2 | Analyzer cadence at Full |
| `pg.cpu_budget_ms` | 2 | Hard stopwatch budget per analyze pass |
| `pg.load_*_threshold` | 1.15 / 1.4 / 1.8 | Adaptive thresholds |
| `pg.max_players_tracked` | 128 | Profile slots |
| `pg.black_box_size` | 120 | Ring capacity |
| `pg.max_reports` / `pg.max_alerts` | 32 / 64 | Caps |
| `pg.ui_refresh_seconds` | 1.5 | Client request interval (replicated) |

## Permissions

- Window / subscribe / snapshot / commands: `AdminFlags.Debug`
- Alert push: active admins with `Admin` or `Debug`

## Non-goals

- Not anti-cheat; not an admin-log replacement
- No RobustToolbox / engine edits
- No per-frame entity scans; no MoveEvent collectors
- Top systems = event-rate heat, not EntitySystemManager profiling

See [PerformanceGuardian-Architecture.md](./PerformanceGuardian-Architecture.md) for layers and extension points.
