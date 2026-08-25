# Achievement Trigger Audit

**Дата:** 2026-08-23  
**Ветка:** `feature/ss13-achievements-port`  
**Полный inventory (JSON):** [AchievementsTriggerAudit.json](./AchievementsTriggerAudit.json)

---

## Executive Summary (final — 2026-08-23)

| Метрика | Число |
|---------|------:|
| **Всего достижений** | **215** |
| **fully_specific** | **172** |
| **generic_but_valid** | **43** |
| **generic_suspicious** | **0** |
| **blocked** | **0** |
| **manual** | **0** |
| **Removed (impossible/fake)** | **279** |

### Final cleanup pass

- Удалены все `blocked` / `manual` записи из каталога (не оставлены stubs)
- Удалены `objective-complete` с placeholder `Objectives` / `*` (нет Fish objective mapping)
- Seed: `FishAchFirstBreath` → `first-late-join`, `FishAchBananaRequiem` → `slip-death`
- `Tools/finalize_achievements_catalog.py` — безопасное удаление блоков + FTL/audit/mapping
- `Tools/refresh_audit_status.py` — CRLF-safe парсинг YAML

---

## Executive Summary (refine pass — 2026-08-23)

| Метрика | Число |
|---------|------:|
| **Всего достижений** | **494** |
| **fully_specific** | **264** |
| **generic_but_valid** | **27** |
| **generic_suspicious** | **0** |
| **blocked** (нет SS14-механики / admin-only) | **199** |
| **manual** (admin grant) | **6** |
| **С conditionParams** | **224** |

### Refine pass #1 (interaction + handlers)

- `UserInteractHandEvent` передаёт `EntityPrototypeId` цели → фильтр `conditionParams.target`
- Новые handlers: `defibrillate`, `surgery`, `gun-shot`; kill передаёт `WeaponPrototypeId`
- `Tools/refine_fish_achievements.py` — deep ID/FTL mapping, coverage matrix в JSON
- **Остаётся:** ~173 interaction без target (blocked или нужен SS13 source lookup)

---

## Executive Summary (initial audit)

### Главный вывод

**96.4% каталога (476/494) — это YAML-заглушки без runtime trigger.**  
Они существуют как `condition: manual`, исключены из индекса `AchievementManager._byCondition` и unlockable только через `achgrant`.

**3.6% (18/494)** имеют реальную цепочку:

```
GAMEPLAY EVENT → AchievementConditionSystem → ContributeAsync → MatchesContext → progress/unlock → DB → UI
```

---

## Архитектура (что реально подключено)

### Condition keys в коде (18)

| Key | Gameplay Event | Handler | Prototypes |
|-----|----------------|---------|------------|
| `manual` | — | — (excluded from index) | 476 |
| `first-late-join` | `PlayerSpawnCompleteEvent` (LateJoin) | `OnPlayerSpawn` | 1 |
| `job-play` | `PlayerSpawnCompleteEvent` | `OnPlayerSpawn` | 1 |
| `round-end-alive` | `RoundEndMessageEvent` + alive | `OnRoundEnd` | 2 |
| `round-survive` | `RoundEndMessageEvent` + alive | `OnRoundEnd` | **0** |
| `counter` | `RoundEndMessageEvent` + CounterKey | `OnRoundEnd` | 1 |
| `antag-win` | `RoundEndMessageEvent` + antag | `OnRoundEnd` | **0** |
| `death` | `MobStateChangedEvent` → Dead | `OnMobStateChanged` | 1 |
| `slip-death` | `SlipEvent` + death | `OnSlip` / `OnMobStateChanged` | 1 |
| `kill` | `KillReportedEvent` | `OnKillReported` | 1 |
| `damage-dealt` | `DamageChangedEvent` ↑ | `OnDamageChanged` | 1 |
| `heal` | `DamageChangedEvent` ↓ | `OnDamageChanged` | 1 |
| `craft` | `ItemConstructionCreated` | `OnCrafted` | 1 |
| `item-pickup` | `DidEquipEvent` | `OnEquipped` | 1 |
| `interaction` | `UserInteractHandEvent` | `OnUserInteractHand` | 2 |
| `station-event` | `GameRuleStartedEvent` | `OnGameRuleStarted` | 1 |
| `shuttle-arrive` | `FTLCompletedEvent` (emergency) | `OnEmergencyShuttleArrived` | 2 |
| `explosion` | `SunriseExplosionEvent` | `OnExplosion` | 1 |

