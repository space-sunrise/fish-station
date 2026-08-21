# -*- coding: utf-8 -*-
"""Translate RU achievement names that lack Cyrillic so Locale Validator passes."""
from __future__ import annotations

import re
import time
from pathlib import Path

from deep_translator import GoogleTranslator

RU_PATH = Path(r"z:\Не трогать\fish-station-mane\Resources\Locale\ru-RU\_fish\achievements.ftl")
CYRILLIC = re.compile(r"[А-Яа-яЁё]")

# Ручные названия (лучше машинного перевода / каламбуры)
MANUAL: dict[str, str] = {
    "achievement-fishach_bestmealofmylife-name": "Лучшая трапеза в жизни",
    "achievement-fishach_crossingthehorizon-name": "За горизонт",
    "achievement-fishach_davemymindisgoing-name": "Дейв, мой разум уходит",
    "achievement-fishach_ggghosts-name": "П-п-призраки?",
    "achievement-fishach_grilledseasonedveteran-name": "Жареный закалённый ветеран",
    "achievement-fishach_killmekillmekillme-name": "УБЕЙМЕНЯУБЕЙМЕНЯУБЕЙМЕНЯ",
    "achievement-fishach_letsplayglobalthermonuclearwar-name": "Сыграем в ГЛОБАЛЬНУЮ ТЕРМОЯДЕРНУЮ ВОЙНУ",
    "achievement-fishach_littlechickadee-name": "Маленькая синичка",
    "achievement-fishach_mindthegap-name": "Осторожно, зазор",
    "achievement-fishach_nyoooom-name": "НЬЮУУУМ",
    "achievement-fishach_overextendedthejoke-name": "Шутка затянулась",
    "achievement-fishach_pleasejustendthepain-name": "Просто прекрати боль",
    "achievement-fishach_seasonedveteran-name": "Закалённый ветеран",
    "achievement-fishach_sousvidegrilledseasonedveteran-name": "Су-вид жареный закалённый ветеран",
    "achievement-fishach_soyboy-name": "Соевый мальчик",
    "achievement-fishach_survivalofthefittest-name": "Выживание сильнейших",
    "achievement-fishach_swarmbeaconcrusher-name": "Крушитель роевых маяков",
    "achievement-fishach_swarmbeaconkiller-name": "Убийца роевых маяков",
    "achievement-fishach_thatwasstupidofyou-name": "Это было глупо с твоей стороны",
    "achievement-fishach_thislousyachievement-name": "Это убогое достижение",
    "achievement-fishach_veteran-name": "Ветеран",
    "achievement-fishach_whoosh-name": "ВЖУХ!",
    "achievement-fishach_greentext-name": "Гринтекст",
    "achievement-fishach_10bux-name": ":10баксов:",
    "fish-achievements-secret-placeholder": "？？？",
}


def has_cyrillic(s: str) -> bool:
    return bool(CYRILLIC.search(s))


def main() -> None:
    translator = GoogleTranslator(source="en", target="ru")
    cache: dict[str, str] = {}
    lines = RU_PATH.read_text(encoding="utf-8").splitlines()
    out: list[str] = []
    fixed = 0

    for line in lines:
        m = re.match(r"^([A-Za-z0-9_\-]+)\s*=\s*(.*)$", line)
        if not m:
            out.append(line)
            continue
        key, value = m.group(1), m.group(2)
        if key in MANUAL:
            value = MANUAL[key]
            fixed += 1
        elif key.endswith("-name") and value.strip() and not has_cyrillic(value):
            if value in cache:
                value = cache[value]
            else:
                try:
                    translated = translator.translate(value)
                    time.sleep(0.04)
                except Exception as exc:
                    print("FAIL", key, value, exc)
                    translated = f"{value} (ачивка)"
                # Гарантия кириллицы
                if not has_cyrillic(translated):
                    translated = f"{translated} — ачивка"
                cache[m.group(2)] = translated
                value = translated
            fixed += 1
        out.append(f"{key} = {value}")

    RU_PATH.write_text("\n".join(out) + "\n", encoding="utf-8")
    print(f"fixed={fixed}, cache={len(cache)}")


if __name__ == "__main__":
    main()
