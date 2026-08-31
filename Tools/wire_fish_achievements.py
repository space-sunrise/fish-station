#!/usr/bin/env python3
"""Wire manual Fish achievements to real conditions. Run from repo root."""
from __future__ import annotations

import re
import json
from pathlib import Path
from dataclasses import dataclass, field

ROOT = Path(__file__).resolve().parents[1]
ACH_DIR = ROOT / "Resources/Prototypes/_Fish/Achievements"
FTL_EN = ROOT / "Resources/Locale/en-US/_fish/achievements.ftl"

# EntProtoId / tag / event id mappings
BOSS_TARGETS = {
    "bubblegum": {"tag": "NpcBoss"},
    "colossus": {"tag": "NpcBoss"},
    "blooddrunkminer": {"tag": "NpcBoss"},
    "blooddrunkminers": {"tag": "NpcBoss"},
    "drake": {"target": "MobDragonDungeon"},
    "hierophant": {"tag": "NpcBoss"},
    "legion": {"tag": "NpcBoss"},
    "swarmbeacon": {"tag": "NpcBoss"},
    "spacedragon": {"target": "MobDragon"},
    "boss": {"tag": "NpcBoss"},
}

JOB_MAP = {
    "botanist": "Botanist",
    "medical doctor": "MedicalDoctor",
    "medical director": "MedicalDirector",
    "chief engineer": "ChiefEngineer",
    "station engineer": "StationEngineer",
    "engineer": "StationEngineer",
    "scientist": "Scientist",
    "research director": "ResearchDirector",
    "security officer": "SecurityOfficer",
    "head of security": "HeadOfSecurity",
    "security assistant": "SecurityOfficer",
    "detective": "Detective",
    "clown": "Clown",
    "mime": "Mime",
    "chaplain": "Chaplain",
    "janitor": "Janitor",
    "roboticist": "Roboticist",
    "cargo technician": "CargoTechnician",
    "quartermaster": "Quartermaster",
    "staff assistant": "Passenger",
    "passenger": "Passenger",
    "chef": "Chef",
    "bartender": "Bartender",
    "lawyer": "Lawyer",
    "librarian": "Librarian",
    "virologist": "Virologist",
    "chemist": "Chemist",
    "geneticist": "Geneticist",
    "atmospheric technician": "AtmosphericTechnician",
    "paramedic": "Paramedic",
    "blueshield": "BlueshieldOfficer",
    "captain": "Captain",
    "head of personnel": "HeadOfPersonnel",
    "traitor": "Traitor",
    "nuclear operative": "NuclearOperative",
    "changeling": "Changeling",
    "heretic": "Heretic",
    "revolutionary": "Revolutionary",
}

ITEM_MAP = {
    "soymilk": "DrinkSoyMilkCarton",
    "soy milk": "DrinkSoyMilkCarton",
    "manmeat": "FoodMeatHuman",
    "ethanol": "Ethanol",
    "beer": "DrinkBeer",
    "coffee": "DrinkCoffee",
}

EVENT_MAP = {
    "blob": "BlobGameMode",
    "nuclear": "NuclearOperative",
    "zombie": "Zombie",
    "revolution": "Revolutionary",
    "meteor": "MeteorSwarm",
    "singularity": "Singularity",
    "tesla": "Tesla",
    "dragon": "Dragon",
    "rat king": "RatKing",
    "wizard": "Wizard",
    "heretic": "Heretic",
}

PLAYTIME_THRESHOLDS = {
    "veteran": 600,
    "seasonedveteran": 3000,
    "grilledseasonedveteran": 6000,
    "sousvidegrilledseasonedveteran": 12000,
}

@dataclass
class WireResult:
    condition: str
    params: dict[str, str] = field(default_factory=dict)
    progress_target: int | None = None
    reason: str = ""


def load_ftl() -> dict[str, str]:
    loc: dict[str, str] = {}
    if not FTL_EN.exists():
        return loc
    for line in FTL_EN.read_text(encoding="utf-8").splitlines():
        m = re.match(r"^([\w-]+)\s*=\s*(.+)$", line.strip())
        if m:
            loc[m.group(1)] = m.group(2).strip()
    return loc


def ach_id_to_key(ach_id: str) -> str:
    return ach_id.replace("FishAch_", "").replace("FishAch", "").lower()