### Защита от фиктивных unlock

1. **`manual`** — не индексируется, handlers не вызывают `ContributeAsync(manual)`.
2. **`allowGenericTrigger: false` + пустые `conditionParams`** → `MatchesContext` возвращает `false`.
3. **`AchievementGameplayGateSystem`** — ghost, observer, visiting mind, admin arena, not-in-round.
4. **`EventKey` dedupe** — одно событие не даёт повторный progress.

---

## ✅ 18 достижений с реальным trigger

### Seed-set (5) — FULLY IMPLEMENTED

| ID | Condition | Target | Trigger chain |
|----|-----------|--------|---------------|
| `FishAchFirstBreath` | `first-late-join` | 1 | Late join spawn → `ContributeAsync(first-late-join)` → unlock |
| `FishAchStillStanding` | `round-end-alive` | 1 | Round end alive ≥180s → unlock |
| `FishAchBananaRequiem` | `slip-death` | 1 | Slip → death (secret) → unlock |
| `FishAchCentcommTourist` | `shuttle-arrive` | 1 | Emergency shuttle FTL → alive on grid → unlock |
| `FishAchHabitualSurvivor` | `counter` + `key: rounds-survived` | 10 | Round end alive → counter increment → unlock at 10 |

### Batch (13) — PARTIALLY IMPLEMENTED

Цепочка **работает**, но trigger **generic** (нет `conditionParams` для job/event/weapon/location):

| ID | Condition | Target | Проблема |
|----|-----------|--------|----------|
| `FishAch_Combat_CorridorsOfPain` | `kill` | 3 | Любые 3 PvP-kill, не конкретный сценарий |
| `FishAch_Misc_FirstShift` | `job-play` | 3 | Любой job spawn ×3, без фильтра profession |
| `FishAch_Misc_LabelMaker` | `craft` | 15 | Любой craft ×15 |
| `FishAch_Orig_ChemRoulette` | `round-end-alive` | 5 | Generic survive ×5 |
| `FishAch_Orig_BlobPerimeter` | `station-event` | 5 | **Любое** station event ×5, не blob-specific |
| `FishAch_Orig_BrigMedicBond` | `heal` | 10 | Любой heal ×10 |
| `FishAch_Orig_Honkvasion` | `shuttle-arrive` | 3 | Generic shuttle ×3 |
| `FishAch_Orig_CESingularityBabysil` | `explosion` | 3 | Любой взрыв в радиусе ×3 |
| `FishAch_Orig_LoadoutMax` | `item-pickup` | 25 | Любой equip ×25 |
| `FishAch_BrainDamage` | `damage-dealt` | 20 | Generic damage bucket |
| `FishAch_ClownCourt` | `interaction` | 30 | Любая hand-interaction ×30 |
| `FishAch_GhostTour` | `death` | 3 | Любая смерть ×3 |
| `FishAch_AtmosPoetry` | `interaction` | 40 | Любая hand-interaction ×40 |

---

## ❌ BROKEN / NO TRIGGER (476)

**Все** с `condition: manual`:

- Не индексируются в `_byCondition`
- Unlock **только** через `achgrant` / admin `TryForceUnlockAsync`
- Описание: `achievement-fish-catalog-pending-desc` (327+ catalog entries)
- Комментарии `# catalog-duplicate:` — дубликаты SS13-каталога

### По категориям (manual count)

