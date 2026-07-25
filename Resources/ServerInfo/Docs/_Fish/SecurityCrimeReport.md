# Маска СБ: быстрый доклад о преступлении (Security Crime Report)

**Автор:** Pifagor/Cursor

**Ветка / PR:** `feature/sec-quick-crime-report` / [#313](https://github.com/space-sunrise/fish-station/pull/313)  
**Префикс кодовой базы:** `_Fish` / маркеры `Fish-edit` / `FIsh edit`  
**Дата оформления документации:** 2026-07-25

---

## 1. Зачем была создана система

На станции офицер СБ часто должен быстро сообщить коллегам о нарушении Space Law:

- открыть рацию;
- вспомнить код и название статьи;
- продиктовать место;
- не перепутать канал;
- сделать это под стрессом (погоня, бой, хаос в рации).

Ручной доклад медленный и подвержен ошибкам. Цель системы — дать **одно действие на маске СБ**: выбрать статью из радиального меню → автоматически уйти сообщение с кодом, названием и локацией в канал Security (и при необходимости в спецканалы подразделения).

Система не заменяет полноценный доклад по рации и не выносит судебное решение. Это **быстрый тактический сигнал** для координации СБ / спецподразделений.

---

## 2. Как система работает (поток)

### 2.1. Экипировка и появление Action

1. На сущности маски есть компонент `SecurityCrimeReport` и `AccessReader` с доступом `Security`.
2. При `MapInit` создаётся `InstantAction` из прототипа `ActionSecurityCrimeReport`, ссылка хранится в `ActionEntity`.
3. Пока маска надета в слот `mask` и у носителя есть доступ `Security`, Action попадает в Action Hotbar через `GetItemActionsEvent`.
4. Без доступа Action **не показывается**. При попытке обойти проверку (если событие всё же пришло) показывается стандартный popup `lock-comp-has-user-access-fail`.

### 2.2. Открытие меню

1. Игрок нажимает Action → событие `OpenSecurityCrimeReportEvent` (InstantAction).
2. Shared-система проверяет AccessReader и cooldown (`UseDelay` id `security-crime-report` на носителе).
3. При успехе клиент открывает `SimpleRadialMenu` **по центру экрана** (`OpenCentered()`, как Emotes).
4. Открытие меню **не** запускает cooldown: у Action нет `useDelay`; cooldown ставится только после успешной отправки доклада.

### 2.3. Выбор статьи

1. В меню — curated список `CorporateLawPrototype` (Fish Space Law), заданный в компоненте.
2. Для каждой статьи: код (`LawIdentifier`), локализованное название, иконка SecHUD + цвет фона по тяжести.
3. Выбор поднимает сетевое событие `SecurityCrimeReportSelectedEvent` (device + law id) на сервер.

### 2.4. Серверная валидация и отправка

Сервер:

1. Проверяет, что отправитель — живой attached entity.
2. Проверяет, что `device` — сущность с `SecurityCrimeReportComponent`.
3. Проверяет, что маска **сейчас** надета в слот `mask` у офицера (нельзя слать «со снятой» / чужой).
4. Проверяет, что выбранный law входит в `Articles` компонента.
5. Индексирует прототип закона, берёт код и title.
6. Определяет, нужен ли режим «подкрепление» (`RequiresReinforcement`).
7. Если активен EMP-malfunction (`MalfunctionUntil`) — гардлит название, **не** подставляет локацию.
8. Иначе берёт ближайший nav beacon через `NavMapSystem.GetNearestBeaconString`.
9. Если на маске есть `EmpDisabledComponent` — **полная отмена** отправки (радиоисточник = маска, EMP глушит как обычную электронику).
10. Повторно проверяет доступ и cooldown.
11. Собирает каналы: всегда `Security` + спецканалы с гарнитуры (см. ниже).
12. Шлёт в Messenger (plain) и Radio (markup, `escapeMarkup: false`).
13. Поднимает `ChatNotification` слушателям каналов (звук + UI).
14. Ставит cooldown 30 с на носителя и на Action.

### 2.5. Формат сообщения

**Радио (с markup):** префикс вроде `ДОЛОЖЕНИЕ СБ`, код, название, локация (если нет malfunction), при необходимости «Требуется подкрепление».

**Messenger / plain:** тот же смысл без markup (совместимость с TTS / текстовым чатом групп).

**Уведомления:**

- обычное — `SecurityCrimeReport` + `notice2.ogg`;
- срочное (подкрепление) — `SecurityCrimeReportUrgent` + `alert.ogg`.

---

## 3. Переиспользованные существующие системы

| Система / API | Зачем |
|---|---|
| `SharedActionsSystem` / InstantAction / Action Hotbar | Стандартный UX «действие с предмета» |
| `SimpleRadialMenu` | Выбор статьи без кастомного окна |
| `CorporateLawPrototype` / Fish Space Law (`_Sunrise.Laws`) | Источник статей, кодов и названий — без дублирования законов |
| `AccessReaderSystem` | Проверка доступа `Security` как у замков/ID |
| `UseDelaySystem` | Антиспам / cooldown на носителе (id `security-crime-report`) |
| `RadioSystem.SendRadioMessage` | Отправка в радиоканалы |
| `MessengerServerSystem` | Дублирование в группы мессенджера по radio channel |
| `NavMapSystem` | Автолокация по ближайшему beacon |
| `StationSystem` | Привязка к станции для messenger server |
| `InventorySystem` / `ClothingComponent` | Только надетая маска; слот `mask` / `ears` |
| `EncryptionKeyHolderComponent` / `WearingHeadsetComponent` | Каналы гарнитуры без хардкода ERT/CentCom |
| `EncryptionKeyStationMaster` | Эталон «станционных» каналов для фильтра спецканалов |
| `EmpPulseEvent` / `EmpDisabledComponent` | Помехи и полный блок при EMP |
| `ChatNotification` / `ChatNotificationEvent` | Заметные алерты слушателям |
| `ActiveRadioComponent` / `HeadsetComponent` | Кому слать notification |
| Locale FTL | RU/EN строки сообщений и Action |
| Стандартный popup access-fail | Единый UX отказа в доступе |

Архитектурный принцип: **не изобретать** отдельный чат, отдельный закон, отдельный UI-фреймворк. Встроить доклад в уже знакомые игроку и движку пути.

---

## 4. Почему такие архитектурные решения

### 4.1. Компонент на маске, а не на job/mind

Доклад — свойство **снаряжения**. Снял маску → нет Action. Передал маску другому с доступом Security → у того появляется Action. Это согласуется с паттерном item actions (`GetItemActions` + clothing slots).

### 4.2. Shared + Client + Server

- **Shared:** grant/revoke Action, access, EMP malfunction timer, cooldown, критерии «подкрепления».
- **Client:** только UI радиального меню и сетевой выбор.
- **Server:** авторитетная валидация, радио, messenger, notifications.

Клиент не может сам «отправить доклад» в обход сервера: сервер заново проверяет маску, статью, доступ, cooldown, EMP.

### 4.3. Curated список статей в компоненте

Не все статьи закона нужны в быстром меню. Список в `SecurityCrimeReportComponent.Articles` — осознанный shortlist с вики/частоты использования. Тяжесть для UI и «подкрепления» выводится из **кода статьи** (1xx–5xx), без второго поля severity в прототипе маски.

### 4.4. Cooldown на носителе, не только на Action

Если cooldown только на `ActionEntity` маски, его можно обойти:

- снять маску / надеть другую с тем же компонентом;
- передать маску и сразу снова надеть.

Поэтому:

1. `UseDelay` с id `security-crime-report` живёт на **носителе**;
2. после успешного доклада ставится и UseDelay, и визуальный cooldown Action;
3. при `GetItemActions` оставшийся UseDelay **синхронизируется** на Action новой/той же маски.

Открытие меню не стартует cooldown (у Action нет `useDelay`) — игрок может открыть меню, передумать, закрыть без штрафа.

### 4.5. Радиоисточник = маска

Раньше риск: слать от офицера как source и обходить EMP на маске. Сейчас `radioSource` = device (маска). Если маска под `EmpDisabled` — отправка целиком отменяется. После снятия EMP остаётся временный `MalfunctionUntil` (помехи без локации), пока не истечёт таймер.

### 4.6. Спецканалы без хардкода имён подразделений

Всегда шлётся `Security`. Дополнительно — каналы с гарнитуры офицера, которых **нет** в `EncryptionKeyStationMaster`. Так CentCom / ERT / DeathSquad / BlueShield и любые будущие спецключи подхватываются из прототипов ключей, а не из switch по строкам.

Обычная станционная гарнитура СБ → только Security.  
Гарнитура ОБР/ЦК с доп. ключами → Security + спецканал(ы).

Последовательные `SendRadioMessage` по каналам безопасны: защита `_messages` в RadioSystem — от реэнтрантности внутри одного send; к концу метода запись снимается, повтор с тем же текстом на другой канал проходит.

### 4.7. Заметность доклада

Обычный текст в рации тонет в болтовне. Поэтому:

- жирный цветной префикс «ДОЛОЖЕНИЕ СБ» / urgent-вариант;
- отдельный ChatNotification со звуком;
- для тяжёлых статей — пометка «Требуется подкрепление» + urgent-звук.

### 4.8. Fork-friendly правки

Новый код в `Content.*/_Fish/SecurityCrimeReport/`. В vanilla `masks.yml` — короткие `Fish-edit` блоки. Локали и прототипы Action/notifications — под `_Fish` / `_fish`.

---

## 5. Ограничения

1. **Не полный Space Law:** только curated статьи в компоненте.
2. **Нужен доступ Security** на ID/доступе носителя; без него Action скрыт.
3. **Нужна надетая маска** с компонентом; чужая/снятая не отправит.
4. **Cooldown 30 с** после успешного доклада; открытие меню не тратит его.
5. **EMP на маске (`EmpDisabled`):** отправка полностью блокируется, пока EMP-disabled активен.
6. **После EMP (MalfunctionUntil, по умолчанию 60 с):** можно слать, но без локации и с помехами в названии.
7. **Локация** зависит от nav beacons; без маяков — fallback `nav-beacon-pos-no-beacons`.
8. **Messenger** работает только если на станции есть messenger server; радио при этом всё равно уходит.
9. **Спецканалы** только если на офицере гарнитура с соответствующими encryption keys.
10. **Уничтожение telecom-серверов** влияет на обычные (не LongRange) каналы штатным RadioSystem; система доклада не обходит телеком отдельно.
11. **LongRange-устойчивость к EMP** для спецканалов **не реализована** (обсуждалась как возможное улучшение): при EmpDisabled на маске глушится весь доклад.
12. **Иконки radial** — SecHUD из `security_icons.rsi`; визуальный polish может потребовать доработки артистами.
13. **Нет отдельного антиспама чата сверх UseDelay + штатного RadioSystem** — основной ограничитель это cooldown носителя.
14. **Смерть / crit / снятие маски** во время открытого меню: сервер отвергнет отправку, если маска уже не на лице.

---

## 6. Взаимодействие с существующими системами проекта

```
[Маска: SecurityCrimeReport + AccessReader]
        │
        ├─ Actions / Hotbar ──► OpenSecurityCrimeReportEvent
        │                           │
        │                           ▼
        │                    Client SimpleRadialMenu
        │                           │
        │                           ▼
        │              SecurityCrimeReportSelectedEvent (net)
        │                           │
        ▼                           ▼
 AccessReader ◄────────── Shared authorize / UseDelay
        │
        ▼
 Server SecurityCrimeReportSystem
        ├─ Inventory (mask worn?)
        ├─ CorporateLaw prototypes (Fish)
        ├─ NavMap (location)
        ├─ EMP / MalfunctionUntil
        ├─ Collect channels (Security + headset − StationMaster)
        ├─ RadioSystem ──► каналы рации
        ├─ MessengerServerSystem ──► группы
        └─ ChatNotification ──► звук/алерт слушателям ActiveRadio
```

Связь с **Fish Corporate Law**: статьи — те же прототипы, что и в остальном юридическом контенте Fish; доклад не создаёт параллельный «закон».

Связь с **радио / гарнитурами**: доклад уважает штатные правила каналов, encryption keys, headset enabled channels для notifications.

Связь с **EMP**: использует общий EmpPulse → Disabled pipeline; дополнительно свой soft-malfunction таймер на компоненте.

---

## 7. Полный список изменений

### 7.1. Новая подсистема Security Crime Report

- Новый компонент `SecurityCrimeReportComponent` (action proto, ActionEntity, Articles, ReportCooldown 30s, MalfunctionDuration 60s, MalfunctionUntil).
- Shared-система: MapInit/Shutdown Action, GetItemActions + access hide, open authorize, EMP pulse → MalfunctionUntil, UseDelay cooldown, sync cooldown, `RequiresReinforcement`.
- Client-система: SimpleRadialMenu, SecHUD-иконки по коду статьи, сеть выбора.
- Server-система: валидация, локация, garble, каналы, radio+messenger, chat notifications, старт cooldown.
- События: `OpenSecurityCrimeReportEvent`, `SecurityCrimeReportSelectedEvent`.

### 7.2. Маски

На следующие прототипы добавлены `SecurityCrimeReport` + `AccessReader` `[["Security"]]`:

- `ClothingMaskGasSecurity` — базовая маска СБ;
- `ClothingMaskGasCentcom` — CentCom (спецканал с гарнитуры);
- `ClothingMaskGasERT` — ОБР; `ClothingMaskGasDeathSquad` наследует ERT.

### 7.3. Entity Action

- Прототип `ActionSecurityCrimeReport` (`InstantAction` → `OpenSecurityCrimeReportEvent`).
- Без `useDelay` в YAML (cooldown только после успешного доклада).
- Иконка — спрайт маски СБ.

### 7.4. Меню выбора статьи

- Radial с curated статьями: 101, 201, 301, 312, 302, 303, 304, 401, 502 (через LawFish* прототипы).
- Tooltip: код — название.
- Визуалы по тяжести / спецкейсам (312, 304, 401, 502 и 1xx–5xx).
- Центрирование меню.

### 7.5. Интеграция со Space Law

- Чтение `CorporateLawPrototype` / FishCorporateLaw.
- Код из `LawIdentifier`, название из `Title` (loc).
- Подкрепление: все 4xx/5xx; плюс 300, 301, 310, 312.

### 7.6. Отправка сообщений

- Радио: markup, `escapeMarkup: false`, источник — маска.
- Messenger: plain-строки в группы, сопоставленные radio channel.
- Префиксы обычный / backup / malfunction / malfunction-backup (RU + EN).

### 7.7. Местоположение

- `NavMapSystem.GetNearestBeaconString(officer, onlyName: true)` + снятие markup.
- Fallback без маяков.
- При malfunction локация не включается.

### 7.8. EMP-поведение

- `EmpPulseEvent`: Affected/Disabled, `MalfunctionUntil = now + MalfunctionDuration`.
- Пока `EmpDisabled` на маске — early return, ничего не уходит.
- Пока `MalfunctionUntil` и нет EmpDisabled — доклад с помехами, без локации.
- Garble: ~35% букв → спецсимволы.

### 7.9. Доступ

- AccessReader Security на масках.
- Hide Action без доступа.
- Popup при отказе.
- Повторная проверка на сервере при отправке.

### 7.10. Cooldown и защита от обхода

- 30 с после **успешной** отправки.
- UseDelay на носителе id `security-crime-report`.
- `SharedActionsSystem.SetCooldown` на Action.
- Sync при GetItemActions (вторая маска / re-equip).
- Открытие меню не жжёт cooldown.

### 7.11. Спецподразделения и доп. каналы

- Всегда Security.
- Плюс каналы гарнитуры вне StationMaster set.
- Работает для CentCom / ERT / DeathSquad / BlueShield-ключей и т.п. без хардкода имён.

### 7.12. Антиспам / двойная отправка

- Cooldown носителя.
- Серверная валидация статьи и маски.
- Последовательные каналы — намеренно один доклад на N каналов, не N разных докладов-спама в один канал.
- ChatNotification `nextDelay: 3`.

### 7.13. Локализация

- `Resources/Locale/ru-RU/_strings/_fish/security/security-crime-report.ftl`
- `Resources/Locale/en-US/_strings/_fish/security/security-crime-report.ftl`
- Имя/desc Action, строки меню, radio/plain/malfunction/backup, interference, chat notifications.

### 7.14. ChatNotification прототипы

- `SecurityCrimeReport`
- `SecurityCrimeReportUrgent`

### 7.15. Документация

- Этот файл: `Resources/ServerInfo/Docs/_Fish/SecurityCrimeReport.md`

---

## 8. Список файлов

### Созданы

- `Content.Client/_Fish/SecurityCrimeReport/SecurityCrimeReportSystem.cs`
- `Content.Server/_Fish/SecurityCrimeReport/SecurityCrimeReportSystem.cs`
- `Content.Shared/_Fish/SecurityCrimeReport/SecurityCrimeReportComponent.cs`
- `Content.Shared/_Fish/SecurityCrimeReport/SecurityCrimeReportEvents.cs`
- `Content.Shared/_Fish/SecurityCrimeReport/SharedSecurityCrimeReportSystem.cs`
- `Resources/Prototypes/_Fish/Actions/security_crime_report.yml`
- `Resources/Prototypes/_Fish/Chat/security_crime_report_notifications.yml`
- `Resources/Locale/en-US/_strings/_fish/security/security-crime-report.ftl`
- `Resources/Locale/ru-RU/_strings/_fish/security/security-crime-report.ftl`
- `Resources/ServerInfo/Docs/_Fish/SecurityCrimeReport.md`

### Изменены

- `Resources/Prototypes/Entities/Clothing/Masks/masks.yml`  
  (`ClothingMaskGasSecurity`, `ClothingMaskGasCentcom`, `ClothingMaskGasERT` + наследник DeathSquad)

### Удалены

- *(нет)*

---

## 9. Чек-лист тестирования

### 9.1. Базовый UX маски СБ

- [ ] Надеть `ClothingMaskGasSecurity` с ID Security → Action «доложить о преступлении» в Hotbar
- [ ] Снять маску → Action исчезает
- [ ] Надеть снова → Action появляется
- [ ] Иконка и описание Action корректны (RU/EN)
- [ ] Передать маску другому игроку с доступом Security → у него появляется Action
- [ ] Сменить одну маску СБ на другую → Action переносится корректно

### 9.2. Доступ

- [ ] Без доступа Security Action **не** виден
- [ ] Если всё же вызвать открытие без доступа — popup `lock-comp-has-user-access-fail`
- [ ] С доступом Security (офицер / голова / валидная карта) — меню открывается
- [ ] После потери доступа (снята карта / изменён доступ) Action пропадает при следующем обновлении item actions

### 9.3. Меню статей

- [ ] Нажатие Action открывает radial **по центру** экрана
- [ ] Видны все curated статьи (код + название в tooltip)
- [ ] Отображаются иконки SecHUD и цвета по тяжести
- [ ] Закрытие меню без выбора не шлёт сообщение
- [ ] Закрытие меню без выбора **не** включает cooldown
- [ ] Повторное открытие меню сразу после закрытия возможно (если нет активного cooldown после отправки)

### 9.4. Отправка и содержимое

- [ ] Выбор статьи → сообщение в канале Security
- [ ] В сообщении есть номер статьи (код)
- [ ] В сообщении есть название статьи
- [ ] В сообщении есть локация (комната / ближайший beacon)
- [ ] Префикс «ДОЛОЖЕНИЕ СБ» / «SEC REPORT» заметен (bold/color)
- [ ] Сообщение также появляется в messenger-группе Security (если сервер мессенджера жив)
- [ ] Повторный доклад той же статьи после cooldown работает
- [ ] Неверная/неиз списка статья с клиента отвергается сервером (не уходит в эфир)

### 9.5. Подкрепление

- [ ] Статья 4xx → пометка «Требуется подкрепление» + urgent notification/звук
- [ ] Статья 5xx → то же
- [ ] Статьи 300 / 301 / 310 / 312 → то же
- [ ] Обычные 1xx/2xx/прочие 3xx из меню без backup-пометки → обычный notification

### 9.6. Локация

- [ ] Рядом с beacon → осмысленное имя зоны
- [ ] Вдали от beacon / без beacon → fallback без краша
- [ ] Локация соответствует положению офицера, а не маски в рюкзаке (маска должна быть надета)

### 9.7. Cooldown

- [ ] После успешной отправки Action показывает ~30 с cooldown
- [ ] Пока cooldown активен, открытие меню / отправка блокируются
- [ ] Снять маску и надеть снова во время cooldown → обойти нельзя (sync UseDelay)
- [ ] Надеть вторую маску с SecurityCrimeReport во время cooldown → обойти нельзя
- [ ] Передать маску другому игроку: cooldown остаётся у **отправителя**, у получателя свой UseDelay (не общий глобальный)
- [ ] Неуспешная попытка (нет доступа) не запускает 30 с cooldown

### 9.8. EMP

- [ ] EMP по маске → компонент получает MalfunctionUntil (~60 с)
- [ ] Пока маска EmpDisabled — доклад **не** уходит ни в radio, ни в messenger
- [ ] После снятия EmpDisabled, но до конца MalfunctionUntil: доклад уходит **без локации**, название с помехами
- [ ] Backup-статьи в malfunction всё ещё показывают «подкрепление»
- [ ] После истечения MalfunctionUntil — снова нормальный доклад с локацией
- [ ] EMP не ломает возможность открыть меню (если authorize проходит); блок именно на send при EmpDisabled

### 9.9. Радио / telecom / гарнитуры

- [ ] Обычная гарнитура СБ → только канал Security
- [ ] Гарнитура ERT / CentCom / DeathSquad с доп. ключами → Security **и** спецканал(ы)
- [ ] Каналы вроде Common/Eng/Med с обычной станционной гарнитуры **не** дублируются (фильтр StationMaster)
- [ ] Выключенный на гарнитуре канал: radio может всё равно уйти штатно; notification учитывает EnabledChannels
- [ ] Без гарнитуры → только попытка Security (как минимум Security в collect set)
- [ ] Уничтожение/отключение telecom servers: поведение как у обычной рации (не LongRange) — зафиксировать фактический результат на карте
- [ ] Несколько каналов = одно логическое доложение на каждый целевой канал (не дубликаты антиспам-блока внутри одного send)

### 9.10. Уведомления слушателям

- [ ] Слушатель Security слышит/видит ChatNotification при обычном докладе
- [ ] При urgent — другой звук/текст
- [ ] Игрок без приёма Security не получает notification
- [ ] Несколько докладов подряд с разных офицеров — каждый получает уведомление (с учётом nextDelay прототипа)

### 9.11. Мультиплеер и гонки

- [ ] Два офицера одновременно докладывают разные статьи — оба сообщения доходят
- [ ] Два офицера одновременно одну статью — оба доклада ок (разные источники)
- [ ] Быстрый двойной клик по статье → не должно уйти два доклада до истечения cooldown (второй режется)
- [ ] Клиент шлёт SelectionEvent со снятой маской → сервер игнор
- [ ] Клиент шлёт SelectionEvent с чужой маской → сервер игнор

### 9.12. Спецмаски

- [ ] `ClothingMaskGasCentcom` + доступ Security → Action есть, доклад работает
- [ ] `ClothingMaskGasERT` → то же + спецканалы при наличии keys
- [ ] `ClothingMaskGasDeathSquad` (наследник ERT) → то же
- [ ] Маски без компонента (обычный gas mask) → Action нет

### 9.13. Локализация и клиент

- [ ] RU клиент: русские строки радио/меню/Action
- [ ] EN клиент: английские строки
- [ ] Нет runtime exceptions при открытии меню / отправке
- [ ] Нет IL verification ошибок на Content.Client / Content.Server

### 9.14. Регрессии

- [ ] Обычное использование рации СБ не сломано
- [ ] Messenger обычных сообщений не сломан
- [ ] Другие InstantAction на одежде работают
- [ ] AccessReader на других объектах не затронут
- [ ] YAMLLinter / прототипы Action и ChatNotification валидны

---

## 10. Информация для ревьюеров

1. Смотреть в первую очередь `_Fish/SecurityCrimeReport/*` и точечные `Fish-edit` в `masks.yml`.
2. Ключевые инварианты: access hide, cooldown на носителе + sync, radioSource = mask, каналы = Security ∪ (headset − StationMaster).
3. UI radial — клиент только; сервер авторитетен.
4. Иконки и звуки можно итерировать отдельно от логики.
5. Документ лежит рядом с Fish Guidebook-контентом: `Resources/ServerInfo/Docs/_Fish/` (markdown для разработки/ревью; не in-game Guidebook XML).

---

## 11. Краткая сводка возможностей

Офицер СБ (или спецподразделение с доступом Security и соответствующей маской) нажимает Action на маске → выбирает статью Fish Space Law в radial → сервер шлёт заметный доклад с кодом/названием/локацией в Security (+ спецканалы гарнитуры), дублирует в messenger и поднимает звуковое уведомление; тяжёлые статьи помечаются как требующие подкрепления; доступ, cooldown на носителе и EMP ограничивают злоупотребления.
