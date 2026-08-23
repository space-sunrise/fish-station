#!/usr/bin/env python3
"""Refine Fish achievements: replace generic allowGenericTrigger with specific conditionParams."""
from __future__ import annotations

import json
import re
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACH_DIR = ROOT / "Resources/Prototypes/_Fish/Achievements"
FTL_EN = ROOT / "Resources/Locale/en-US/_fish/achievements.ftl"
PROTO_DIR = ROOT / "Resources/Prototypes"
AUDIT_JSON = ROOT / "Resources/Docs/_Fish/AchievementsTriggerAudit.json"

# Reuse mappings from wire script
from wire_fish_achievements import (  # noqa: E402
    BOSS_TARGETS,
    EVENT_MAP,
    ITEM_MAP,
    JOB_MAP,
    PLAYTIME_THRESHOLDS,
    ach_id_to_key,
)

# ID fragment → (condition, params, progress_target?)
ID_OVERRIDES: dict[str, tuple[str, dict[str, str], int | None]] = {
    "soyboy": ("item-ingest", {"item": "DrinkSoyMilkCarton"}, 1),
    "jackpot": ("interaction", {"target": "ArcadePrize"}, 1),
    "tendrilexterminator": ("kill", {"target": "MobTendril"}, 1),
    "greentext": ("objective-complete", {"objective": "*"}, 1),
    "missioncomplete": ("objective-complete", {"objective": "*"}, 1),
    "ggghosts": ("became-ghost", {}, 1),
    "ghosttour": ("became-ghost", {}, 1),
    "fish": ("first-late-join", {}, 1),
    "lastclipstanding": ("kill", {}, 5),
    "bestmealofmylife": ("item-ingest", {"item": "FoodMeatHuman"}, 1),
    "plasmaperfume": ("explosion", {}, 1),
    "mostlyarmless": ("round-end-alive", {"shuttle": "emergency"}, 1),
    "blobperimeter": ("station-event", {"event": "BlobGameMode"}, 5),
    "clowncourt": ("interaction", {"target": "ClownRecorder"}, 30),
    "atmospoetry": ("interaction", {"target": "GasPipe"}, 40),
    "honkvasion": ("shuttle-arrive", {}, 3),
    "brigmedicbond": ("heal", {}, 10),
    "cesingularitybabysil": ("explosion", {}, 3),
    "loadoutmax": ("item-pickup", {}, 25),
    "labelmaker": ("craft", {"item": "Paper"}, 15),
    "firstshift": ("role-added", {"job": "Passenger"}, 3),
    "corridorsofpain": ("kill", {"requirePlayerVictim": "true"}, 3),
    "assistantvictory": ("kill", {"requirePlayerVictim": "true"}, 1),
    "killmekillmekillme": ("kill", {"tag": "NpcBoss"}, 1),
    "toofasttoofurious": ("kill", {"tag": "NpcBoss"}, 1),
    "honorarynukie": ("kill", {"tag": "NpcBoss"}, 1),
    "kickassandchewbubblegum": ("kill", {"tag": "NpcBoss"}, 1),
}

# Description phrase → prototype target (verified in repo)
DESC_TARGET_MAP: dict[str, str] = {
    "mulebot": "MobMule",
    "mule bot": "MobMule",
    "honk bot": "MobHonkBot",
    "honker": "MobHonkBot",
    "pod bay": "AirlockGlass",
    "arcade": "ArcadeComputer",
    "slot machine": "SlotMachine",
    "vending": "VendingMachine",
    "smoke machine": "SmokeMachine",
    "camera": "SurveillanceCamera",
    "turret": "WeaponEnergyTurret",
    "nuclear disk": "NukeDisk",
    "nuclear authentication": "NukeDisk",
    "supermatter": "Supermatter",
    "singularity": "SingularityGenerator",
    "tesla": "TeslaGenerator",
    "ai core": "PlayerStationAi",
    "ai upload": "AiUpload",
    "law board": "AiUpload",
    "cryo": "CryoPod",
    "cloning": "CloningPod",
    "telecomms": "TelecomServer",
    "recharger": "Recharger",
    "disposal": "DisposalUnit",
    "chapel": "Altar",
    "confessional": "Confessional",
    "holopad": "Holopad",
    "nuclear bomb": "NuclearBomb",
    "nuke": "NuclearBomb",
    "banana": "FoodBanana",
    "soymilk": "DrinkSoyMilkCarton",
    "soy milk": "DrinkSoyMilkCarton",
    "defibr": "Defibrillator",
    "surgery": "OperatingTable",
    "operating table": "OperatingTable",
    "revolver": "WeaponRevolver",
    "pulse rifle": "WeaponPulseRifle",
    "energy sword": "EnergySword",
    "stun baton": "Stunbaton",
    "fire axe": "FireAxe",
    "welder": "Welder",
    "analyzer": "GasAnalyzer",
    "trash": "TrashBag",
    "botany": "HydroponicsTray",
    "hydroponics": "HydroponicsTray",
    "lathe": "Protolathe",
    "autolathe": "Autolathe",
    "circuit imprinter": "CircuitImprinter",
    "techfab": "TechFab",
    "fishing": "FishingRod",
    "fishing rod": "FishingRod",
}

