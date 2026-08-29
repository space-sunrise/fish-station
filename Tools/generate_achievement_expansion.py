#!/usr/bin/env python3
"""Generate ~100 Fish-based achievements from wired condition handlers."""
from __future__ import annotations

import json
import textwrap
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACH_DIR = ROOT / "Resources/Prototypes/_Fish/Achievements"
LOCALE_EN = ROOT / "Resources/Locale/en-US/_fish/achievements.ftl"
LOCALE_RU = ROOT / "Resources/Locale/ru-RU/_fish/achievements.ftl"

# Each entry:
# suffix, category, condition, params, progress, min_round, secret, order,
# name_en, name_ru, desc_en, desc_ru, [secret_en, secret_ru],
# allow_generic=False, require_player_victim=True
CATALOG: list[tuple] = [
    # --- Fish / Sunrise roles (meaningful departments) ---
    ("JudgeOnBench", "FishAchRoles", "role-added", {"job": "CentcommSectoralJudge"}, 1, 0, False, 1100,
     "Bench Warmer", "Судья на месте",
     "Serve a shift as the sectoral judge.", "Отработать смену секторным судьёй."),
    ("SpecOpsDeploy", "FishAchRoles", "role-added", {"job": "SpecOpsOfficer"}, 1, 0, False, 1101,
     "Spec Ops Deployed", "Спецоперация",
     "Take the Spec Ops officer role during a round.", "Зайти за офицера спецопераций."),
    ("MagistratSeal", "FishAchRoles", "role-added", {"job": "Magistrat"}, 1, 0, False, 1102,
     "Magistrate Seal", "Печать магистрата",
     "Hold the magistrate post for a full round.", "Занять пост магистрата на смену."),
    ("BrigmedicOath", "FishAchRoles", "heal", {}, 15, 300, False, 1103,
     "Brigmedic Oath", "Клятва бригмедика",
     "Patch up fifteen crewmates — brig included.", "Вылечить пятнадцать членов экипажа.", None, None, True),
    ("RoboticistSpark", "FishAchRoles", "craft", {"item": "MachineFrame"}, 8, 300, False, 1104,
     "Frame Builder", "Сборщик каркасов",
     "Assemble eight machine frames from scratch.", "Собрать восемь каркасов машин."),
    ("PrisonerStreak", "FishAchRoles", "role-added", {"job": "PlanetPrisoner"}, 1, 0, False, 1105,
     "Planet Prisoner", "Планетный заключённый",
     "Spawn as a planet-prison inmate and survive the shift.", "Зайти за заключённого и пережить смену."),

    # --- Antagonists ---
    ("TraitorBriefing", "FishAchSecret", "antag-selected", {"antag": "Traitor"}, 1, 0, True, 1200,
     "Syndicate Briefing", "Брифинг Синдиката",
     "Receive a syndicate uplink assignment.", "Получить задание от Синдиката.",
     "Someone trusts you with terrible paperwork.", "Кому-то можно доверить ужасную бумажную работу."),
    ("ChangelingHunger", "FishAchSecret", "antag-selected", {"antag": "Changeling"}, 1, 0, True, 1201,
     "Genetic Appetite", "Генетический аппетит",
     "Get selected as a changeling.", "Быть выбранным генокрадом.",
     "Your DNA file has been… updated.", "Ваш геном… обновлён."),
    ("NukeOpsManifest", "FishAchCombat", "antag-selected", {"antag": "Nukeops"}, 1, 0, False, 1202,
     "War Ops Manifest", "Манифест ядерных оперативников",
     "Join the nuclear operatives team.", "Попасть в команду ядерных оперативников."),
    ("WizardHat", "FishAchFunny", "antag-selected", {"antag": "Wizard"}, 1, 0, False, 1203,
     "Pointy Hat", "Остроконечная шляпа",
     "Become the station's resident wizard.", "Стать станционным волшебником."),
    ("ThiefList", "FishAchMisc", "antag-selected", {"antag": "Thief"}, 1, 0, False, 1204,
     "Shopping List", "Список покупок",
     "Roll thief and receive your steal objectives.", "Получить цели вора."),
    ("FleshConvert", "FishAchSecret", "antag-selected", {"antag": "FleshCultist"}, 1, 0, True, 1205,
     "Soft Conversion", "Мягкая конверсия",
     "Join the flesh cult.", "Вступить в культ плоти.",
     "The heart beats for everyone eventually.", "Сердце бьётся для всех — рано или поздно."),
    ("AssaultDrop", "FishAchCombat", "antag-selected", {"antag": "AssaultOperative"}, 1, 0, False, 1206,
     "Assault Drop", "Десант штурма",
     "Deploy as an assault operative.", "Высадиться штурмовиком."),
    ("FugitiveRun", "FishAchSurvival", "antag-selected", {"antag": "Fugitive"}, 1, 0, False, 1207,
     "Most Wanted", "В розыске",
     "Spawn as the fugitive and stay alive.", "Выжить, будучи беглецом."),

    # --- Station events ---
    ("AbductorLights", "FishAchStation", "station-event", {"event": "AbductorsSpawn"}, 1, 0, False, 1300,
     "Abductor Lights", "Огни похитителей",
     "Be aboard when abductors arrive.", "Быть на станции при появлении похитителей."),
    ("BloodMoon", "FishAchStation", "station-event", {"event": "BloodCult"}, 1, 0, False, 1301,
     "Blood Moon", "Кровавая луна",
     "Witness the blood cult round start.", "Пережить начало раунда культа крови."),
    ("FleshOutbreak", "FishAchStation", "station-event", {"event": "FleshCult"}, 1, 0, False, 1302,
     "Flesh Outbreak", "Вспышка плоти",
     "Be present when the flesh cult rises.", "Присутствовать при восстании культа плоти."),
    ("AssaultAlarm", "FishAchStation", "station-event", {"event": "AssaultOps"}, 1, 0, False, 1303,
     "Assault Alarm", "Тревога штурма",
     "Survive the assault ops announcement.", "Пережить объявление штурмовой операции."),
    ("ObrRed", "FishAchStation", "station-event", {"event": "FishObrShuttleRed"}, 1, 0, False, 1304,
     "Red Response", "Красный ответ",
     "Be on-station when a red OBR shuttle is called.", "Быть на станции при красном вызове ОБР."),
    ("DragonRise", "FishAchStation", "station-event", {"event": "DragonSpawn"}, 1, 0, False, 1305,
     "Dragon Rise", "Пробуждение дракона",
     "Witness a space dragon spawn event.", "Стать свидетелем появления космического дракона."),
    ("ZombieHour", "FishAchSurvival", "station-event", {"event": "ZombieOutbreak"}, 1, 0, False, 1306,
     "Zombie Hour", "Час зомби",
     "Be alive when the zombie outbreak begins.", "Быть живым при начале zombie-вспышки."),
    ("KudzuCreep", "FishAchStation", "station-event", {"event": "KudzuGrowth"}, 1, 0, False, 1307,
     "Kudzu Creep", "Ползучий kudzu",
     "Watch kudzu claim part of the station.", "Увидеть, как kudzu захватывает станцию."),
    ("IonStorm", "FishAchStation", "station-event", {"event": "IonStorm"}, 1, 0, False, 1308,
     "Ion Storm", "Ионный шторм",
     "Endure an ion storm as silicon or crew.", "Пережить ионный шторм."),
    ("PlanetWar", "FishAchStation", "station-event", {"event": "PlanetWarRule"}, 1, 0, False, 1309,
     "Planet War", "Планетная война",
     "Join a Planet War gamemode round.", "Сыграть раунд «Планетная война»."),
    ("HamsterShift", "FishAchFunny", "station-event", {"event": "AnimalObjectives"}, 1, 0, False, 1310,
     "Hamster Shift", "Хомячья смена",
     "Play during the animal objectives event.", "Играть во время режима целей животных."),

    # --- Objectives (round end) ---
    ("TraitorEscape", "FishAchSurvival", "objective-complete", {"objective": "EscapeShuttleObjective"}, 1, 300, False, 1400,
     "Clean Getaway", "Чистый побег",
     "Complete a traitor escape shuttle objective.", "Выполнить цель побега на шаттле."),
    ("StealCaptainId", "FishAchMisc", "objective-complete", {"objective": "CaptainIDStealObjective"}, 1, 300, False, 1401,
     "ID Thief", "Вор ID",
     "Steal the captain's ID as a traitor.", "Украсть ID капитана."),
    ("StealIan", "FishAchFunny", "objective-complete", {"objective": "IanStealObjective"}, 1, 300, False, 1402,
     "Ian Heist", "Похищение Яна",
     "Complete the thief objective to steal Ian.", "Выполнить цель вора — украсть Яна."),
    ("LingSurvive", "FishAchSurvival", "objective-complete", {"objective": "ChangelingSurviveObjective"}, 1, 300, False, 1403,
     "Ling Survivor", "Выживший генокrad",
     "Survive the round as a changeling.", "Выжить раунд генокradом."),
    ("WizardLive", "FishAchSurvival", "objective-complete", {"objective": "WizardSurviveObjective"}, 1, 300, False, 1404,
     "Wizard Lives", "Волшебник жив",
     "Complete the wizard survive objective.", "Выполнить цель выживания волшебника."),
    ("FugitiveEscape", "FishAchSurvival", "objective-complete", {"objective": "FugitiveEscapeShuttleObjective"}, 1, 300, False, 1405,
     "Fugitive Escape", "Побег беглеца",
     "Escape on the shuttle as the fugitive.", "Сбежать на шаттле беглецом."),
    ("HamsterDonut", "FishAchFunny", "objective-complete", {"objective": "AnimalEatDonutObjective"}, 1, 60, False, 1406,
     "Donut Quest", "Квест пончика",
     "Complete the hamster donut objective.", "Выполнить цель «съесть пончик»."),

    # --- Playtime meta ---
    ("Veteran10h", "FishAchSurvival", "playtime-minutes", {"threshold": "600"}, 1, 0, False, 1500,
     "Ten Hour Veteran", "Ветеран 10 часов",
     "Accumulate ten hours of overall playtime.", "Набрать десять часов общего времени."),
    ("Veteran30h", "FishAchSurvival", "playtime-minutes", {"threshold": "1800"}, 1, 0, False, 1501,
     "Thirty Hour Veteran", "Ветеран 30 часов",
     "Accumulate thirty hours on the server.", "Набрать тридцать часов на сервере."),
    ("Veteran50h", "FishAchSurvival", "playtime-minutes", {"threshold": "3000"}, 1, 0, False, 1502,
     "Fifty Hour Veteran", "Ветеран 50 часов",
     "Spend fifty hours aboard Fish Station.", "Провести пятьдесят часов на Fish Station."),
    ("Veteran100h", "FishAchSurvival", "playtime-minutes", {"threshold": "6000"}, 1, 0, False, 1503,
     "Century Crew", "Экипаж столетия",
     "Reach one hundred hours of total playtime.", "Достичь ста часов общего времени."),

    # --- Combat: guns ---
    ("PythonShot", "FishAchCombat", "gun-shot", {"weapon": "WeaponRevolverPython"}, 12, 180, False, 1600,
     "Python Practice", "Тренировка с Python",
     "Fire a revolver python until it feels personal.", "Стрелять из Python, пока не станет личным."),
    ("BulldogBurst", "FishAchCombat", "gun-shot", {"weapon": "WeaponShotgunBulldog"}, 10, 180, False, 1601,
     "Bulldog Burst", "Залп Bulldog",
     "Empty a bulldog shotgun across ten engagements.", "Использовать Bulldog в десяти перестрелках."),
    ("LaserTag", "FishAchCombat", "gun-shot", {"weapon": "WeaponLaserCarbine"}, 15, 180, False, 1602,
     "Laser Tag", "Лазертаг",
     "Fire a laser carbine in anger fifteen times.", "Применить лазерный кarabин пятнадцать раз."),
    ("MagnumMoment", "FishAchCombat", "gun-shot", {"weapon": "WeaponEnergyMagnum"}, 8, 180, False, 1603,
     "Magnum Moment", "Момент Magnum",
     "Use the energy magnum eight times.", "Использовать энергетический magnum восемь раз."),
    ("LecterLine", "FishAchCombat", "gun-shot", {"weapon": "WeaponRifleLecter"}, 20, 180, False, 1604,
     "Lecter Line", "Линия Lecter",
     "Put twenty rounds downrange with a Lecter.", "Выпустить двадцать очередей из Lecter."),
    ("DisablerDiscipline", "FishAchCombat", "gun-shot", {"weapon": "WeaponDisabler"}, 10, 120, False, 1605,
     "Disabler Discipline", "Дисциплина disabler",
     "Non-lethally engage ten times with a disabler.", "Применить disabler десять раз."),

    # --- Combat: melee kills ---
    ("EnergyBlade", "FishAchCombat", "kill", {"weapon": "EnergySword"}, 3, 300, False, 1610,
     "Energy Blade", "Энергоклинок",
     "Eliminate three foes with an energy sword.", "Устранить троих энергоклинком."),
    ("KitchenKnife", "FishAchCombat", "kill", {"weapon": "CombatKnife"}, 2, 300, False, 1611,
     "Kitchen Knife", "Кухонный нож",
     "Win two fights with a combat knife.", "Выиграть два боя боевым ножом."),
    ("AxeMurder", "FishAchCombat", "kill", {"weapon": "FireAxe"}, 2, 300, False, 1612,
     "Axe Murder", "Топорное дело",
     "Drop two targets with a fire axe.", "Уложить двоих пожарным топором."),

    # --- Combat: creature kills ---
    ("FleshPudge", "FishAchCombat", "kill", {"target": "MobFleshPudge"}, 3, 180, False, 1620,
     "Pudge Slayer", "Убийца pudge",
     "Destroy three flesh pudge blobs.", "Уничтожить три flesh pudge.", None, None, False, False),
    ("FleshWorm", "FishAchCombat", "kill", {"target": "MobFleshWorm"}, 5, 180, False, 1621,
     "Worm Wrangler", "Укротитель червей",
     "Kill five flesh worms.", "Убить пять flesh worm.", None, None, False, False),

    # --- Medical ---
    ("BrainSurgeon", "FishAchRoles", "surgery", {"target": "SurgeryStepExtractBrain"}, 1, 300, False, 1700,
     "Brain Surgeon", "Нейрохирург",
     "Extract a brain during surgery.", "Извлечь мозг во время операции."),
    ("HeartTransplant", "FishAchRoles", "surgery", {"target": "SurgeryStepInsertHeart"}, 1, 300, False, 1701,
     "Heart Transplant", "Пересадка сердца",
     "Insert a donor heart.", "Вживить донорское сердце."),
    ("HeartRemoval", "FishAchRoles", "surgery", {"target": "SurgeryStepRemoveHeart"}, 1, 300, False, 1702,
     "Heart Removal", "Удаление сердца",
     "Remove a patient's heart on the table.", "Удалить сердце пациента на столе."),
    ("Appendix", "FishAchFunny", "surgery", {"target": "SurgeryStepRemoveAppendix"}, 1, 180, False, 1703,
     "Appendix Story", "История аппendix",
     "Perform an appendix removal.", "Провести удаление аппendix."),
    ("CloseIncision", "FishAchRoles", "surgery", {"target": "SurgeryStepCloseIncision"}, 5, 180, False, 1704,
     "Stitch Count", "Счёт швов",
     "Close five surgical incisions.", "Закрыть пять хирургических разрезов."),
    ("DefibHero", "FishAchRoles", "defibrillate", {}, 8, 180, False, 1705,
     "Defib Hero", "Герой деfib",
     "Shock eight patients back from the brink.", "Вернуть восемь пациентов деfibом.", None, None, True),
    ("FieldMedic", "FishAchRoles", "heal", {}, 25, 300, False, 1706,
     "Field Medic", "Полевой медик",
     "Heal crewmates twenty-five times in one shift.", "Вылечить экипаж двадцать пять раз за смену.", None, None, True),

    # --- Chemistry / reagents ---
    ("UltimiumBuzz", "FishAchMisc", "reagent-metabolize", {"reagent": "Ultimium"}, 1, 180, False, 1800,
     "Ultimium Buzz", "Ultimium-тремор",
     "Metabolize ultimium gas in your bloodstream.", "Метаболизировать ultimium в крови."),
    ("ZenthiumHaze", "FishAchMisc", "reagent-metabolize", {"reagent": "Zenthium"}, 1, 180, False, 1801,
     "Zenthium Haze", "Zenthium-туман",
     "Survive zenthium in your veins.", "Пережить zenthium в венах."),
    ("MethRun", "FishAchFunny", "reagent-metabolize", {"reagent": "Desoxyephedrine"}, 1, 120, False, 1802,
     "Meth Run", "Мет-забег",
     "Metabolize desoxyephedrine.", "Метаболизировать desoxyephedrine."),
    ("OmnizineMiracle", "FishAchRoles", "reagent-metabolize", {"reagent": "Omnizine"}, 1, 120, False, 1803,
     "Omnizine Miracle", "Чудо omnizine",
     "Process omnizine in your body.", "Пропустить omnizine через организм."),
    ("BananaDine", "FishAchFunny", "reagent-metabolize", {"reagent": "Bananadine"}, 1, 60, False, 1804,
     "Bananadine High", "Bananadine-приход",
     "Metabolize bananadine.", "Метаболизировать bananadine."),
    ("UnholyWater", "FishAchSecret", "reagent-metabolize", {"reagent": "Unholywater"}, 1, 0, True, 1805,
     "Unholy Water", "Нечестивая вода",
     "Drink unholy water and live to tell.", "Выпить unholy water и выжить.",
     "Tastes like regret and candle wax.", "На вкус как сожаление и воск."),
    ("TearGasTears", "FishAchSurvival", "reagent-metabolize", {"reagent": "TearGas"}, 1, 120, False, 1806,
     "Tear Gas Tears", "Слёзы tear gas",
     "Metabolize tear gas — willingly or not.", "Метаболизировать tear gas — добровольно или нет."),
    ("HappinessOverdose", "FishAchFunny", "reagent-metabolize", {"reagent": "Happiness"}, 1, 60, False, 1807,
     "Happiness Overdose", "Передоз счастья",
     "Metabolize happiness reagent.", "Метаболизировать happiness."),

    # --- Engineering / station interactions ---
    ("SecVendor", "FishAchStation", "interaction", {"target": "VendingMachineSec"}, 1, 120, False, 1900,
     "Sec Vendor", "Sec-автомат",
     "Use the security equipment vendor.", "Воспользоваться sec-автоматом."),
    ("EvacConsole", "FishAchStation", "interaction", {"target": "ComputerEmergencyShuttle"}, 1, 180, False, 1901,
     "Evac Console", "Консоль эvac",
     "Access the emergency shuttle console.", "Получить доступ к консоли эvac-шаттла."),
    ("NukeInspect", "FishAchStation", "interaction", {"target": "NuclearBomb"}, 1, 180, False, 1902,
     "Nuke Inspect", "Осмотр бомбы",
     "Interact with the nuclear bomb.", "Взаимодействовать с ядерной бомбой."),
    ("ChemDispense", "FishAchStation", "interaction", {"target": "ChemDispenser"}, 1, 120, False, 1903,
     "Chem Dispense", "Хим-dispenser",
     "Use a chem dispenser.", "Использовать chem dispenser."),
    ("ObrConsole", "FishAchStation", "interaction", {"target": "FishComputerShuttleObr"}, 1, 180, False, 1904,
     "OBR Console", "Консоль ОБР",
     "Use the OBR shuttle console.", "Использовать консоль шаттла ОБР."),
    ("Autolathe", "FishAchStation", "interaction", {"target": "Autolathe"}, 1, 120, False, 1905,
     "Autolathe", "Autolathe",
     "Operate an autolathe.", "Поработать с autolathe."),
    ("CloneConsole", "FishAchStation", "interaction", {"target": "ComputerCloningConsole"}, 1, 180, False, 1906,
     "Clone Console", "Клон-консоль",
     "Access the cloning console.", "Получить доступ к cloning console."),
    ("SinguloGen", "FishAchStation", "interaction", {"target": "SingularityGenerator"}, 1, 300, False, 1907,
     "Singulo Gen", "Генератор сингулы",
     "Interact with the singularity generator.", "Взаимодействовать с генератором сингулярности."),
    ("SalvageConsole", "FishAchStation", "interaction", {"target": "SalvageExpeditionConsole"}, 1, 180, False, 1908,
     "Salvage Console", "Консоль salvage",
     "Plan a salvage expedition.", "Спланировать salvage-экспедицию."),
    ("IcarusTerminal", "FishAchStation", "interaction", {"target": "ComputerIcarus"}, 1, 180, False, 1909,
     "Icarus Terminal", "Терминал Icarus",
     "Touch the Icarus assault terminal.", "Дотронуться до терминала Icarus."),

    # --- Crafting ---
    ("SpearSmith", "FishAchMisc", "craft", {"item": "Spear"}, 5, 120, False, 2000,
     "Spear Smith", "Кузнец копий",
     "Craft five spears.", "Скрафтить пять копий."),
    ("RodRunner", "FishAchMisc", "craft", {"item": "MetalRod"}, 20, 120, False, 2001,
     "Rod Runner", "Прутковый мастер",
     "Craft twenty metal rods.", "Скрафтить двадцать металлических прутов."),
    ("ImprovBow", "FishAchMisc", "craft", {"item": "ImprovisedBow"}, 1, 120, False, 2002,
     "Improv Bow", "Самодельный лук",
     "Craft an improvised bow.", "Скрафтить самодельный лук."),
    ("ApcBuild", "FishAchStation", "craft", {"item": "APC"}, 3, 180, False, 2003,
     "APC Build", "Сборка APC",
     "Build three APC units.", "Собрать три APC."),
    ("ChairCraft", "FishAchMisc", "craft", {"item": "Chair"}, 10, 60, False, 2004,
     "Chair Craft", "Стулomania",
     "Craft ten chairs.", "Скрафтить десять стульев."),

    # --- Food / ingest ---
    ("CorgiBite", "FishAchFunny", "item-ingest", {"item": "FoodMeatCorgi"}, 1, 60, False, 2100,
     "Corgi Bite", "Укус corgi",
     "Eat corgi meat.", "Съесть мясо corgi."),
    ("BananaDiet", "FishAchFunny", "item-ingest", {"item": "FoodBanana"}, 5, 60, False, 2101,
     "Banana Diet", "Банановая диета",
     "Eat five bananas.", "Съесть пять бананов."),
    ("HellRamen", "FishAchFunny", "item-ingest", {"item": "DrinkHellRamen"}, 1, 60, False, 2102,
     "Hell Ramen", "Адский ramen",
     "Drink hell ramen.", "Выпить hell ramen."),
    ("MangoCroissant", "FishAchMisc", "item-ingest", {"item": "FoodBakedMangoCroissant"}, 1, 60, False, 2103,
     "Mango Croissant", "Манgo-круассан",
     "Try the Fish mango croissant.", "Попробовать Fish mango croissant."),

    # --- Emotes / social ---
    ("HonkArmy", "FishAchFunny", "emote", {"emote": "Honk"}, 20, 120, False, 2200,
     "Honk Army", "Армия honk",
     "Honk twenty times.", "Honkнуть двадцать раз."),
    ("SaluteCaptain", "FishAchMisc", "emote", {"emote": "Salute"}, 10, 60, False, 2201,
     "Salute", "Сalute",
     "Salute ten times.", "Salute десять раз."),
    ("FlipTable", "FishAchFunny", "emote", {"emote": "Flip"}, 5, 60, False, 2202,
     "Flip Out", "Переворот",
     "Perform five flip emotes.", "Сделать пять flip-emote."),
    ("FoxYip", "FishAchFunny", "emote", {"emote": "Fox"}, 10, 60, False, 2203,
     "Fox Yip", "Лисиный yip",
     "Use the kitsune fox emote ten times.", "Использовать fox-emote десять раз."),
    ("DanceFloor", "FishAchFunny", "emote", {"emote": "Dance"}, 8, 60, False, 2204,
     "Dance Floor", "Танцпол",
     "Dance eight times.", "Танцевать восемь раз."),

    # --- Pickups ---
    ("EnergySwordPickup", "FishAchCombat", "item-pickup", {"item": "EnergySword"}, 1, 120, False, 2300,
     "Energy Sword", "Энергомеч",
     "Equip an energy sword.", "Экипировать energy sword."),
    ("JudgeStamp", "FishAchRoles", "item-pickup", {"item": "RubberStampSectoralJudge"}, 1, 120, False, 2301,
     "Judge Stamp", "Печать судьи",
     "Equip the sectoral judge stamp.", "Взять печать секторного судьи."),
    ("KillTomePickup", "FishAchSecret", "item-pickup", {"item": "KillTome"}, 1, 0, True, 2302,
     "Kill Tome", "Kill Tome",
     "Hold the kill tome.", "Держать kill tome.",
     "Some books should stay closed.", "Некоторые книги лучше не открывать."),

    # --- Exploration ---
    ("MeteorWatch", "FishAchMisc", "examine", {"tag": "Meteor"}, 3, 180, False, 2400,
     "Meteor Watch", "Наблюдение метеоров",
     "Examine three meteors.", "Осмотреть три метеора."),
    ("KillTomeRead", "FishAchSecret", "examine", {"target": "KillTome"}, 1, 0, True, 2401,
     "Forbidden Read", "Запретное чтение",
     "Examine the kill tome.", "Осмотреть kill tome.",
     "Curiosity kills more than cats.", "Любопытство убивает не только кошек."),

    # --- Station disasters / misc progress ---
    ("BlastRadius", "FishAchSurvival", "explosion", {}, 5, 180, False, 2500,
     "Blast Radius", "Радиус взрыва",
     "Survive near five explosions.", "Пережить пять взрывов рядом.", None, None, True),
    ("FloorThief", "FishAchFunny", "tile-pry", {"tag": "IntactFloor"}, 12, 300, False, 2501,
     "Floor Thief", "Вор полов",
     "Pry twelve intact floor tiles.", "Оторвать двенадцать целых плиток пола."),
    ("LawyerBot", "FishAchMisc", "ai-law-changes", {}, 5, 300, False, 2502,
     "Lawyer Bot", "Бот-юрист",
     "Have your silicon laws changed five times.", "Пять раз сменить законы silicon.", None, None, True),

    # --- Survival tiers ---
    ("EvacVeteran", "FishAchSurvival", "shuttle-arrive", {}, 5, 300, False, 2600,
     "Evac Veteran", "Вeteran эvac",
     "Arrive on CentComm five times.", "Пять раз добраться до CentComm на шаттле."),
    ("RoundVeteran25", "FishAchSurvival", "round-end-alive", {}, 25, 300, False, 2601,
     "Quarter Century", "Четверть сотни",
     "Survive twenty-five rounds to the end.", "Выжить до конца двадцать пять раундов."),
    ("OldTimer", "FishAchSurvival", "counter", {"key": "rounds-survived"}, 50, 300, False, 2602,
     "Old Timer", "Старый таймер",
     "Survive fifty counted rounds.", "Пережить пятьдесят учтённых раундов."),

    # --- Secrets / rare ---
    ("MindTheGap", "FishAchSecret", "chasm-fall", {}, 1, 0, True, 2700,
     "Mind the Gap", "Осторожно, пропасть",
     "Fall into a chasm.", "Упасть в пропасть.",
     "The station has a basement you weren't meant to see.", "У станции есть подвал, который вам не показывали."),
    ("Spaghettified", "FishAchSecret", "singularity-consumed", {}, 1, 0, True, 2701,
     "Spaghettified", "Спaghettification",
     "Get consumed by a singularity.", "Быть поглощённым сингулярностью.",
     "Physics finally noticed you.", "Физика наконец заметила вас."),
    ("GibbedOut", "FishAchSecret", "gibbed", {}, 1, 0, True, 2702,
     "Gibbed Out", "В кусочки",
     "Be gibbed.", "Быть разобранным на gibs.",
     "Recycling is mandatory.", "Переработка обязательна."),
]