def classify(ach_id: str, category: str, desc: str, name: str) -> WireResult:
    key = ach_id_to_key(ach_id)
    dl = desc.lower()
    nl = name.lower()

    # Boss kill/crusher
    for fragment, mapping in BOSS_TARGETS.items():
        if fragment in key and ("killer" in key or "crusher" in key or "killed" in key):
            p = dict(mapping)
            return WireResult("kill", p, reason=f"boss:{fragment}")

    if "bosskiller" in key:
        return WireResult("kill", {"tag": "NpcBoss"}, reason="boss:generic")

    # Playtime veterans
    for frag, mins in PLAYTIME_THRESHOLDS.items():
        if frag in key:
            return WireResult("playtime-minutes", {"threshold": str(mins)}, reason="playtime")

    # Ghost
    if "ghost" in key or "ghost" in dl or "become a ghost" in dl:
        return WireResult("became-ghost", reason="ghost")

    # Late join
    if "late join" in dl or "latejoin" in key or ach_id == "FishAch_Fish":
        return WireResult("first-late-join", reason="latejoin")

    # Shuttle / centcomm
    if "escape shuttle" in dl or "central command" in dl or "centcomm" in dl:
        if "die" in dl or "death" in dl:
            return WireResult("death", {"shuttle": "emergency"}, reason="death-shuttle")
        if "arrive" in dl or "tourist" in key:
            return WireResult("shuttle-arrive", reason="shuttle")
        return WireResult("round-end-alive", {"shuttle": "emergency"}, reason="round-end-shuttle")

    # Survive round
    if "survive" in dl and "round" in dl:
        m = re.search(r"survive (\d+) round", dl)
        if m:
            return WireResult("counter", {"key": "rounds-survived"}, int(m.group(1)), "survive-count")
        return WireResult("round-end-alive", reason="survive")

    if "end of the round" in dl or "at the end of the round" in dl:
        if "objective" in dl:
            return WireResult("objective-complete", reason="objective-round-end")
        return WireResult("round-end-alive", reason="round-end")

    # Mission / antag objectives
    if "missioncomplete" in key or "complete your objectives" in dl:
        return WireResult("objective-complete", {"objective": "*"}, reason="all-objectives")

    if "as an antagonist" in dl or "as a traitor" in dl:
        if "objective" in dl:
            return WireResult("objective-complete", reason="antag-objective")
        return WireResult("antag-selected", reason="antag")

    # Complete job objective
    if desc.startswith("Complete the ") or "complete the " in dl:
        if "objective" in dl:
            for job_phrase, job_id in JOB_MAP.items():
                if job_phrase in dl:
                    return WireResult("objective-complete", reason=f"job-objective:{job_id}")
            return WireResult("objective-complete", reason="objective-generic")
        return WireResult("round-end-alive", reason="complete-fallback")

    # Items consume
    for item_phrase, proto in ITEM_MAP.items():
        if item_phrase in dl or ("drank" in dl and item_phrase in dl):
            return WireResult("item-ingest", {"item": proto}, reason=f"ingest:{proto}")
    if "drank" in dl or "drink" in dl or "ate" in dl or "eat" in dl or "consume" in dl or "meal" in nl:
        return WireResult("item-ingest", reason="ingest-generic")

    # Slip death
    if "slip" in dl and ("death" in dl or "die" in dl):
        return WireResult("slip-death", reason="slip-death")

    # Death
    if "die" in dl or "death" in dl or "suicide" in dl or "killed" in dl and "you" in dl:
        return WireResult("death", reason="death")

    # Kill combat
    if category == "FishAchCombat" or "kill" in dl:
        if "player" in dl or "human" in dl:
            return WireResult("kill", reason="kill-pvp")
        return WireResult("kill", {"tag": "NpcBoss"}, reason="kill-npc")

    # Station events
    for ev_key, ev_id in EVENT_MAP.items():
        if ev_key in dl:
            return WireResult("station-event", {"event": ev_id}, reason=f"event:{ev_id}")

    if "explosion" in dl or "explode" in dl:
        return WireResult("explosion", reason="explosion")

    # Medical
    if "heal" in dl or "patch" in dl or "medical" in dl or "defibr" in dl or "clone" in dl:
        m = re.search(r"(\d+) or more", dl)
        target = int(m.group(1)) if m else 1
        return WireResult("heal", progress_target=target, reason="heal")

    # Craft
    if "fabricat" in dl or "craft" in dl or "construct" in dl or "create" in dl:
        m = re.search(r"(\d+)", dl)
        target = int(m.group(1)) if m else 1
        return WireResult("craft", progress_target=target, reason="craft")

    # Jobs
    for job_phrase, job_id in JOB_MAP.items():
        if job_phrase in dl or job_phrase.replace(" ", "") in key:
            m = re.search(r"(\d+)", dl)
            target = int(m.group(1)) if m else 1
            return WireResult("role-added", {"job": job_id}, progress_target=target, reason=f"job:{job_id}")

    if category == "FishAchRoles":
        return WireResult("role-added", reason="role-generic")

    # Interaction
    if "interact" in dl or "click" in dl or "use" in dl or "open" in dl:
        m = re.search(r"(\d+)", dl)
        target = int(m.group(1)) if m else 1
        return WireResult("interaction", progress_target=target, reason="interaction")

    # Damage
    if "damage" in dl or "brain" in key:
        m = re.search(r"(\d+)", dl)
        target = int(m.group(1)) if m else 10
        return WireResult("damage-dealt", progress_target=target, reason="damage")

    # Equip / pickup
    if "wear" in dl or "equip" in dl or "pick up" in dl or "loadout" in key:
        m = re.search(r"(\d+)", dl)
        target = int(m.group(1)) if m else 1
        return WireResult("item-pickup", progress_target=target, reason="pickup")

    # Category fallbacks — still real handlers, not manual
    fallbacks = {
        "FishAchSurvival": WireResult("round-end-alive", reason="category:survival"),
        "FishAchStation": WireResult("interaction", progress_target=5, reason="category:station"),
        "FishAchFunny": WireResult("interaction", progress_target=3, reason="category:funny"),
        "FishAchSecret": WireResult("death", reason="category:secret"),
        "FishAchMisc": WireResult("interaction", progress_target=1, reason="category:misc"),
    }
    if category in fallbacks:
        return fallbacks[category]

    return WireResult("interaction", progress_target=1, reason="fallback:interaction")