# Weapon keywords in description
WEAPON_KEYWORDS = {
    "revolver": "WeaponRevolver",
    "pulse rifle": "WeaponPulseRifle",
    "energy sword": "EnergySword",
    "laser": "WeaponLaserGun",
    "shotgun": "WeaponShotgun",
    "syringe gun": "WeaponSyringeGun",
    "stun baton": "Stunbaton",
    "fire axe": "FireAxe",
    "captain's sabre": "CaptainSabre",
    "sabre": "CaptainSabre",
    "energy gun": "WeaponEnergyGun",
    "disabler": "WeaponDisabler",
    "taser": "WeaponTaser",
    "bolas": "Bola",
    "crossbow": "WeaponCrossbow",
    "harpoon": "WeaponHarpoon",
    "spear": "Spear",
    "toolbox": "ToolboxEmergency",
    "fire extinguisher": "FireExtinguisher",
    "welder": "Welder",
    "stunprod": "Stunprod",
    "circular saw": "CircularSaw",
    "bone saw": "BoneSaw",
    "scalpel": "Scalpel",
    "defibrillator": "Defibrillator",
}

OBJECTIVE_PHRASE_MAP: dict[str, str] = {
    "escape shuttle": "EscapeShuttleObjective",
    "escape to centcomm": "EscapeShuttleObjective",
    "die a glorious death": "DieObjective",
    "steal": "StealObjective",
    "assassinate": "KillPersonObjective",
    "download": "DownloadObjective",
    "maroon": "MaroonObjective",
    "survive": "SurviveObjective",
    "debrain": "DebrainObjective",
}


@dataclass
class RefineResult:
    condition: str
    params: dict[str, str] = field(default_factory=dict)
    progress_target: int | None = None
    status: str = "fully_specific"
    reason: str = ""
    trigger: str = ""


# Boss-фрагменты в ID → kill params (NpcBoss — реальный tag в _Sunrise/tags.yml)
BOSS_ID_FRAGMENTS: dict[str, dict[str, str]] = {
    "blooddrunkminer": {"tag": "NpcBoss"},
    "blooddrunkminers": {"tag": "NpcBoss"},
    "bubblegum": {"tag": "NpcBoss"},
    "colossus": {"tag": "NpcBoss"},
    "drake": {"target": "MobDragonDungeon"},
    "hierophant": {"tag": "NpcBoss"},
    "legion": {"tag": "NpcBoss"},
    "spacedragon": {"target": "MobDragon"},
    "swarmbeacon": {"tag": "NpcBoss"},
    "tendril": {"target": "MobTendril"},
    "megafauna": {"tag": "NpcBoss"},
    "boss": {"tag": "NpcBoss"},
    "wendigo": {"tag": "NpcBoss"},
    "demonicfrostminer": {"tag": "NpcBoss"},
    "kinggoat": {"tag": "NpcBoss"},
    "swarmerbeacon": {"tag": "NpcBoss"},
    "swarmer": {"tag": "NpcBoss"},
    "thething": {"tag": "NpcBoss"},
    "thing": {"tag": "NpcBoss"},
    "killmekillmekillme": {"tag": "NpcBoss"},
    "toofasttoofurious": {"tag": "NpcBoss"},
    "honorarynukie": {"tag": "NpcBoss"},
    "kickassandchewbubblegum": {"tag": "NpcBoss"},
}

# ID-only: admin / minigame / нет SS14-механики
BLOCKED_ID_FRAGMENTS = (
    "contributor",
    "10bux",
    "blockstacker",
    "robustris",
    "guerrierdugelato",
    "chevalierdusorbet",
    "chapterii",
    "arcfiend",
    "ashockingdemise",
)


def extract_progress(text: str, default: int = 1) -> int:
    m = re.search(r"\b(\d+)\b", text)
    return int(m.group(1)) if m else default