def loc_key(ach_id: str, suffix: str) -> str:
    slug = ach_id.replace("FishAchExp_", "").lower()
    return f"achievement-fishexp-{slug}-{suffix}"


def yaml_entry(entry: tuple) -> str:
    allow_generic = False
    require_player_victim = True
    if len(entry) >= 15:
        allow_generic = bool(entry[14])
    if len(entry) >= 16:
        require_player_victim = bool(entry[15])

    (suffix, cat, cond, params, prog, min_r, secret, order,
     _, _, _, _, *sec) = entry[:13] if len(entry) >= 13 else entry
    ach_id = f"FishAchExp_{suffix}"
    lines = [
        "- type: achievement",
        f"  id: {ach_id}",
        f"  name: {loc_key(ach_id, 'name')}",
        f"  description: {loc_key(ach_id, 'desc')}",
        f"  category: {cat}",
        f"  condition: {cond}",
    ]
    if params:
        lines.append("  conditionParams:")
        for k, v in params.items():
            lines.append(f"    {k}: {v}")
    if allow_generic:
        lines.append("  allowGenericTrigger: true")
    if not require_player_victim:
        lines.append("  requirePlayerVictim: false")
    if prog != 1:
        lines.append(f"  progressTarget: {prog}")
    if min_r:
        lines.append(f"  minRoundSeconds: {min_r}")
    if secret:
        lines.append("  secret: true")
        if len(entry) >= 14 and entry[12]:
            lines.append(f"  secretDescription: {loc_key(ach_id, 'secret')}")
    lines.append("  oncePerRound: true")
    lines.append(f"  order: {order}")
    lines.append("  # source: Fish-expansion")
    return "\n".join(lines)


