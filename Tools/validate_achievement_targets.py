#!/usr/bin/env python3
"""Find interaction achievements whose conditionParams.target is not a real entity prototype."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACH_DIR = ROOT / "Resources/Prototypes/_Fish/Achievements"
PROTO_DIR = ROOT / "Resources/Prototypes"
AUDIT = ROOT / "Resources/Docs/_Fish/AchievementsTriggerAudit.json"


def collect_entity_ids() -> set[str]:
    ids: set[str] = set()
    for path in PROTO_DIR.rglob("*.yml"):
        try:
            text = path.read_text(encoding="utf-8")
        except OSError:
            continue
        for m in re.finditer(r"^  id:\s+(\S+)\s*$", text, re.MULTILINE):
            if m.group(1).startswith("FishAch_"):
                continue
            ids.add(m.group(1))
    return ids


def parse_blocks(text: str) -> list[dict]:
    blocks = re.split(r"(?=\n- type: achievement)", text)
    out: list[dict] = []
    for block in blocks[1:]:
        data: dict = {}
        target = None
        in_params = False
        for line in block.splitlines():
            if line.strip() == "conditionParams:":
                in_params = True
                continue
            if in_params:
                m = re.match(r"^\s{4}(\w+):\s*(\S+)\s*$", line)
                if m:
                    if m.group(1) == "target":
                        target = m.group(2)
                    continue
                if re.match(r"^  \w+:", line):
                    in_params = False
            m = re.match(r"^  (\w+):\s*(.*)$", line)
            if m and not in_params:
                data[m.group(1)] = m.group(2).strip()
        if target:
            data["_target"] = target
        if data.get("id"):
            out.append(data)
    return out


def strip_target_from_yaml(ach_id: str) -> int:
    changed = 0
    for path in sorted(ACH_DIR.glob("*.yml")):
        text = path.read_text(encoding="utf-8")
        blocks = re.split(r"(?=\n- type: achievement)", text)
        out = [blocks[0]] if blocks else []
        file_changed = False
        for block in blocks[1:]:
            if f"id: {ach_id}" not in block:
                out.append(block)
                continue
            new_block = re.sub(
                r"\n  conditionParams:\n(?:    \w+: \S+\n)+",
                "\n",
                block,
            )
            if new_block != block:
                file_changed = True
                changed += 1
            out.append(new_block)
        if file_changed:
            path.write_text("".join(out), encoding="utf-8")
    return changed


def main() -> None:
    entity_ids = collect_entity_ids()
    invalid: list[tuple[str, str]] = []
    for path in sorted(ACH_DIR.glob("*.yml")):
        for ach in parse_blocks(path.read_text(encoding="utf-8")):
            target = ach.get("_target")
            if not target:
                continue
            if target not in entity_ids:
                invalid.append((ach["id"], target))

    print(f"Invalid targets: {len(invalid)}")
    for ach_id, target in invalid:
        print(f"  {ach_id}: {target}")

    if AUDIT.exists() and invalid:
        audit = json.loads(AUDIT.read_text(encoding="utf-8"))
        by_id = {r["id"]: r for r in audit}
        for ach_id, target in invalid:
            row = by_id.get(ach_id)
            if not row:
                continue
            if row.get("condition") != "interaction":
                continue
            row["status"] = "blocked"
            row["reason"] = f"invalid-target:{target}"
            row["conditionParams"] = {}

        AUDIT.write_text(json.dumps(list(by_id.values()), indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        print("Updated audit for invalid interaction targets")

    stripped = sum(strip_target_from_yaml(ach_id) for ach_id, _ in invalid)
    if stripped:
        print(f"Stripped invalid targets from {stripped} YAML entries")


if __name__ == "__main__":
    main()