def classify_by_id(ach_id: str, desc: str, protos: set[str]) -> RefineResult | None:
    key = ach_id_to_key(ach_id)
    dl = desc.lower()

    for frag in BLOCKED_ID_FRAGMENTS:
        if frag in key:
            return RefineResult("manual", {}, 1, "blocked", f"blocked:{frag}", "")

    # Boss kill/crusher/killed/slayer/exterminator по имени в ID
    for boss_frag, params in sorted(BOSS_ID_FRAGMENTS.items(), key=lambda x: -len(x[0])):
        if boss_frag in key and re.search(r"(killer|crusher|killed|slayer|exterminator|crush)", key):
            p = {k: v for k, v in params.items() if k != "target" or v in protos}
            if "target" in params and params["target"] not in protos and "tag" not in p:
                p["tag"] = "NpcBoss"
            return RefineResult("kill", p, 1, "fully_specific", f"boss-id:{boss_frag}", "KillReportedEvent")

    if "bosskiller" in key:
        return RefineResult("kill", {"tag": "NpcBoss"}, 1, "fully_specific", "bosskiller", "KillReportedEvent")

    for frag, (cond, params, pt) in ID_OVERRIDES.items():
        if frag in key:
            return RefineResult(cond, dict(params), pt, "fully_specific", f"id:{frag}", _trigger_for(cond))

    if "ghost" in key and "ghost" not in ("ghostrole",):
        return RefineResult("became-ghost", {}, 1, "fully_specific", "id:ghost", "MindRemovedMessage")

    if "slip" in key or key == "ijustcleanedthat":
        return RefineResult("slip-death", {}, 1, "fully_specific", "id:slip", "SlipEvent")

    if "gorefest" in key or "gibbed" in key or ("gib" in key and "gibber" not in key):
        return RefineResult("death", {}, 1, "fully_specific", "id:gibbed", "MobStateChangedEvent")

    if "survive" in key or "stillstanding" in key or "habitual" in key:
        m = re.search(r"(\d+)", desc)
        if m and "round" in dl:
            return RefineResult("counter", {"key": "rounds-survived"}, int(m.group(1)), "fully_specific", "id:survive-count", "RoundEndMessageEvent")
        return RefineResult("round-end-alive", {}, 1, "fully_specific", "id:survive", "RoundEndMessageEvent")

    if "honk" in key or "bestdriver" in key:
        pt = 100 if "100" in desc or "bestdriver" in key else extract_progress(desc, 1)
        horn = "BikeHorn" if "BikeHorn" in protos else None
        if horn:
            return RefineResult("interaction", {"target": horn}, pt, "fully_specific", "id:honk", "UserInteractHandEvent")
        return RefineResult("interaction", {}, pt, "blocked", "id:honk-no-proto", "UserInteractHandEvent")

    if "formatcomplete" in key or "ailaw" in key:
        target = next((p for p in ("AiUpload", "StationAiCore", "PlayerStationAi") if p in protos), None)
        if target:
            return RefineResult("interaction", {"target": target}, 1, "fully_specific", "id:ai-laws", "UserInteractHandEvent")

    if "captainslog" in key:
        target = next((p for p in ("PaperOffice", "Paper", "BookCaptainsLog") if p in protos), None)
        if target:
            return RefineResult("interaction", {"target": target}, 1, "fully_specific", "id:captains-log", "UserInteractHandEvent")

    if "crossingthehorizon" in key:
        # CE overspeed / horizon — ближайший аналог: atmos/explosion или singulo event
        return RefineResult("station-event", {"event": "Singularity"}, 1, "fully_specific", "id:ce-horizon", "GameRuleStartedEvent")

    if "davemymindisgoing" in key:
        target = next((p for p in ("AirlockGlass", "AirlockCommand", "AirlockEngineering") if p in protos), None)
        if target:
            return RefineResult("interaction", {"target": target}, 1, "fully_specific", "id:pod-bay", "UserInteractHandEvent")

    if "jackpot" in key or "arcade" in key or "pulsar" in key:
        target = next((p for p in ("ArcadeComputer", "SpaceArcade", "ArcadeBlockGame") if p in protos), None)
        if target:
            return RefineResult("interaction", {"target": target}, 1, "fully_specific", "id:arcade", "UserInteractHandEvent")

    if "nyoooom" in key or "roundandfull" in key:
        return RefineResult("death", {}, 1, "generic_but_valid", "id:speed-death", "MobStateChangedEvent")

    if "pleasejustendthepain" in key:
        return RefineResult("round-end-alive", {"shuttle": "emergency"}, 1, "fully_specific", "id:escape-pain", "RoundEndMessageEvent")

    if "mindthegap" in key:
        return RefineResult("shuttle-arrive", {}, 1, "fully_specific", "id:mind-gap", "FTLCompletedEvent")

    if "littlechickadee" in key or "100mdash" in key or "timeforbeer" in key:
        item = next((p for p in ("DrinkBeer", "DrinkAle", "DrinkBeerCan") if p in protos), None)
        if item:
            return RefineResult("item-ingest", {"item": item}, 10 if "ten" in dl else extract_progress(desc, 1), "fully_specific", "id:beer", "IngestedEvent")

    if "brownpants" in key:
        return RefineResult("role-added", {"job": "Captain"}, 1, "fully_specific", "id:captain-pants", "RoleAddedEvent")

    if "buttonpusher" in key:
        target = next((p for p in ("Button", "SignalButton", "DoorButton") if p in protos), None)
        if target:
            return RefineResult("interaction", {"target": target}, 1, "fully_specific", "id:button", "UserInteractHandEvent")

    if "hesdeadjim" in key:
        return RefineResult("surgery", {}, 1, "fully_specific", "id:bones", "SurgeryStepCompleteEvent")

    if "bearhug" in key:
        return RefineResult("kill", {}, 1, "generic_but_valid", "id:bearhug", "KillReportedEvent")

    if "banned" in key:
        return RefineResult("death", {}, 1, "generic_but_valid", "id:banned", "MobStateChangedEvent")

    if "yourlifebeforeyoureyes" in key:
        return RefineResult("explosion", {}, 1, "fully_specific", "id:space-debris", "SunriseExplosionEvent")

    if "featofstrength" in key:
        target = next((p for p in ("SingularityGenerator", "TeslaGenerator", "GravGenerator") if p in protos), None)
        if target:
            return RefineResult("interaction", {"target": target}, 1, "fully_specific", "id:feat-strength", "UserInteractHandEvent")

    if "veryimportantpiscis" in key:
        return RefineResult("round-end-alive", {}, 1, "fully_specific", "id:piscis", "RoundEndMessageEvent")

    if "survivalofthefittest" in key:
        return RefineResult("gun-shot", {}, 1, "generic_but_valid", "id:survival-gun", "GunShotEvent")

    if "rat" in key and ("kill" in key or "killed" in key or "killer" in key):
        target = next((p for p in ("MobRat", "MobMouse", "MobCrateRat") if p in protos), None)
        if target:
            return RefineResult("kill", {"target": target}, extract_progress(desc, 1), "fully_specific", "id:rat-kill", "KillReportedEvent")

    # Job в ID (PlayAsChef etc.)
    for job_phrase, job_id in JOB_MAP.items():
        token = job_phrase.replace(" ", "")
        if token in key.replace("_", "").replace("-", ""):
            return RefineResult("role-added", {"job": job_id}, extract_progress(desc, 1), "fully_specific", f"id:job:{job_id}", "RoleAddedEvent")

    return None


