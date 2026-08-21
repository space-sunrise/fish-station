# Аудит источников достижений SS13

Числа в этом документе относятся к реально найденным определениям и не являются числом уже перенесённых достижений. До дедупликации одинаковые достижения разных форков учитываются отдельно.

## Текущие версии

### TGStation

- Repository: `tgstation/tgstation`
- Revision: `c40e5f1f6e8247937e91ad9469d6552a3db0a9ae`
- Binary achievements в актуальных definition-файлах: 95
- Файлы:
  - `boss_achievements.dm`: 22
  - `job_achievements.dm`: 9
  - `mafia_achievements.dm`: 19
  - `misc_achievements.dm`: 43
  - `skill_achievements.dm`: 2
- Отдельно исследуются score/progress datums и удалённые определения.

### BeeStation

- Repository: `BeeStation/BeeStation-Hornet`
- Revision: `091c43624121c87032d613099eacec17c7ad90d9`
- Binary achievements в актуальных definition-файлах: 43
- Файлы:
  - `boss_achievements.dm`: 16
  - `misc_achievements.dm`: 27
- Значительная часть унаследована от TG и будет дедуплицирована.
- Reward-поля BeeCoin не переносятся.

### Yogstation

- Repository: `yogstation13/Yogstation`
- Revision: `ae2886be95c234202212a3090ee7637173ea2c1d`
- Актуальных achievement datums: 51
- Исторически найдено typepaths: 64
- Удалённых из текущей версии typepaths: 13
- История definition-файла проверена по 212 commits.
- Поддерживаются скрытые достижения; публичный viewer скрывает условие до получения.

### Goonstation

- Repository: `goonstation/goonstation`
- Revision: `12b98d7f0c5f2ea92e02c2b3b7db2869972d95d8`
- Wiki revision: `72754` от 2026-08-09
- Публичный каталог wiki:
  - несекретных: 94
  - секретных: 58
  - всего: 152
- Дополнительно в актуальном коде подтверждены отсутствующие в этой wiki revision:
  - `Space Bowl Full Time Showman`;
  - `Fame and Fartuna`;
  - `#1 Victory Royale`.
- Текущее подтверждённое минимальное количество: 155.
- Medal rewards, role weighting и cosmetic unlocks не переносятся.

### CM-SS13

- Repository: `cmss13-devs/cmss13`
- Revision: `7dc625ef9b6373e5de94a53f9dc0ae3cd235e2f8`
- Актуальных achievement datums: 11
- Каталог использует external API для persistence, но условия регистрируются server-side через mob signals.
- В SS14 внешний achievement API не требуется: используется существующая база Fish.

## Промежуточный итог

- Подтверждённых актуальных записей до дедупликации: не менее 355.
- Это число ещё не включает:
  - TG score/progress records;
  - исторические TG/Bee/Goon definitions;
  - уникальные Shiptest, NovaSector, Skyrat и Monkestation records;
  - результаты дедупликации;
  - решения по адаптации отсутствующих в Sunrise механик.

## Исторический аудит

Для каждой линии истории фиксируются:

- первый и последний commit с определением;
- переименования;
- изменения условия;
- удалённые typepaths;
- текущий или архивный статус;
- trigger callsites, если они доступны в публичном коде.

Изменение текста без изменения механики не создаёт новое достижение. Существенно отличающееся историческое условие сохраняется как отдельный кандидат только после ручной проверки.

## Источники, не давшие отдельного каталога

`ParadiseSS13/Paradise` проверен как крупный активный проект. В текущей публичной версии формальной account-wide achievement system не найдено; job objectives являются round-only контентом и не добавляются в каталог как достижения.
