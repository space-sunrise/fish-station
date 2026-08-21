# Каталог достижений (аудит)

Машиночитаемые снимки:

- `AchievementsCatalogRaw.csv` — все найденные определения до дедупликации;
- `AchievementsCatalogUnique.csv` — уникальные по нормализованному имени.

## Сводка (2026-08-21)

| Метрика | Значение |
| --- | --- |
| Найдено до дедупликации | 510 |
| Уникальных после name-dedup | 344 |
| Перенесено в SS14 | 0 |
| Адаптировано | 0 |
| Исключено | 0 |

### По источникам (raw)

| Источник | Записей |
| --- | --- |
| Goonstation (wiki Medals r72754) | 152 |
| TGStation (HEAD awards+scores) | 115 |
| Monkestation | 102 |
| Yogstation | 51 |
| Shiptest | 47 |
| BeeStation | 43 |

Monkestation/Shiptest/Bee в основном наследуют TG-семейство; после дедупликации уникальный вклад Goon ≈ 152, TG-семейство + Yog дают остальное.

### Что ещё не закрыто

1. Исторические удалённые TG/Bee typepaths (commits по `code/datums/achievements/*`).
2. CM-SS13 achievement datums (external API; definitions не лежат в одном файле — требуется точечный поиск `subtypesof(/datum/achievement)`).
3. Ручная дедупликация по смыслу условия (не только по имени).
4. Fish-оригинальные адаптации: не копировать названия/описания 1:1, сохранять дух условия.
5. Доведение качественного уникального набора до порядка ~500 без мусорных записей.

## Правила переноса

- Reward/BeeCoin/role weighting/cosmetics → **не переносим**.
- Secret → секретные в UI до unlock.
- Progress/score datums TG → отдельные progress-достижения только если порог осмысленен в SS14.
- Отсутствующая механика Sunrise → адаптация / эквивалент / исключение с записью в migration report.