def load_ftl() -> dict[str, str]:
    loc: dict[str, str] = {}
    if not FTL_EN.exists():
        return loc
    for line in FTL_EN.read_text(encoding="utf-8").splitlines():
        m = re.match(r"^([\w-]+)\s*=\s*(.+)$", line.strip())
        if m:
            loc[m.group(1)] = m.group(2).strip()
    return loc


def ftl_key(ach_id: str, kind: str = "desc") -> str:
    suffix = "-desc" if kind == "desc" else "-name"
    if ach_id.startswith("FishAch_"):
        return f"achievement-fishach_{ach_id[8:].lower()}{suffix}"
    if ach_id.startswith("FishAch"):
        return f"achievement-fish-{ach_id[7:].lower().replace('_', '-')}{suffix}"
    return f"achievement-{ach_id.lower()}{suffix}"


def build_prototype_index() -> set[str]:
    protos: set[str] = set()
    for path in PROTO_DIR.rglob("*.yml"):
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        for m in re.finditer(r"^\s+id:\s+(\S+)", text, re.M):
            protos.add(m.group(1))
    return protos


def resolve_proto(phrase: str, protos: set[str], candidates: dict[str, str]) -> str | None:
    phrase_l = phrase.lower()
    if len(phrase_l) < 4:
        return None
    if phrase in candidates:
        pid = candidates[phrase_l]
        if pid in protos:
            return pid
    # direct id match
    for token in re.findall(r"[A-Za-z][A-Za-z0-9]+", phrase):
        for pid in protos:
            if pid.lower() == token.lower():
                return pid
    # substring scoring
    best: tuple[int, str] | None = None
    for pid in protos:
        pl = pid.lower()
        if len(pl) < 4:
            continue
        if phrase_l in pl or (len(pl) >= 4 and pl in phrase_l):
            score = len(phrase_l)
            if best is None or score > best[0]:
                best = (score, pid)
    return best[1] if best else None


