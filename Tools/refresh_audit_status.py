#!/usr/bin/env python3
"""Refresh audit status/trigger fields from achievement YAML + known handlers."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACH_DIR = ROOT / "Resources/Prototypes/_Fish/Achievements"
AUDIT = ROOT / "Resources/Docs/_Fish/AchievementsTriggerAudit.json"
FTL = ROOT / "Resources/Locale/en-US/_fish/achievements.ftl"

HANDLED = {
    "manual", "round-survive", "round-end-alive", "death", "slip-death", "job-play",
    "antag-win", "kill", "damage-dealt", "item-pickup", "interaction", "craft", "heal",
    "explosion", "shuttle-arrive", "station-event", "first-late-join", "counter",
    "became-ghost", "item-ingest", "antag-selected", "objective-complete", "playtime-minutes",
    "role-added", "defibrillate", "surgery", "gun-shot", "examine", "singularity-consumed",
    "succumb", "emote", "ai-law-changes", "reagent-metabolize", "chasm-fall", "gavel-strike",
    "tile-pry", "gibbed",
}

INHERENT = {
    "became-ghost", "singularity-consumed", "succumb", "first-late-join", "antag-win",
    "round-end-alive", "round-survive", "shuttle-arrive", "chasm-fall", "gibbed",
}

TRIGGER = {
    "gavel-strike": "GavelSystem.OnHit",
    "tile-pry": "TileToolDoAfterEvent",
    "gibbed": "BeingGibbedEvent",
    "singularity-consumed": "EventHorizonConsumedEntityEvent",
    "chasm-fall": "ChasmFallingComponent/ComponentInit",
    "examine": "ExaminedEvent",
    "reagent-metabolize": "SolutionContainerChangedEvent",
    "succumb": "CritSuccumbEvent",
    "emote": "EmoteEvent",
    "objective-complete": "RoundEndMessageEvent",
    "became-ghost": "MindRemovedMessage",
    "item-ingest": "IngestedEvent",
    "role-added": "RoleAddedEvent",
    "kill": "KillReportedEvent",
    "gun-shot": "GunShotEvent",
    "defibrillate": "TargetDefibrillatedEvent",
    "surgery": "SurgeryStepCompleteEvent",
    "interaction": "UserInteractHandEvent",
    "death": "MobStateChangedEvent",
    "station-event": "GameRuleStartedEvent",
    "shuttle-arrive": "FTLCompletedEvent",
    "explosion": "SunriseExplosionEvent",
    "craft": "ItemConstructionCreated",
    "heal": "DamageChangedEvent",
    "playtime-minutes": "AchievementPlaytimeSystem",
}


def ftl_desc(ach_id: str) -> str:
    key = ach_id.replace("FishAch_", "achievement-fishach_", 1).lower() + "-desc"
    for line in FTL.read_text(encoding="utf-8").splitlines():
        if line.startswith(f"{key} = "):
            return line.split(" = ", 1)[1].strip()
    return ""


def parse_yaml() -> dict[str, dict]:
    out: dict[str, dict] = {}
    for path in sorted(ACH_DIR.glob("*.yml")):
        text = path.read_text(encoding="utf-8")
        for block in re.split(r"(?=\n- type: achievement)", text)[1:]:
            aid_m = re.search(r"^  id: (FishAch_\w+)", block, re.M)
            if not aid_m:
                continue
            aid = aid_m.group(1)
            cond_m = re.search(r"^  condition: (\S+)", block, re.M)
            cond = cond_m.group(1) if cond_m else "manual"
            blocked = "# blocked:" in block or cond == "manual"
            ag = "allowGenericTrigger: true" in block
            params: dict[str, str] = {}
            pm = re.search(r"  conditionParams:\n((?:    \w+: .+\n)+)", block)
            if pm:
                for line in pm.group(1).splitlines():
                    k, v = line.strip().split(": ", 1)
                    params[k] = v
            out[aid] = {
                "condition": cond,
                "params": params,
                "blocked": blocked,
                "allowGeneric": ag,
            }
    return out


def classify(entry: dict) -> tuple[str, str]:
    if entry["blocked"] or entry["condition"] not in HANDLED:
        return "blocked", entry.get("reason", "unmapped")
    if entry["allowGeneric"]:
        return "generic_but_valid", "allowGenericTrigger"
    if entry["condition"] == "role-added" and not entry["params"].get("job"):
        return "blocked", "role-no-job"
    if entry["params"]:
        return "fully_specific", f"params:{','.join(sorted(entry['params']))}"
    if entry["condition"] in INHERENT:
        return "fully_specific", f"id:{entry['condition']}"
    if entry["condition"] == "interaction":
        return "blocked", "interaction-no-target"
    return "generic_but_valid", entry["condition"]


def main() -> None:
    yaml = parse_yaml()
    audit = json.loads(AUDIT.read_text(encoding="utf-8"))
    by_id = {r["id"]: r for r in audit}
    for aid, y in yaml.items():
        row = by_id.get(aid, {"id": aid})
        row["condition"] = y["condition"]
        row["conditionParams"] = y["params"]
        row["trigger"] = TRIGGER.get(y["condition"], y["condition"])
        row["description"] = ftl_desc(aid)
        if y["blocked"]:
            row["status"] = "blocked"
            if not row.get("reason") or row["reason"].startswith("id:"):
                row["reason"] = "documented-blocked"
        else:
            status, reason = classify(y)
            row["status"] = status
            row["reason"] = reason
        by_id[aid] = row

    AUDIT.write_text(json.dumps(list(by_id.values()), indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    counts: dict[str, int] = {}
    for r in by_id.values():
        counts[r.get("status", "?")] = counts.get(r.get("status", "?"), 0) + 1
    print("Audit refreshed:", counts)


if __name__ == "__main__":
    main()
