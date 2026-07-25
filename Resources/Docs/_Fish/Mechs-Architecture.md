# Архитектурное решение: Fish Mech как расширение Mech

Дата: 2026-07-26  
Ветка: `feature/space-battle-shuttle`  
PR: https://github.com/space-sunrise/fish-station/pull/319

## Требования (только поведение)

1. Меха — пилотируемая машина с батареей, модулями, BUI, уроном, светом, воздухом.
2. Внутренние повреждения с эффектами и путями ремонта.
3. Броня зависит от направления удара; возможен deflect.
4. Два «ручных» слота оборудования (primary/secondary) при сохранении Sunrise radial.
5. Шасси-способности: overload, defence, thrusters, smoke, strafe, zoom, phase.
6. DNA lock и maintenance gate (болты/люк/ячейка) блокируют вход/движение/equip.
7. Медицинский Odysseus + medical modules.
8. Кабина: toggle воздуха, радио; станция: bay, tracking, wreckage.
9. Совместимость с Battle Shuttle (оба — Mech specialization; без коллизий directed events).

## Решение

**Не** создавать параллельный vehicle/mecha стек.  
**Не** копировать чужие class/UI ID и snowflake-протоколы.  
**Да** — data-driven Content-слой `_Fish/Mechs`, тонкие hooks в vanilla/Sunrise.

### Переиспользуем

Mech entry/exit, equipment, battery, BUI, actions, guns, grabber, soundboard, Sunrise select/paint/EMP/armor clothing, Battle Shuttle lock/hatch (отдельная специализация).

### Собственный код (`_Fish/Mechs`)

| Компонент | Роль |
| --- | --- |
| `MechInternalDamage` | Bitflags + пороги; control-lost / short-circuit |
| `MechFacingArmor` | Front/Side/Back множители + deflect |
| `MechDnaLock` | Импринт ДНК; gate entry |
| `MechMaintenance` | Locked→Bolts→Hatch→Cell; blocks move/equip |
| `MechOverload` | Скорость↑, drain↑, self-damage |
| `MechDefenceMode` | Якорь + deflect↑ |
| `MechThrusters` | Space push / MovementAlwaysTouching |
| `MechSmoke` | Заряды дыма + cooldown |
| `MechStrafe` | Сохранять facing; energy cost |
| `MechZoom` / `MechPhasing` | Zoom / phase + damtype cycle |
| `MechDualEquipment` | SecondarySelected + swap |
| `MechCabinAtmos` | Tank toggle поверх `MechAir` |
| `MechRadio` | Mic / speaker actions |
| Tracking / bay / wreckage | Станционная логистика |

Одно семейство Shared/Server systems; Client — UI fragments / action visuals.

### Правила интеграции

- Dual-hand: primary = `CurrentSelectedEquipment`; secondary в Fish-компоненте.
- Internal damage: подписка на урон меха; UI читает компонент.
- Facing armor: `DamageModifyEvent` + направление origin vs `Transform.LocalRotation`.
- Chassis abilities: actions при insert пилота.
- Maintenance/DNA: `MechEntryEvent` / `UpdateCanMoveEvent` / equipment insert.
- Install gate: отдельный `MechEquipmentInstallGate` — без двойной подписки на `MechEquipment`+`AfterInteract`.
- Прототипы: `_Fish` parents + `# Fish edit` на конкретных шасси.

### Content vs Engine

Только Content. RobustToolbox не трогаем.

### Статус

Ядро, шасси-способности, cabin/radio/zoom/phase/medical/bay/tracking/wreckage — **готово**.  
CI / интеграционные тесты — `FishMechSystemsTest`.

### Производительность

- Без LINQ в hot path урона/движения.
- Ability Update только при активных флагах.
- Facing armor: один `DamageModify` handler.