def find_target_in_text(text: str, protos: set[str]) -> str | None:
    tl = text.lower()
    for phrase, pid in sorted(DESC_TARGET_MAP.items(), key=lambda x: -len(x[0])):
        if phrase in tl and pid in protos:
            return pid
    # try camelCase tokens from id-like words
    for word in re.findall(r"[A-Z][a-z]+|[a-z]+", text):
        if len(word) < 4:
            continue
        hit = resolve_proto(word, protos, DESC_TARGET_MAP)
        if hit:
            return hit
    return None


def find_weapon_in_text(text: str, protos: set[str]) -> str | None:
    tl = text.lower()
    for phrase, pid in sorted(WEAPON_KEYWORDS.items(), key=lambda x: -len(x[0])):
        if phrase in tl and pid in protos:
            return pid
    return None


def find_job_in_text(text: str) -> str | None:
    tl = text.lower()
    for phrase, job_id in sorted(JOB_MAP.items(), key=lambda x: -len(x[0])):
        if phrase in tl:
            return job_id
    return None


def find_objective_in_text(text: str, protos: set[str]) -> str | None:
    tl = text.lower()
    for phrase, obj_id in OBJECTIVE_PHRASE_MAP.items():
        if phrase in tl and obj_id in protos:
            return obj_id
    # scan objective prototypes by name field in yaml - simplified: known ids
    for pid in protos:
        if "Objective" in pid and pid.lower().replace("objective", "") in tl.replace(" ", ""):
            return pid
    return None


