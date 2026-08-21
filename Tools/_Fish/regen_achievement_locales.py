# -*- coding: utf-8 -*-
"""Regenerate achievement locales: normal descs + secret riddles."""
from __future__ import annotations

import re
import time
from pathlib import Path

from deep_translator import GoogleTranslator

ROOT = Path(r"z:\Не трогать\fish-station-mane")
EN_PATH = ROOT / "Resources/Locale/en-US/_fish/achievements.ftl"
RU_PATH = ROOT / "Resources/Locale/ru-RU/_fish/achievements.ftl"

# Загадки до unlock: не спойлерят условие напрямую.
RIDDLES_EN: dict[str, str] = {
    "fish-banana-requiem": "Yellow. Soft. The floor suddenly becomes your enemy.",
    "fishach_10bux": "A tip for silence. Some secrets cost ten.",
    "fishach_100mdash": "When the clock stops, raise a glass.",
    "fishach_421": "Four. Two. One. The house always remembers.",
    "fishach_ahollyjollyspacemas": "Holiday cheer, delivered by steel and bruises.",
    "fishach_adjutantonline": "Good morning, Captain. Systems online.",
    "fishach_banned": "Not a ban from admins — a different kind of exile.",
    "fishach_bearhug": "Warm embrace. Cold outcome.",
    "fishach_bombiniismissing": "Someone small and loud is not where they should be.",
    "fishach_buttonpusher": "Curiosity has a big red habit.",
    "fishach_captainslog": "Stardate whatever. Make it official.",
    "fishach_chevalierdusorbet": "A frozen title for a frozen crusade.",
    "fishach_deepfreeze": "The cold doesn't negotiate.",
    "fishach_formatcomplete": "Wipe complete. Personality optional.",
    "fishach_goawaybatin": "Shoo. The vents are not a hotel.",
    "fishach_googone": "Now you see me. Now you don't.",
    "fishach_guerrierdugelato": "Another frozen knight for the dessert wars.",
    "fishach_helios": "Too close to the sun — again.",
    "fishach_hesdeadjim": "Medical bay poetry. Flatline edition.",
    "fishach_ijustcleanedthat": "Fresh floor. Fresh regret.",
    "fishach_ispy": "I spy with my little eye... something classified.",
    "fishach_icarus": "Wings of wax, station of steel.",
    "fishach_identitytheft": "Your face, someone else's smile.",
    "fishach_illuminated": "A lightbulb moment that burns.",
    "fishach_isitreallythattimeagain": "The calendar laughs. You don't.",
    "fishach_itsatrap": "Admiral Ackbar sends his regards.",
    "fishach_itsnoteasybeinggreen": "Kermit called. He wants his struggle back.",
    "fishach_itsamemario": "It's-a me... in space.",
    "fishach_leavenomanbehind": "Everyone goes home. Or else.",
    "fishach_lockblock": "Access denied is a lifestyle.",
    "fishach_masterofunlocking": "Keys, cards, and questionable ethics.",
    "fishach_mybolognahasafirstname": "Oscar. Mayer. You know the rest.",
    "fishach_nero": "The station burns. Someone fiddles.",
    "fishach_newtonscrew": "What goes up must... invent gravity jokes.",
    "fishach_ohdoctor": "Oh, doctor! No, not that doctor.",
    "fishach_olbuddyolpal": "Old friend. Older favor.",
    "fishach_oldenemy": "Familiar face. Unfamiliar side.",
    "fishach_onmyowneightspacelegs": "Eight legs, zero excuses.",
    "fishach_onearmedbandit": "Pull the lever. Trust the odds. Don't.",
    "fishach_originalsin": "The first mistake tastes like fruit.",
    "fishach_remembertowashbehindtheantennae": "Hygiene checklist for the chrome-skinned.",
    "fishach_rookiethief": "First steal. Soft gloves. Loud heart.",
    "fishach_sheesh": "Sheesh.",
    "fishach_shutupandjam": "Less talk. More noise.",
    "fishach_spaceham": "Not the radio kind.",
    "fishach_spaceshipoftheseus": "Replace every bolt. Is it still you?",
    "fishach_spamhaus": "Inbox zero is a myth.",
    "fishach_survivor": "Still here. Somehow.",
    "fishach_suspiciouscharacter": "Something about you sets off the sensors.",
    "fishach_thankyoubusdriver": "Please hold on. Next stop: anywhere.",
    "fishach_thatsnomoonthats": "That's no moon...",
    "fishach_theforceisstrongwiththis": "Wave your hand. Ignore the airlock.",
    "fishach_tinkerer": "One more wire. What could go wrong?",
    "fishach_trailofblood": "Follow the red breadcrumbs.",
    "fishach_tryjigglingthehandle": "Have you tried turning it off and on again?",
    "fishach_virtualascension": "Upload complete. Body optional.",
    "fishach_weirdscience": "Science! With jazz hands.",
    "fishach_wonk": "Wonk.",
    "fishach_wrathofthefather": "Dad's not mad. Dad's disappointed. Violently.",
    "fishach_arcanefailure": "The ritual almost worked. Almost.",
    "fishach_braindamage": "Thoughts optional. Consequences mandatory.",
    "fishach_breathofdeath": "Inhale. Regret. Exhale.",
    "fishach_burninhell": "Temperature rising. Attitude unchanged.",
    "fishach_catastrophe": "When it rains, it hull-breaches.",
    "fishach_embracethebird": "Hug the feathered omen.",
    "fishach_honorarynukie": "Red suit energy without the invitation.",
    "fishach_leadlined": "Fashion tip: opaque to radiation.",
    "fishach_silencebird": "The bird stops singing.",
    "fishach_youspinmeround": "Round and round and round...",
    "fishach_ghostbuster": "Who you gonna call?",
    "fishach_silentsingularity": "A quiet appetite that eats light.",
    "fishach_secret_maintenanceoracle": "The tunnels whisper. Listen carefully.",
    "fishach_secret_quietai": "No laws spoken. Still watching.",
    "fishach_secret_thirdlawdebate": "Protect humans — except when the debate starts.",
    "fishach_secret_lostandfoundrelic": "Lost. Found. Claimed by fate.",
    "fishach_secret_echoesinsolars": "Sunlight and static in equal measure.",
    "fishach_secret_nameonthemanifest": "Your name, someone else's shift.",
    "fishach_secret_wrongshuttle": "Wrong dock. Right story.",
    "fishach_secret_vendingjackpot": "The machine owes you. Collect.",
    "fishach_secret_camerablindspot": "Smile. Nobody's watching — supposedly.",
    "fishach_secret_theotherbutton": "Not that one. The other one.",
    "fishach_orig_emagcuriosity": "A card that opens more than doors.",
    "fishach_orig_lawsetpoetry": "Silicon sonnets with sharp edges.",
}