def split_batches(entries: list[tuple]) -> dict[str, list[tuple]]:
    n = len(entries)
    chunk = (n + 3) // 4
    return {
        "batch05_expansion_roles_events.yml": entries[0:chunk],
        "batch06_expansion_combat_medical.yml": entries[chunk:chunk * 2],
        "batch07_expansion_station_craft.yml": entries[chunk * 2:chunk * 3],
        "batch08_expansion_misc_secrets.yml": entries[chunk * 3:],
    }


def ftl_block(entry: tuple, lang: str) -> str:
    ach_id = f"FishAchExp_{entry[0]}"
    name_idx = 8 if lang == "en" else 9
    desc_idx = 10 if lang == "en" else 11
    lines = [
        f"{loc_key(ach_id, 'name')} = {entry[name_idx]}",
        f"{loc_key(ach_id, 'desc')} = {entry[desc_idx]}",
    ]
    if entry[6] and len(entry) > 12 and entry[12]:
        sec_idx = 12 if lang == "en" else 13
        lines.append(f"{loc_key(ach_id, 'secret')} = {entry[sec_idx]}")
    return "\n".join(lines)


def main() -> None:
    assert len(CATALOG) == 100, f"Expected 100 entries, got {len(CATALOG)}"

    batches = split_batches(CATALOG)
    for fname, items in batches.items():
        body = "\n".join(yaml_entry(e) for e in items) + "\n"
        (ACH_DIR / fname).write_text(body, encoding="utf-8")
        print(f"Wrote {fname}: {len(items)} achievements")

    en_extra = ["\n# expansion batch 05-08\n"]
    ru_extra = ["\n# expansion batch 05-08\n"]
    for entry in CATALOG:
        en_extra.append(ftl_block(entry, "en"))
        ru_extra.append(ftl_block(entry, "ru"))

    for path, extra in ((LOCALE_EN, en_extra), (LOCALE_RU, ru_extra)):
        text = path.read_text(encoding="utf-8").rstrip() + "\n" + "\n".join(extra) + "\n"
        path.write_text(text, encoding="utf-8")

    # Fix broken batch04 blob achievement
    batch04 = ACH_DIR / "batch04_fish_original.yml"
    text = batch04.read_text(encoding="utf-8")
    text = text.replace("event: BlobGameMode", "event: KudzuGrowth")
    batch04.write_text(text, encoding="utf-8")

    print(f"Total new achievements: {len(CATALOG)}")


if __name__ == "__main__":
    main()