def refine(ach_id: str, category: str, desc: str, name: str, protos: set[str]) -> RefineResult:
    by_id = classify_by_id(ach_id, desc, protos)
    if by_id is not None:
        return by_id

    key = ach_id_to_key(ach_id)
    dl = desc.lower()
    nl = name.lower()
    combined = f"{nl} {dl} {key}"

    # Playtime
    for frag, mins in PLAYTIME_THRESHOLDS.items():
        if frag in key:
            return RefineResult(
                "playtime-minutes",
                {"threshold": str(mins)},
                1,
                "fully_specific",
                "playtime",
                "PlayTimeTrackingManager",
            )

    # Ghost — only explicit ghost transition
    if re.search(r"\bbecome a ghost\b|\bghost\b", dl) and "ghost" in key:
        return RefineResult("became-ghost", {}, 1, "fully_specific", "ghost", "MindRemovedMessage")

    # Late join
    if "late join" in dl or key == "fish":
        return RefineResult("first-late-join", {}, 1, "fully_specific", "latejoin", "PlayerSpawnCompleteEvent")

    # Shuttle / centcomm / escape
    if any(x in dl for x in ("escape shuttle", "central command", "centcomm", "cent comm")):
        if "die" in dl or "death" in dl:
            return RefineResult("death", {"shuttle": "emergency"}, 1, "fully_specific", "death-shuttle", "MobStateChangedEvent")
        if "arrive" in dl or "tourist" in key:
            return RefineResult("shuttle-arrive", {}, 1, "fully_specific", "shuttle", "FTLCompletedEvent")
        return RefineResult("round-end-alive", {"shuttle": "emergency"}, 1, "fully_specific", "round-end-shuttle", "RoundEndMessageEvent")

    # Survive rounds counter
    m = re.search(r"survive (\d+) round", dl)
    if m:
        return RefineResult("counter", {"key": "rounds-survived"}, int(m.group(1)), "fully_specific", "survive-count", "RoundEndMessageEvent")

    if "survive" in dl and "round" in dl:
        return RefineResult("round-end-alive", {}, 1, "fully_specific", "survive", "RoundEndMessageEvent")

    # Objectives
    if "complete your objectives" in dl or "mission complete" in dl or "greentext" in key:
        return RefineResult("objective-complete", {"objective": "*"}, 1, "fully_specific", "all-objectives", "RoundEndMessageEvent")

    obj = find_objective_in_text(combined, protos)
    if obj and ("objective" in dl or "complete" in dl or "mission" in dl):
        return RefineResult("objective-complete", {"objective": obj}, 1, "fully_specific", f"obj:{obj}", "RoundEndMessageEvent")

    if desc.startswith("Complete the ") or "complete the " in dl:
        job = find_job_in_text(dl)
        if job:
            return RefineResult("role-added", {"job": job}, 1, "fully_specific", f"job-complete:{job}", "RoleAddedEvent")
        if obj:
            return RefineResult("objective-complete", {"objective": obj}, 1, "fully_specific", "objective", "RoundEndMessageEvent")

    # Antag
    if "as an antagonist" in dl or "as a traitor" in dl or "as antag" in dl:
        if "objective" in dl:
            return RefineResult("objective-complete", {"objective": "*"}, 1, "fully_specific", "antag-objective", "RoundEndMessageEvent")
        return RefineResult("antag-selected", {}, 1, "generic_but_valid", "antag", "AfterAntagEntitySelectedEvent")

    # Ingest / drink / eat
    for item_phrase, proto in ITEM_MAP.items():
        if item_phrase in dl and proto in protos:
            return RefineResult("item-ingest", {"item": proto}, 1, "fully_specific", f"ingest:{proto}", "IngestedEvent")
    if re.search(r"\b(drank|drink|drinking|ate|eat|eating|consume|consumed|swallow|ingest|meal)\b", dl):
        if "blooddrunk" in key or "blood drunk" in dl:
            pass  # не путать с boss BloodDrunkMiner
        else:
            item = find_target_in_text(combined, protos)
            if item and item.startswith(("Food", "Drink", "Reagent")):
                return RefineResult("item-ingest", {"item": item}, 1, "fully_specific", f"ingest:{item}", "IngestedEvent")
            return RefineResult("item-ingest", {}, extract_progress(dl), "blocked", "ingest-no-item", "IngestedEvent")

    # Slip death
    if "slip" in dl and ("death" in dl or "die" in dl):
        return RefineResult("slip-death", {}, 1, "fully_specific", "slip-death", "SlipEvent+MobStateChangedEvent")

    # Surgery / defibr / medical
    if "defibr" in dl or "shock" in dl and "heart" in dl:
        return RefineResult("defibrillate", {}, extract_progress(dl, 1), "fully_specific", "defibr", "TargetDefibrillatedEvent")
    if "surgery" in dl or "operat" in dl or "scalpel" in dl or "saw" in dl and "bone" in dl:
        return RefineResult("surgery", {}, extract_progress(dl, 1), "fully_specific", "surgery", "SurgeryStepCompleteEvent")
    if any(w in dl for w in ("heal", "patch", "treat", "revive", "clone", "medical")):
        pt = extract_progress(dl, 1)
        return RefineResult("heal", {}, pt, "generic_but_valid" if pt > 1 else "fully_specific", "heal", "DamageChangedEvent")

    # Gun / shoot
    weapon = find_weapon_in_text(combined, protos)
    if weapon and any(w in dl for w in ("shoot", "fire", "shot", "gun", "revolver", "rifle")):
        pt = extract_progress(dl, 1)
        return RefineResult("gun-shot", {"weapon": weapon}, pt, "fully_specific", f"gun:{weapon}", "GunShotEvent")

    # Boss kill
    for fragment, mapping in BOSS_TARGETS.items():
        if fragment in key and any(w in key for w in ("killer", "crusher", "killed", "kill", "slay")):
            return RefineResult("kill", dict(mapping), 1, "fully_specific", f"boss:{fragment}", "KillReportedEvent")
    if "bosskiller" in key or ("boss" in dl and "kill" in dl):
        p = {"tag": "NpcBoss"} if "NpcBoss" in str(protos) else {}
        return RefineResult("kill", p, 1, "fully_specific" if p else "generic_suspicious", "boss", "KillReportedEvent")

    # Kill
    if "kill" in dl or (category == "FishAchCombat" and "kill" in key):
        params: dict[str, str] = {}
        target = find_target_in_text(combined, protos)
        if target and target.startswith("Mob"):
            params["target"] = target
        elif "player" in dl or "human" in dl or "crew" in dl or "people" in dl:
            params["tag"] = "Humanoid"
        if weapon and ("with" in dl or "using" in dl):
            params["weapon"] = weapon
        status = "fully_specific" if params else "generic_suspicious"
        return RefineResult("kill", params, extract_progress(dl, 1), status, "kill", "KillReportedEvent")

    # Death
    if any(w in dl for w in ("die", "death", "suicide", "killed", "perish", "mortal coil")):
        params: dict[str, str] = {}
        if "shuttle" in dl or "escape" in dl:
            params["shuttle"] = "emergency"
        job = find_job_in_text(dl)
        if job and ("as" in dl or "role" in dl):
            params["job"] = job
        if "explosion" in dl or "explode" in dl:
            return RefineResult("explosion", {}, 1, "fully_specific", "death-explosion", "SunriseExplosionEvent")
        status = "fully_specific" if params else "generic_but_valid"
        return RefineResult("death", params, extract_progress(dl, 1), status, "death", "MobStateChangedEvent")

    # Station events
    for ev_key, ev_id in EVENT_MAP.items():
        if ev_key in dl or ev_key in key:
            return RefineResult("station-event", {"event": ev_id}, extract_progress(dl, 1), "fully_specific", f"event:{ev_id}", "GameRuleStartedEvent")

    if "explosion" in dl or "explode" in dl or "boom" in dl:
        return RefineResult("explosion", {}, extract_progress(dl, 1), "generic_but_valid", "explosion", "SunriseExplosionEvent")

    # Craft
    if any(w in dl for w in ("fabricat", "craft", "construct", "create", "build")):
        params: dict[str, str] = {}
        item = find_target_in_text(combined, protos)
        if item:
            params["item"] = item
        pt = extract_progress(dl, 1)
        status = "fully_specific" if params else "generic_suspicious"
        return RefineResult("craft", params, pt, status, "craft", "ItemConstructionCreated")

    # Jobs / roles
    job = find_job_in_text(combined)
    if job:
        pt = extract_progress(dl, 1)
        return RefineResult("role-added", {"job": job}, pt, "fully_specific", f"job:{job}", "RoleAddedEvent")

    if category == "FishAchRoles":
        return RefineResult("role-added", {}, 1, "blocked", "role-no-job", "RoleAddedEvent")

    # Equip / wear
    if any(w in dl for w in ("wear", "equip", "pick up", "don", "loadout")):
        params: dict[str, str] = {}
        item = find_target_in_text(combined, protos)
        if item:
            params["item"] = item
        pt = extract_progress(dl, 1)
        status = "fully_specific" if params else "generic_suspicious"
        return RefineResult("item-pickup", params, pt, status, "equip", "DidEquipEvent")

    # Damage
    if "damage" in dl or "brain" in key:
        return RefineResult("damage-dealt", {}, extract_progress(dl, 10), "generic_but_valid", "damage", "DamageChangedEvent")

    # Interaction — must have target when possible
    if any(w in dl for w in ("interact", "click", "use", "open", "touch", "press", "push", "pull", "honk")):
        params: dict[str, str] = {}
        target = find_target_in_text(combined, protos)
        if target:
            params["target"] = target
        pt = extract_progress(dl, 1)
        if params:
            return RefineResult("interaction", params, pt, "fully_specific", f"interact:{target}", "UserInteractHandEvent")
        # Try reclassify from id
        if "honk" in key:
            return RefineResult("interaction", {"target": "BikeHorn"}, pt, "fully_specific", "honk", "UserInteractHandEvent")
        return RefineResult("interaction", {}, pt, "blocked", "interaction-no-target", "UserInteractHandEvent")

    # Round end fallback for survival category only when desc mentions end/survive
    if category == "FishAchSurvival" and any(w in dl for w in ("survive", "end", "alive", "round")):
        return RefineResult("round-end-alive", {}, 1, "generic_but_valid", "survival", "RoundEndMessageEvent")

    # Last resort — blocked, not generic interaction
    return RefineResult("interaction", {}, 1, "blocked", "unmapped", "UserInteractHandEvent")


