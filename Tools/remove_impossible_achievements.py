#!/usr/bin/env python3
"""Remove blocked/manual achievements from YAML, FTL, audit, and mapping."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACH_DIR = ROOT / "Resources/Prototypes/_Fish/Achievements"
AUDIT = ROOT / "Resources/Docs/_Fish/AchievementsTriggerAudit.json"
MAPPING = ROOT / "Resources/Docs/_Fish/AchievementsSs13Mapping.json"
FTL_PATHS = [
    ROOT / "Resources/Locale/en-US/_fish/achievements.ftl",
    ROOT / "Resources/Locale/ru-RU/_fish/achievements.ftl",
]
REMOVED_LOG = ROOT / "Resources/Docs/_Fish/AchievementsRemoved.json"


def load_remove_ids() -> set[str]:
    audit = json.loads(AUDIT.read_text(encoding="utf-8"))
    ids = {
        row["id"]
        for row in audit
        if row.get("status") == "blocked" or row.get("condition") == "manual"
    }
    # YAML с комментарием blocked, ещё не в audit
    block_pat = re.compile(r"- type: achievement\r?\n[\s\S]*?(?=\r?\n- type: |\Z)")
    for path in sorted(ACH_DIR.glob("*.yml")):
        if path.name == "categories.yml":
            continue
        text = path.read_text(encoding="utf-8")
        for block in block_pat.findall(text):
            if "# blocked:" not in block:
                continue
            m = re.search(r"^  id: (FishAch\w+)", block, re.M)
            if m:
                ids.add(m.group(1))
    return ids


def remove_yaml_blocks(ids: set[str]) -> int:
    removed = 0
    block_pat = re.compile(r"- type: achievement\r?\n[\s\S]*?(?=\r?\n- type: |\Z)")
    for path in sorted(ACH_DIR.glob("*.yml")):
        if path.name == "categories.yml":
            continue
        text = path.read_text(encoding="utf-8")
        kept: list[str] = []
        for block in block_pat.findall(text):
            m = re.search(r"^  id: (FishAch\w+)", block, re.M)
            if m and m.group(1) in ids:
                removed += 1
                continue
            kept.append(block)
        new_text = "".join(kept)
        if new_text != text:
            path.write_text(new_text, encoding="utf-8")
    return removed


def remove_ftl_entries(ids: set[str]) -> int:
    removed = 0
    for ftl in FTL_PATHS:
        if not ftl.exists():
            continue
        lines = ftl.read_text(encoding="utf-8").splitlines(keepends=True)
        out: list[str] = []
        skip_prefixes: set[str] = set()
        for ach_id in ids:
            frag = ach_id.replace("FishAch_", "achievement-fishach_", 1).lower()
            skip_prefixes.add(frag)
        i = 0
        while i < len(lines):
            line = lines[i]
            drop = False
            for prefix in skip_prefixes:
                if line.startswith(prefix):
                    drop = True
                    removed += 1
                    break
            if not drop:
                out.append(line)
            i += 1
        ftl.write_text("".join(out), encoding="utf-8")
    return removed


def update_audit(ids: set[str]) -> list[dict]:
    audit = json.loads(AUDIT.read_text(encoding="utf-8"))
    kept = [row for row in audit if row["id"] not in ids]
    removed_rows = [row for row in audit if row["id"] in ids]
    AUDIT.write_text(json.dumps(kept, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return removed_rows


def update_mapping(ids: set[str]) -> None:
    if not MAPPING.exists():
        return
    mapping = json.loads(MAPPING.read_text(encoding="utf-8"))
    kept = [row for row in mapping if row.get("id") not in ids]
    MAPPING.write_text(json.dumps(kept, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def main() -> None:
    ids = load_remove_ids()
    print(f"Removing {len(ids)} blocked/manual achievements")
    yaml_removed = remove_yaml_blocks(ids)
    ftl_removed = remove_ftl_entries(ids)
    removed_rows = update_audit(ids)
    update_mapping(ids)

    REMOVED_LOG.write_text(
        json.dumps(
            {
                "count": len(ids),
                "ids": sorted(ids),
                "reason": "blocked or manual — no Fish/Sunrise mechanic after SS13 research",
            },
            indent=2,
            ensure_ascii=False,
        )
        + "\n",
        encoding="utf-8",
    )

    print(f"  YAML blocks removed: {yaml_removed}")
    print(f"  FTL lines removed: {ftl_removed}")
    print(f"  Audit entries removed: {len(removed_rows)}")
    print(f"  Remaining audit: {len(json.loads(AUDIT.read_text(encoding='utf-8')))}")


if __name__ == "__main__":
    main()
