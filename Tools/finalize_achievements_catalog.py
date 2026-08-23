#!/usr/bin/env python3
"""Remove blocked/manual/fake achievements; fix seed; rebuild audit references."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACH_DIR = ROOT / "Resources/Prototypes/_Fish/Achievements"
AUDIT = ROOT / "Resources/Docs/_Fish/AchievementsTriggerAudit.json"
MAPPING = ROOT / "Resources/Docs/_Fish/AchievementsSs13Mapping.json"
REMOVED_LOG = ROOT / "Resources/Docs/_Fish/AchievementsRemoved.json"
FTL_PATHS = [
    ROOT / "Resources/Locale/en-US/_fish/achievements.ftl",
    ROOT / "Resources/Locale/ru-RU/_fish/achievements.ftl",
]
SEED = ACH_DIR / "seed.yml"

BLOCK_RE = re.compile(r"- type: achievement\r?\n[\s\S]*?(?=\r?\n- type: |\Z)")


def parse_block(block: str) -> dict:
    aid_m = re.search(r"^  id: (FishAch\w+)", block, re.M)
    cond_m = re.search(r"^  condition: (\S+)", block, re.M)
    obj_m = re.search(r"^    objective: (.+)$", block, re.M)
    return {
        "id": aid_m.group(1) if aid_m else None,
        "condition": cond_m.group(1) if cond_m else "manual",
        "objective": obj_m.group(1).strip() if obj_m else None,
        "blocked_comment": "# blocked:" in block,
    }


def load_remove_ids() -> set[str]:
    audit = json.loads(AUDIT.read_text(encoding="utf-8"))
    ids = {
        row["id"]
        for row in audit
        if row.get("status") == "blocked" or row.get("condition") == "manual"
    }
    for path in sorted(ACH_DIR.glob("*.yml")):
        if path.name == "categories.yml":
            continue
        for block in BLOCK_RE.findall(path.read_text(encoding="utf-8")):
            meta = parse_block(block)
            if meta["id"] and meta["blocked_comment"]:
                ids.add(meta["id"])
            if meta["condition"] == "objective-complete" and meta["objective"] in ("*", "Objectives"):
                ids.add(meta["id"])
    # SS13 research: нет механики ingest supermatter
    ids.add("FishAch_BestMealofMyLife")
    return ids


def rewrite_yaml(ids: set[str]) -> int:
    removed = 0
    for path in sorted(ACH_DIR.glob("*.yml")):
        if path.name == "categories.yml":
            continue
        text = path.read_text(encoding="utf-8")
        kept: list[str] = []
        for block in BLOCK_RE.findall(text):
            meta = parse_block(block)
            if meta["id"] in ids:
                removed += 1
                continue
            if not block.endswith("\n"):
                block += "\n"
            kept.append(block)
        new_text = "".join(kept)
        if new_text != text:
            path.write_text(new_text, encoding="utf-8")
    return removed


def fix_seed() -> None:
    text = SEED.read_text(encoding="utf-8")
    text = text.replace(
        "  condition: interaction\n  progressTarget: 1\n  oncePerRound: true\n  order: 10",
        "  condition: first-late-join\n  progressTarget: 1\n  oncePerRound: true\n  order: 10",
        1,
    )
    text = text.replace(
        "  condition: interaction\n  progressTarget: 1\n  oncePerRound: true\n  minRoundSeconds: 120\n  order: 30",
        "  condition: slip-death\n  progressTarget: 1\n  oncePerRound: true\n  minRoundSeconds: 120\n  order: 30",
        1,
    )
    SEED.write_text(text, encoding="utf-8")


def remove_ftl(ids: set[str]) -> int:
    removed = 0
    prefixes = {aid.replace("FishAch_", "achievement-fishach_", 1).lower() for aid in ids}
    # seed ids without underscore
    for aid in ids:
        if not aid.startswith("FishAch_"):
            frag = aid.replace("FishAch", "achievement-fish-", 1)
            prefixes.add(re.sub(r"([a-z])([A-Z])", r"\1-\2", frag).lower())
    for ftl in FTL_PATHS:
        if not ftl.exists():
            continue
        out: list[str] = []
        for line in ftl.read_text(encoding="utf-8").splitlines(keepends=True):
            if any(line.startswith(p) for p in prefixes):
                removed += 1
                continue
            out.append(line)
        ftl.write_text("".join(out), encoding="utf-8")
    return removed


def update_json(ids: set[str]) -> None:
    audit = json.loads(AUDIT.read_text(encoding="utf-8"))
    AUDIT.write_text(
        json.dumps([r for r in audit if r["id"] not in ids], indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    if MAPPING.exists():
        mapping = json.loads(MAPPING.read_text(encoding="utf-8"))
        MAPPING.write_text(
            json.dumps([r for r in mapping if r.get("id") not in ids], indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )
    REMOVED_LOG.write_text(
        json.dumps(
            {
                "count": len(ids),
                "ids": sorted(ids),
                "reason": "blocked/manual/fake objective — no Fish mechanic after SS13 research",
            },
            indent=2,
            ensure_ascii=False,
        )
        + "\n",
        encoding="utf-8",
    )


def main() -> None:
    ids = load_remove_ids()
    print(f"Removing {len(ids)} achievements")
    yaml_removed = rewrite_yaml(ids)
    ftl_removed = remove_ftl(ids)
    fix_seed()
    update_json(ids)
    print(f"  YAML blocks removed: {yaml_removed}")
    print(f"  FTL lines removed: {ftl_removed}")


if __name__ == "__main__":
    main()