RIDDLES_RU: dict[str, str] = {
    "fish-banana-requiem": "Жёлтое. Мягкое. Пол вдруг становится врагом.",
    "fishach_10bux": "Чаевые за молчание. Некоторые секреты стоят десятку.",
    "fishach_100mdash": "Когда часы останавливаются — подними бокал.",
    "fishach_421": "Четыре. Два. Один. Казино всё помнит.",
    "fishach_ahollyjollyspacemas": "Праздничное настроение доставляют ящики с инструментами.",
    "fishach_adjutantonline": "Доброе утро, капитан. Системы в сети.",
    "fishach_banned": "Не бан от админов — изгнание другого сорта.",
    "fishach_bearhug": "Тёплое объятие. Холодный итог.",
    "fishach_bombiniismissing": "Кто-то маленький и шумный не на своём месте.",
    "fishach_buttonpusher": "Любопытство любит большие красные кнопки.",
    "fishach_captainslog": "Звёздная дата: всё равно. Занеси в журнал.",
    "fishach_chevalierdusorbet": "Морозный титул для морозного похода.",
    "fishach_deepfreeze": "Холод не торгуется.",
    "fishach_formatcomplete": "Форматирование завершено. Личность — опционально.",
    "fishach_goawaybatin": "Кыш. Вентиляция — не гостиница.",
    "fishach_googone": "Был. И нет.",
    "fishach_guerrierdugelato": "Ещё один ледяной рыцарь десертных войн.",
    "fishach_helios": "Снова слишком близко к солнцу.",
    "fishach_hesdeadjim": "Поэзия медотсека. Версия с плоской линией.",
    "fishach_ijustcleanedthat": "Свежий пол. Свежее разочарование.",
    "fishach_ispy": "Я вижу кое-что... секретное.",
    "fishach_icarus": "Крылья из воска, станция из стали.",
    "fishach_identitytheft": "Твоё лицо — чужая улыбка.",
    "fishach_illuminated": "Озарение, которое жжёт.",
    "fishach_isitreallythattimeagain": "Календарь смеётся. Ты — нет.",
    "fishach_itsatrap": "Это ловушка. Классика.",
    "fishach_itsnoteasybeinggreen": "Нелегко быть зелёным.",
    "fishach_itsamemario": "It's-a me... только в космосе.",
    "fishach_leavenomanbehind": "Все возвращаются. Или почти все.",
    "fishach_lockblock": "Доступ запрещён — как стиль жизни.",
    "fishach_masterofunlocking": "Ключи, карты и сомнительная этика.",
    "fishach_mybolognahasafirstname": "У этой колбасы есть имя.",
    "fishach_nero": "Станция горит. Кто-то настраивает скрипку.",
    "fishach_newtonscrew": "Что взлетает вверх, то шутит про гравитацию.",
    "fishach_ohdoctor": "О, доктор! Нет, не тот доктор.",
    "fishach_olbuddyolpal": "Старый друг. Старая услуга.",
    "fishach_oldenemy": "Знакомое лицо. Незнакомая сторона.",
    "fishach_onmyowneightspacelegs": "Восемь ног — ноль оправданий.",
    "fishach_onearmedbandit": "Дёрни рычаг. Не доверяй удаче.",
    "fishach_originalsin": "Первая ошибка на вкус как фрукт.",
    "fishach_remembertowashbehindtheantennae": "Гигиена для хромированной кожи.",
    "fishach_rookiethief": "Первая кража. Мягкие перчатки. Громкое сердце.",
    "fishach_sheesh": "Шиш.",
    "fishach_shutupandjam": "Меньше слов. Больше шума.",
    "fishach_spaceham": "Не радиолюбительский.",
    "fishach_spaceshipoftheseus": "Замени каждый болт. Это всё ещё ты?",
    "fishach_spamhaus": "Пустой ящик — миф.",
    "fishach_survivor": "Всё ещё здесь. Как-то.",
    "fishach_suspiciouscharacter": "Датчики от тебя нервничают.",
    "fishach_thankyoubusdriver": "Держитесь. Следующая остановка — куда угодно.",
    "fishach_thatsnomoonthats": "Это не луна...",
    "fishach_theforceisstrongwiththis": "Махни рукой. Забудь про шлюз.",
    "fishach_tinkerer": "Ещё один провод. Что может пойти не так?",
    "fishach_trailofblood": "Следуй по красным крошкам.",
    "fishach_tryjigglingthehandle": "Пробовал выключить и включить?",
    "fishach_virtualascension": "Загрузка завершена. Тело — по желанию.",
    "fishach_weirdscience": "Наука! С джазовыми руками.",
    "fishach_wonk": "Вонк.",
    "fishach_wrathofthefather": "Папа не злится. Папа разочарован. Громко.",
    "fishach_arcanefailure": "Ритуал почти сработал. Почти.",
    "fishach_braindamage": "Мысли опциональны. Последствия — нет.",
    "fishach_breathofdeath": "Вдох. Сожаление. Выдох.",
    "fishach_burninhell": "Температура растёт. Настроение — нет.",
    "fishach_catastrophe": "Если уж лить, то через пробоину.",
    "fishach_embracethebird": "Обними пернатое знамение.",
    "fishach_honorarynukie": "Красный вайб без приглашения.",
    "fishach_leadlined": "Мода сезона: непрозрачно для радиации.",
    "fishach_silencebird": "Птица замолкает.",
    "fishach_youspinmeround": "Кругом, кругом, кругом...",
    "fishach_ghostbuster": "Кому ты позвонишь?",
    "fishach_silentsingularity": "Тихий аппетит, который ест свет.",
    "fishach_secret_maintenanceoracle": "Тоннели шепчут. Слушай внимательнее.",
    "fishach_secret_quietai": "Законы не произнесены. Но смотрит.",
    "fishach_secret_thirdlawdebate": "Защищай людей — пока не начнётся спор.",
    "fishach_secret_lostandfoundrelic": "Потеряно. Найдено. Присвоено судьбой.",
    "fishach_secret_echoesinsolars": "Солнечный свет и помехи поровну.",
    "fishach_secret_nameonthemanifest": "Твоё имя — чужая смена.",
    "fishach_secret_wrongshuttle": "Не тот док. Та самая история.",
    "fishach_secret_vendingjackpot": "Автомат должен. Забери своё.",
    "fishach_secret_camerablindspot": "Улыбнись. Вроде никто не смотрит.",
    "fishach_secret_theotherbutton": "Не та. Другая.",
    "fishach_orig_emagcuriosity": "Карта, которая открывает больше, чем двери.",
    "fishach_orig_lawsetpoetry": "Кремниевые сонеты с острыми краями.",
}

