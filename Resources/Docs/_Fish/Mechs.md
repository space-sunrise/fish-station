# Мехи Fish Station

Документация для разработчиков контента Fish Station / SS14.

Fish Mech — **расширение существующего стека Mech** (Wizden + Sunrise UI/модули), а не отдельный vehicle/mecha-движок.

Связанные материалы:

- архитектура: `Resources/Docs/_Fish/Mechs-Architecture.md`
- чеклист фич: `Resources/Docs/_Fish/Mechs-Audit.md`
- прототипы: `Resources/Prototypes/_Fish/Mechs/`
- код: `Content.Shared|Server|Client/_Fish/Mechs/`
- локали: `Resources/Locale/{en-US,ru-RU}/_fish/mechs.ftl`
- соседняя специализация: `Resources/Docs/_Fish/BattleShuttles.md`

---

# Общая архитектура

## Устройство

```
BaseMech
 └─ FishMechCore          # общие Fish-компоненты ядра
     └─ конкретные шасси (Ripley / Gygax / Durand / … / Odysseus)
```

Пилот, батарея, BUI, actions, воздух кабины, EMP, урон, оружие, equipment container — **Mech**.

Собственный слой `_Fish/Mechs` отвечает за:

- внутренние повреждения и ремонт;
- направленную броню / deflect;
- DNA-замок и техобслуживание;
- dual-hand (primary/secondary) поверх Sunrise radial;
- шасси-способности (overload, defence, thrusters, smoke, strafe, zoom, phase);
- кабинный воздух (toggle баллона), радио, трекинг, bay, wreckage;
- медицинский Odysseus и medical equipment.

## Компоненты (кратко)

| Компонент | Назначение |
| --- | --- |
| `MechInternalDamage` | Флаги внутренних повреждений + пороги |
| `MechFacingArmor` | Front/Side/Back множители + deflect |
| `MechDnaLock` | Импринт ДНК пилота |
| `MechMaintenance` | Locked → Bolts → Hatch → Cell |
| `MechDualEquipment` | Secondary selected + swap |
| `MechOverload` / `MechDefenceMode` / `MechThrusters` / `MechSmoke` / `MechStrafe` / `MechZoom` / `MechPhasing` | Шасси-способности |
| `MechCabinAtmos` | Toggle внутреннего баллона поверх `MechAir` |
| `MechRadio` | Mic / speaker |
| `MechTrackingBeacon` / bay / wreckage | Станционная инфраструктура |

## Правила интеграции

1. Не дублировать Mech entry/exit, BUI, battery, equipment DoAfter.
2. Directed events (`AfterInteract`, `MechPilotReadyEvent`, …) не делить с BattleShuttle / vanilla `MechEquipmentSystem` без явного ordering.
3. Dual-hand: `MechComponent.CurrentSelectedEquipment` = primary; secondary в Fish-компоненте.
4. Шасси-способности выдаются пилоту при insert (как lights/eject).
5. Vanilla/Sunrise YAML — только тонкие `# Fish edit` hooks; основной код в `_Fish`.

## Content vs Engine

Вся логика в Content. RobustToolbox не трогаем.

## Производительность

- Без LINQ в hot path урона/движения.
- Ability Update только при активных флагах.
- Facing armor: один `DamageModify` handler.

## Нецели

- Полный паритет fab/construction graphs upstream.
- Medical beam без SS14-аналога.
- Atmos port-connector в модели DM.
- Параллельный mecha-стек рядом с Mech.
