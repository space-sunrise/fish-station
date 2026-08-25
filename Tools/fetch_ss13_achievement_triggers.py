#!/usr/bin/env python3
"""Fetch SS13 achievement unlock sites from BeeStation/TG GitHub raw files."""
from __future__ import annotations

import json
import re
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Resources/Docs/_Fish/Ss13AchievementTriggerScan.json"

BEE_BASE = "https://raw.githubusercontent.com/BeeStation/BeeStation-Hornet/master/"
TG_BASE = "https://raw.githubusercontent.com/tgstation/tgstation/master/"

# Files known to contain give_award / achievement unlock from Bee commit history
SCAN_PATHS = [
    "code/modules/mob/living/death.dm",
    "code/modules/mob/living/living.dm",
    "code/modules/mob/living/carbon/human/human_defense.dm",
    "code/game/gamemodes/meteor/meteors.dm",
    "code/game/machinery/computer/arcade.dm",
    "code/modules/awaymissions/super_secret_room.dm",
    "code/modules/mob/living/simple_animal/hostile/megafauna/megafauna.dm",
    "code/modules/mob/living/carbon/human/life.dm",
    "code/modules/mob/living/carbon/human/human_helpers.dm",
    "code/modules/mob/living/emote.dm",
    "code/modules/vehicles/bicycle.dm",
    "code/modules/vehicles/scooter.dm",
    "code/game/objects/items/toys.dm",
    "code/modules/atmospherics/machinery/pipes/pipes.dm",
    "code/modules/mob/living/silicon/ai/ai.dm",
    "code/modules/reagents/chemistry/reagents/other_reagents.dm",
    "code/modules/mob/living/carbon/human/species.dm",
    "code/game/machinery/Speedrunners.dm",
    "code/modules/mob/living/simple_animal/friendly/pet.dm",
    "code/modules/mob/living/carbon/alien/larva/larva.dm",
    "code/modules/shuttle/on_move.dm",
    "code/game/objects/items/grabbing.dm",
    "code/modules/mob/living/simple_animal/bot/honkbot.dm",
    "code/modules/mob/living/carbon/human/death.dm",
    "code/modules/mob/suicide.dm",
    "code/modules/mob/living/carbon/human/human.dm",
    "code/modules/food_and_drinks/drinks/drinks.dm",
    "code/datums/elements/footstep.dm",
    "code/modules/mob/living/simple_animal/friendly/corgi.dm",
    "code/modules/mob/living/carbon/human/species_types/golems.dm",
    "code/modules/ruins/lavaland_legendary.dm",
    "code/modules/mob/living/simple_animal/hostile/mining_mobs/necropolis_tendril.dm",
    # TG equivalents
]

TG_SCAN = [
    "code/modules/meteors/meteor_types.dm",
    "code/modules/meteors/meteor_deflection.dm",
    "code/game/machinery/computer/arcade.dm",
    "code/modules/mob/living/death.dm",
    "code/modules/vehicles/bicycle.dm",
]


def fetch(url: str) -> str | None:
    try:
        with urllib.request.urlopen(url, timeout=20) as resp:
            return resp.read().decode("utf-8", errors="replace")
    except Exception as e:
        return None


def scan(content: str, path: str, repo: str) -> list[dict]:
    hits: list[dict] = []
    if not content:
        return hits
    for i, line in enumerate(content.splitlines(), 1):
        if "give_award" not in line and "unlock" not in line.lower():
            continue
        if "/datum/award" not in line and "achievement" not in line.lower():
            continue
        ctx_start = max(0, i - 4)
        ctx_end = min(len(content.splitlines()), i + 3)
        ctx = content.splitlines()[ctx_start:ctx_end]
        hits.append(
            {
                "repo": repo,
                "file": path,
                "line": i,
                "code": line.strip(),
                "context": ctx,
            }
        )
    return hits


def main() -> None:
    all_hits: list[dict] = []
    for path in SCAN_PATHS:
        url = BEE_BASE + path
        text = fetch(url)
        all_hits.extend(scan(text or "", path, "BeeStation"))

    for path in TG_SCAN:
        url = TG_BASE + path
        text = fetch(url)
        all_hits.extend(scan(text or "", path, "tgstation"))

    OUT.write_text(json.dumps(all_hits, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"hits: {len(all_hits)} -> {OUT}")


if __name__ == "__main__":
    main()