def _trigger_for(condition: str) -> str:
    return {
        "kill": "KillReportedEvent",
        "death": "MobStateChangedEvent",
        "interaction": "UserInteractHandEvent",
        "heal": "DamageChangedEvent",
        "craft": "ItemConstructionCreated",
        "item-pickup": "DidEquipEvent",
        "item-ingest": "IngestedEvent",
        "role-added": "RoleAddedEvent",
        "round-end-alive": "RoundEndMessageEvent",
        "objective-complete": "RoundEndMessageEvent",
        "station-event": "GameRuleStartedEvent",
        "explosion": "SunriseExplosionEvent",
        "shuttle-arrive": "FTLCompletedEvent",
        "became-ghost": "MindRemovedMessage",
        "defibrillate": "TargetDefibrillatedEvent",
        "surgery": "SurgeryStepCompleteEvent",
        "gun-shot": "GunShotEvent",
        "examine": "ExaminedEvent",
        "singularity-consumed": "EventHorizonConsumedEntityEvent",
        "succumb": "CritSuccumbEvent",
        "emote": "EmoteEvent",
        "ai-law-changes": "SiliconLawUpdaterComponent/EntInsertedIntoContainerMessage",
        "reagent-metabolize": "SolutionContainerChangedEvent",
        "chasm-fall": "ChasmFallingComponent/ComponentInit",
    }.get(condition, condition)


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
        if k == "requirePlayerVictim":
            continue  # YAML field, not conditionParam
        lines.append(f"{indent}  {k}: {v}")
    return "\n".join(lines) + "\n"