| Категория | Manual | С trigger |
|-----------|-------:|----------:|
| Combat | ~60 | 1 (`kill`) |
| Survival | ~45 | 4 |
| Medical | ~40 | 1 (`heal`) |
| Engineering | ~55 | 0 |
| Science | ~40 | 0 |
| Exploration | ~35 | 0 |
| Items | ~50 | 1 (`item-pickup`) |
| Weapons | ~45 | 1 (`damage-dealt`) |
| Roles | ~55 | 1 (`job-play`) |
| Antagonists | ~30 | 0 |
| Round | ~25 | 2 (`round-end-alive`) |
| Interaction | ~20 | 2 (`interaction`) |
| Funny/Misc/Station | ~26 | 6 |

*Точные counts по категориям — в JSON inventory.*

### Что нужно для каждого manual achievement

1. Определить **существующее SS14/Sunrise event** (не invent новое без нужды)
2. Добавить handler в `AchievementConditionSystem` **или** parametric filter на существующий key
3. Сменить `condition: manual` → реальный key + `conditionParams`
4. Написать нормальное description (не `catalog-pending`)
5. Добавить regression test для критичных семейств

---

## Handlers без прототипов (infrastructure ready)

| Condition | Event | Status |
|-----------|-------|--------|
| `round-survive` | Round end alive | Handler fires, **zero prototypes** |
| `antag-win` | Round end + antag mind | Handler fires, **zero prototypes** |

---

## Неиспользуемые conditionParams (код есть, YAML нет)

| Param | Назначение | Пример использования |
|-------|------------|---------------------|
| `job` | Фильтр profession | «Сыграй 3 смены инженером» |
| `event` | Фильтр GameRule id | «Переживи 5 blob events» |
| `shuttle: emergency` | Только emergency shuttle | Уже implicit в `shuttle-arrive` handler |
| `key` | Counter family | Используется в `FishAchHabitualSurvivor` |

---

## Category coverage vs SS14 events

| Domain | Events available in code | Achievements wired |
|--------|-------------------------|-------------------|
| Combat (kill/damage/death) | ✅ KillReported, DamageChanged, MobState | 3 |
| Survival (round/latejoin/shuttle) | ✅ RoundEnd, Spawn, FTL | 6 |
| Medical (heal/revive/surgery) | ⚠️ heal only | 1 |
| Engineering (build/power/atmos) | ❌ no handlers | 0 |
| Science (research/craft) | ⚠️ craft only | 1 |
| Exploration (location/area) | ❌ no handlers | 0 |
| Items (pickup/use/break) | ⚠️ equip only | 1 |
| Weapons (shot/reload/specific) | ❌ no weapon-specific | 0 |
| Roles (job/tasks) | ⚠️ generic job-play | 1 |
| Antagonists (antag/objectives) | ⚠️ antag-win handler, no proto | 0 |
| Round (events/end) | ⚠️ station-event generic | 2 |
| Interaction | ⚠️ generic hand interact | 2 |

---

## Исправления в этом аудите

1. **UI:** manual stubs скрыты во **всех** вкладках (не только «Все»), пока нет progress/unlock
2. **Docs:** этот audit + обновление `Achievements.md`
3. **Tests:** `AchievementTriggerAuditTests` (integration) — regression на counts и unlock path
4. **JSON inventory:** machine-readable полный список 494 entries

---

## Рекомендации (не в scope этого PR)

1. **Не публиковать 476 manual** в UI до реализации trigger (сейчас скрыты)
2. **Приоритет wiring:** combat, role+job param, antag-win, medical revive
3. **Один handler — много achievements** через `conditionParams`, не дублировать systems
4. **Blocked list** вести в JSON со ссылкой на нужный SS14 event

---

## Точные итоговые числа

| | |
|---|---:|
| Всего достижений | **494** |
| Реально работают (gameplay unlock возможен) | **18** |
| Definitions без trigger | **476** |
| Trigger без progress | **0** |
| Broken unlock path | **0** |
| Исправлено в аудите | **3** (UI hide, docs, tests) |
| Заблокировано (нужна реализация trigger) | **476** |