FALLBACK_RIDDLE_EN = "The station remembers. You don't — yet."
FALLBACK_RIDDLE_RU = "Станция помнит. Ты — пока нет."


def parse_ftl(path: Path) -> dict[str, str]:
    data: dict[str, str] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        m = re.match(r"^([A-Za-z0-9_\-]+)\s*=\s*(.*)$", line)
        if m:
            data[m.group(1)] = m.group(2)
    return data


def is_ru_placeholder_name(s: str) -> bool:
    return s.startswith("Достижение:")


def is_ru_placeholder_desc(s: str) -> bool:
    return (
        "Особое условие Fish Station" in s
        or s.strip() in {"???", ""}
        or s.startswith("Адаптированное условие")
    )


def needs_riddle(s: str | None) -> bool:
    return s is None or s.strip() in {"???", ""}


def short_flavor_ru_from_en(en_desc: str, translator: GoogleTranslator, cache: dict[str, str]) -> str:
    text = en_desc.strip()
    if not text:
        return "Сделай что-то стоящее на смене."
    # Truncate huge spoilery walls of text for display
    if len(text) > 220:
        text = text[:217].rsplit(" ", 1)[0] + "…"
    if text in cache:
        return cache[text]
    try:
        ru = translator.translate(text)
        time.sleep(0.05)
    except Exception:
        ru = text
    cache[text] = ru
    return ru