def refine_file(path: Path, ftl: dict[str, str], protos: set[str], stats: dict, audit: list) -> None:
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
        category = data.get("category", "FishAchMisc")
        desc = ftl.get(ftl_key(ach_id).replace("-desc", "-desc"), "")
        if not desc or desc.endswith("-desc"):
            desc = ftl.get(ftl_key(ach_id), data.get("description", ""))
        desc = ftl.get(ftl_key(ach_id), desc)
        name = ftl.get(ftl_key(ach_id, "name"), ach_id)

        result = refine(ach_id, category, desc, name, protos)
        stats["refined"] += 1
        stats["by_status"][result.status] = stats["by_status"].get(result.status, 0) + 1
        stats["by_condition"][result.condition] = stats["by_condition"].get(result.condition, 0) + 1
        if result.params:
            stats["with_params"] += 1
        if result.status == "blocked":
            stats["blocked"] += 1

        audit.append(
            {
                "id": ach_id,
                "condition": result.condition,
                "trigger": result.trigger or _trigger_for(result.condition),
                "conditionParams": result.params,
                "source": data.get("category", ""),
                "status": result.status,
                "reason": result.reason,
                "description": desc[:120],
            }
        )

        lines = block.splitlines()
        new_lines = []
        skip_cp = False
        require_pvp = result.params.pop("requirePlayerVictim", None)
        for line in lines:
            if re.match(r"^  condition:", line):
                new_lines.append(f"  condition: {result.condition}")
                skip_cp = False
                continue
            if re.match(r"^  conditionParams:", line):
                skip_cp = True
                continue
            if skip_cp and re.match(r"^    \w+:", line):
                continue
            if skip_cp and re.match(r"^  \w", line):
                skip_cp = False
            if re.match(r"^  allowGenericTrigger:", line):
                continue
            if re.match(r"^  progressTarget:", line):
                if result.progress_target:
                    new_lines.append(f"  progressTarget: {result.progress_target}")
                else:
                    new_lines.append(line)
                continue
            if re.match(r"^  requirePlayerVictim:", line):
                if require_pvp == "true":
                    new_lines.append("  requirePlayerVictim: true")
                continue
            new_lines.append(line)

        rebuilt = "\n".join(new_lines)
        params_str = format_params(result.params)
        insert = f"  condition: {result.condition}\n"
        if params_str:
            rebuilt = rebuilt.replace(insert, insert + params_str, 1)
        elif result.status == "generic_but_valid":
            rebuilt = rebuilt.replace(insert, insert + "  allowGenericTrigger: true\n", 1)
        elif result.status == "generic_suspicious":
            rebuilt = rebuilt.replace(insert, insert + "  allowGenericTrigger: true\n", 1)

        out_parts.append(rebuilt)

    path.write_text("".join(out_parts), encoding="utf-8")


def main() -> None:
    ftl = load_ftl()
    protos = build_prototype_index()
    stats: dict = {
        "refined": 0,
        "with_params": 0,
        "blocked": 0,
        "by_status": {},
        "by_condition": {},
    }
    audit: list = []

    for yml in sorted(ACH_DIR.glob("*.yml")):
        if yml.name == "categories.yml":
            continue
        refine_file(yml, ftl, protos, stats, audit)

    AUDIT_JSON.write_text(json.dumps(audit, indent=2, ensure_ascii=False), encoding="utf-8")
    manifest = ROOT / "Resources/Docs/_Fish/AchievementsRefineManifest.json"
    manifest.write_text(json.dumps(stats, indent=2), encoding="utf-8")
    print(json.dumps(stats, indent=2))
    print(f"audit -> {AUDIT_JSON}")


if __name__ == "__main__":
    main()
