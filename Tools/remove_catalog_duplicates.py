#!/usr/bin/env python3
"""Remove catalog-duplicate flavor achievements (same trigger as sibling, filler only)."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACH_DIR = ROOT / "Resources/Prototypes/_Fish/Achievements"
AUDIT = ROOT / "Resources/Docs/_Fish/AchievementsTriggerAudit.json"
MAPPING = ROOT / "Resources/Docs/_Fish/AchievementsSs13Mapping.json"
REMOVED = ROOT / "Resources/Docs/_Fish/AchievementsRemoved.json"
FTL_PATHS = [
    ROOT / "Resources/Locale/en-US/_fish/achievements.ftl",
    ROOT / "Resources/Locale/ru-RU/_fish/achievements.ftl",
]

BLOCK_RE = re.compile(r"- type: achievement\r?\n[\s\S]*?(?=\r?\n- type: |\Z)")


def block_id(block: str) -> str | None:
    m = re.search(r"^  id: (FishAch\w+)", block, re.M)
    return m.group(1) if m else None


def main() -> None:
    remove_ids: set[str] = set()
    for path in sorted(ACH_DIR.glob("*.yml")):
        if path.name == "categories.yml":
            continue
        text = path.read_text(encoding="utf-8")
        kept: list[str] = []
        for block in BLOCK_RE.findall(text):
            aid = block_id(block)
            if aid and "# catalog-duplicate:" in block:
                remove_ids.add(aid)
                continue
            if not block.endswith("\n"):
                block += "\n"
            kept.append(block)
        path.write_text("".join(kept), encoding="utf-8")

    prefixes = {aid.replace("FishAch_", "achievement-fishach_", 1).lower() for aid in remove_ids}
    for ftl in FTL_PATHS:
        lines = ftl.read_text(encoding="utf-8").splitlines(keepends=True)
        out = [ln for ln in lines if not any(ln.startswith(p) for p in prefixes)]
        ftl.write_text("".join(out), encoding="utf-8")

    if AUDIT.exists():
        audit = json.loads(AUDIT.read_text(encoding="utf-8"))
        AUDIT.write_text(
            json.dumps([r for r in audit if r["id"] not in remove_ids], indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )

    if MAPPING.exists():
        mp = json.loads(MAPPING.read_text(encoding="utf-8"))
        MAPPING.write_text(
            json.dumps([r for r in mp if r.get("id") not in remove_ids], indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )

    log = json.loads(REMOVED.read_text(encoding="utf-8")) if REMOVED.exists() else {"ids": []}
    all_ids = sorted(set(log.get("ids", [])) | remove_ids)
    REMOVED.write_text(
        json.dumps(
            {
                "count": len(all_ids),
                "ids": all_ids,
                "reason": "blocked/manual/fake objective/catalog-duplicate filler",
            },
            indent=2,
            ensure_ascii=False,
        )
        + "\n",
        encoding="utf-8",
    )

    print(f"Removed {len(remove_ids)} catalog-duplicate achievements")


if __name__ == "__main__":
    main()