def main() -> None:
    en = parse_ftl(EN_PATH)
    ru = parse_ftl(RU_PATH)
    translator = GoogleTranslator(source="en", target="ru")
    cache: dict[str, str] = {}

    # Collect achievement base ids from name keys
    bases: list[str] = []
    for key in en:
        if key.endswith("-name") and key.startswith("achievement-"):
            bases.append(key[len("achievement-") : -len("-name")])

    # Update EN secrets
    for base in bases:
        sk = f"achievement-{base}-secret"
        if sk in en and needs_riddle(en[sk]):
            en[sk] = RIDDLES_EN.get(base, FALLBACK_RIDDLE_EN)
        # Also add secret lines for bases that have secret in YAML but missing? skip

    # Seed/handcrafted already good; force banana etc.
    for base, riddle in RIDDLES_EN.items():
        en[f"achievement-{base}-secret"] = riddle

    # Update RU names/descs/secrets
    for base in bases:
        name_k = f"achievement-{base}-name"
        desc_k = f"achievement-{base}-desc"
        sec_k = f"achievement-{base}-secret"

        en_name = en.get(name_k, base)
        en_desc = en.get(desc_k, "")

        ru_name = ru.get(name_k, "")
        if is_ru_placeholder_name(ru_name) or not ru_name:
            # Убираем префикс «Достижение:», оставляем живое название (часто англ. титул — ок для порта)
            cleaned = re.sub(r"^Достижение:\s*", "", ru_name).strip() if ru_name else ""
            ru[name_k] = cleaned or en_name

        ru_desc = ru.get(desc_k, "")
        if is_ru_placeholder_desc(ru_desc):
            # Для секретных с ??? в desc — нормальное описание после unlock из EN
            if needs_riddle(ru_desc) and base.startswith(("fishach_secret_", "fishach_orig_")):
                # после unlock покажем нормальный spoiler-lite текст
                ru[desc_k] = short_flavor_ru_from_en(en_desc if en_desc not in {"???", ""} else en_name, translator, cache)
            else:
                ru[desc_k] = short_flavor_ru_from_en(en_desc, translator, cache)

        # Secrets: всегда загадка, если ключ есть или есть в RIDDLES
        if sec_k in en or sec_k in ru or base in RIDDLES_RU:
            ru[sec_k] = RIDDLES_RU.get(base, FALLBACK_RIDDLE_RU)
            if sec_k not in en:
                en[sec_k] = RIDDLES_EN.get(base, FALLBACK_RIDDLE_EN)

    # Force all known riddles into RU
    for base, riddle in RIDDLES_RU.items():
        ru[f"achievement-{base}-secret"] = riddle

    # Improve a few seed RU/EN already fine
    en["achievement-fish-banana-requiem-secret"] = RIDDLES_EN["fish-banana-requiem"]
    ru["achievement-fish-banana-requiem-secret"] = RIDDLES_RU["fish-banana-requiem"]

    def write_ftl(path: Path, data: dict[str, str], original_order_path: Path) -> None:
        # Preserve original key order; append new keys at end
        lines_out: list[str] = []
        seen: set[str] = set()
        for line in original_order_path.read_text(encoding="utf-8").splitlines():
            m = re.match(r"^([A-Za-z0-9_\-]+)\s*=\s*(.*)$", line)
            if not m:
                lines_out.append(line)
                continue
            key = m.group(1)
            seen.add(key)
            if key in data:
                lines_out.append(f"{key} = {data[key]}")
            else:
                lines_out.append(line)
        for key, val in data.items():
            if key not in seen:
                lines_out.append(f"{key} = {val}")
        path.write_text("\n".join(lines_out) + "\n", encoding="utf-8")

    # Read originals for order before overwrite
    en_order = EN_PATH
    ru_order = RU_PATH
    write_ftl(EN_PATH, en, en_order)
    write_ftl(RU_PATH, ru, ru_order)
    print(f"Updated {EN_PATH}")
    print(f"Updated {RU_PATH}")
    print(f"Translated cache size: {len(cache)}")


if __name__ == "__main__":
    main()