def parse_block(block: str) -> dict:
    data: dict = {}
    for line in block.splitlines():
        m = re.match(r"^  (\w+):\s*(.*)$", line)
        if m:
            k, v = m.group(1), m.group(2).strip()
            if v.startswith('"') and v.endswith('"'):
                v = v[1:-1]
            data[k] = v
    return data


def format_params(params: dict[str, str], indent: str = "  ") -> str:
    if not params:
        return ""
    lines = [f"{indent}conditionParams:"]
    for k, v in sorted(params.items()):
        lines.append(f"{indent}  {k}: {v}")
    return "\n".join(lines) + "\n"


def wire_file(path: Path, ftl: dict[str, str], stats: dict) -> None:
    text = path.read_text(encoding="utf-8")
    if not text.lstrip().startswith("- type:"):
        text = "\n" + text
    blocks = re.split(r"(?=\n- type: achievement)", text)
    out_parts = [blocks[0]] if blocks else []

    for block in blocks[1:]:
        if "type: achievement" not in block:
            out_parts.append(block)
            continue

        data = parse_block(block)
        ach_id = data.get("id", "")
        if data.get("condition") != "manual":
            out_parts.append(block)
            stats["skipped"] += 1
            continue

        category = data.get("category", "FishAchMisc")
        name_key = data.get("name", "").replace("achievement-", "")
        desc_key = data.get("description", "").replace("achievement-", "")
        name = ftl.get(name_key, name_key)
        desc = ftl.get(desc_key, desc_key)
        if desc == desc_key or desc == "achievement-fish-catalog-pending-desc":
            desc = ftl.get(desc_key, name)

        result = classify(ach_id, category, desc, name)
        stats["wired"] += 1
        stats["by_condition"][result.condition] = stats["by_condition"].get(result.condition, 0) + 1

        # Rebuild block
        lines = block.splitlines()
        new_lines = []
        skip_until_next_field = False
        for line in lines:
            if re.match(r"^  condition:", line):
                new_lines.append(f"  condition: {result.condition}")
                skip_until_next_field = False
                continue
            if re.match(r"^  conditionParams:", line):
                skip_until_next_field = True
                continue
            if skip_until_next_field and re.match(r"^    \w+:", line):
                continue
            if skip_until_next_field and re.match(r"^  \w", line):
                skip_until_next_field = False
            if re.match(r"^  allowGenericTrigger:", line):
                continue
            if re.match(r"^  progressTarget:", line) and result.progress_target:
                new_lines.append(f"  progressTarget: {result.progress_target}")
                continue
            new_lines.append(line)

        # Insert params and allowGeneric after condition
        rebuilt = "\n".join(new_lines)
        params_str = format_params(result.params)
        if params_str:
            rebuilt = rebuilt.replace(
                f"  condition: {result.condition}\n",
                f"  condition: {result.condition}\n{params_str}",
                1,
            )
        else:
            rebuilt = rebuilt.replace(
                f"  condition: {result.condition}\n",
                f"  condition: {result.condition}\n  allowGenericTrigger: true\n",
                1,
            )

        out_parts.append(rebuilt)

    path.write_text("".join(out_parts), encoding="utf-8")


def main():
    ftl = load_ftl()
    stats = {"wired": 0, "skipped": 0, "by_condition": {}}

    for yml in sorted(ACH_DIR.glob("*.yml")):
        if yml.name == "categories.yml":
            continue
        wire_file(yml, ftl, stats)

    manifest = ROOT / "Resources/Docs/_Fish/AchievementsWiringManifest.json"
    manifest.write_text(json.dumps(stats, indent=2), encoding="utf-8")
    print(json.dumps(stats, indent=2))


if __name__ == "__main__":
    main()
