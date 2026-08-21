# -*- coding: utf-8 -*-
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(r"z:\Не трогать\fish-station-mane")
EN_PATH = ROOT / "Resources/Locale/en-US/_fish/achievements.ftl"
RU_PATH = ROOT / "Resources/Locale/ru-RU/_fish/achievements.ftl"

EN_FIXES = {
    "achievement-fishach_silentsingularity-desc": "You met a hunger that does not roar.",
    "achievement-fishach_secret_maintenanceoracle-desc": "Forty quiet conversations with the tunnels later...",
    "achievement-fishach_secret_quietai-desc": "An AI that listens more than it lectures.",
    "achievement-fishach_secret_thirdlawdebate-desc": "Silicon ethics, argued until sparks fly.",
    "achievement-fishach_secret_lostandfoundrelic-desc": "Claimed something that was never quite yours.",
    "achievement-fishach_secret_echoesinsolars-desc": "Heard the station breathe between solar panels.",
    "achievement-fishach_secret_nameonthemanifest-desc": "Your name was on the list. The story was not.",
    "achievement-fishach_secret_wrongshuttle-desc": "Arrived somewhere that was not the plan.",
    "achievement-fishach_secret_vendingjackpot-desc": "The machine finally paid out.",
    "achievement-fishach_secret_camerablindspot-desc": "Moved where the cameras politely look away.",
    "achievement-fishach_secret_theotherbutton-desc": "Pressed the button everyone ignores.",
    "achievement-fishach_orig_emagcuriosity-desc": "Curiosity, laminated in purple.",
    "achievement-fishach_orig_lawsetpoetry-desc": "Rewrote silicon commandments into verse.",
    "achievement-fishach_421-desc": "Four, two, one — and the house remembers.",
    "achievement-fishach_bearhug-desc": "A warm squeeze with cold consequences.",
    "achievement-fishach_bombiniismissing-desc": "Someone small and loud went missing.",
    "achievement-fishach_captainslog-desc": "Make it official. Stardate optional.",
    "achievement-fishach_itsamemario-desc": "It's-a you, in space.",
}

RU_FIXES = {
    "achievement-fishach_silentsingularity-desc": "Ты встретил голод, который не рычит.",
    "achievement-fishach_secret_maintenanceoracle-desc": "Сорок тихих разговоров с тоннелями спустя...",
    "achievement-fishach_secret_quietai-desc": "ИИ, который больше слушает, чем читает нотации.",
    "achievement-fishach_secret_thirdlawdebate-desc": "Кремниевая этика — пока не полетят искры.",
    "achievement-fishach_secret_lostandfoundrelic-desc": "Забрал то, что никогда не было по-настоящему твоим.",
    "achievement-fishach_secret_echoesinsolars-desc": "Услышал, как станция дышит между солнечными панелями.",
    "achievement-fishach_secret_nameonthemanifest-desc": "Имя в списке было твоим. История — нет.",
    "achievement-fishach_secret_wrongshuttle-desc": "Прибыл туда, куда не собирался.",
    "achievement-fishach_secret_vendingjackpot-desc": "Автомат наконец-то расплатился.",
    "achievement-fishach_secret_camerablindspot-desc": "Прошёл там, куда камеры вежливо не смотрят.",
    "achievement-fishach_secret_theotherbutton-desc": "Нажал кнопку, которую все игнорируют.",
    "achievement-fishach_orig_emagcuriosity-desc": "Любопытство, ламинированное фиолетовым.",
    "achievement-fishach_orig_lawsetpoetry-desc": "Переписал кремниевые заповеди в стихи.",
    "achievement-fishach_misc_gallerycritic-name": "Критик галереи",
    "achievement-fishach_421-desc": "Четыре, два, один — и казино всё помнит.",
    "achievement-fishach_bearhug-desc": "Тёплое сжатие с холодными последствиями.",
    "achievement-fishach_bombiniismissing-desc": "Кто-то маленький и шумный пропал.",
    "achievement-fishach_captainslog-desc": "Занеси в журнал. Звёздная дата — по желанию.",
    "achievement-fishach_itsamemario-desc": "It's-a you, только в космосе.",
}


def apply(path: Path, fixes: dict[str, str]) -> None:
    text = path.read_text(encoding="utf-8")
    for key, value in fixes.items():
        text2, n = re.subn(
            rf"^{re.escape(key)}\s*=\s*.*$",
            f"{key} = {value}",
            text,
            count=1,
            flags=re.M,
        )
        if n == 0:
            print("MISSING", path.name, key)
        text = text2
    path.write_text(text, encoding="utf-8")
    print("patched", path.name, len(fixes))


if __name__ == "__main__":
    apply(EN_PATH, EN_FIXES)
    apply(RU_PATH, RU_FIXES)
