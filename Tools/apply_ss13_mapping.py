#!/usr/bin/env python3
"""Apply SS13 source mappings to achievement YAML and audit JSON."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MAPPING = ROOT / "Resources/Docs/_Fish/AchievementsSs13Mapping.json"
ACH_DIR = ROOT / "Resources/Prototypes/_Fish/Achievements"
AUDIT = ROOT / "Resources/Docs/_Fish/AchievementsTriggerAudit.json"
FTL = ROOT / "Resources/Locale/en-US/_fish/achievements.ftl"

TRIGGER_BY_CONDITION = {
    "singularity-consumed": "EventHorizonConsumedEntityEvent",
    "examine": "ExaminedEvent",
    "reagent-metabolize": "SolutionContainerChangedEvent",
    "succumb": "CritSuccumbEvent",
    "ai-law-changes": "SiliconLawUpdaterComponent/EntInsertedIntoContainerMessage",
    "emote": "EmoteEvent",
    "objective-complete": "RoundEndMessageEvent",
    "became-ghost": "MindRemovedMessage",
    "chasm-fall": "ChasmFallingComponent/ComponentInit",
    "gavel-strike": "GavelSystem.OnHit/GavelHammer AfterInteract",
    "tile-pry": "TileToolDoAfterEvent/SharedToolSystem.Tile",
}


def ftl_desc(ach_id: str) -> str:
    key = ach_id.replace("FishAch_", "achievement-fishach_", 1).lower() + "-desc"
    text = FTL.read_text(encoding="utf-8")
    for line in text.splitlines():
        if line.startswith(f"{key} = "):
            return line.split(" = ", 1)[1].strip()
    return ""


def load_yaml_blocks(path: Path) -> tuple[str, list[str]]:
    text = path.read_text(encoding="utf-8")
    if not text.lstrip().startswith("- type:"):
        text = "\n" + text
    parts = re.split(r"(?=\n- type: achievement)", text)
    return parts[0], parts[1:]


def parse_block(block: str) -> dict[str, str]:
    data: dict[str, str] = {}
    in_params = False
    for line in block.splitlines():
        if line.strip().startswith("conditionParams:"):
            in_params = True
            continue
        if in_params:
            if re.match(r"^  \w+:", line) and not line.startswith("    "):
                in_params = False
            elif line.startswith("    "):
                continue
            else:
                in_params = False
        m = re.match(r"^  (\w+):\s*(.*)$", line)
        if m and not in_params:
            data[m.group(1)] = m.group(2).strip()
    return data


def strip_fields(block: str, fields: set[str]) -> str:
    lines = block.splitlines()
    out: list[str] = []
    skip_params = False
    for line in lines:
        if line.strip() == "conditionParams:":
            skip_params = "conditionParams" in fields
            if skip_params:
                continue
        if skip_params:
            if line.startswith("    "):
                continue
            skip_params = False
        m = re.match(r"^  (\w+):", line)
        if m and m.group(1) in fields:
            continue
        out.append(line)
    return "\n".join(out) + "\n"


def upsert_field(block: str, key: str, value: str) -> str:
    pattern = re.compile(rf"^  {re.escape(key)}:.*$", re.MULTILINE)
    line = f"  {key}: {value}"
    if pattern.search(block):
        return pattern.sub(line, block, count=1)
    # insert after condition line if exists
    cond = re.search(r"^  condition: .+$", block, re.MULTILINE)
    if cond:
        idx = cond.end()
        return block[:idx] + "\n" + line + block[idx:]
    return block.replace("\n  order:", f"\n{line}\n  order:", 1)


def upsert_params(block: str, params: dict[str, str]) -> str:
    block = strip_fields(block, {"conditionParams", "allowGenericTrigger"})
    if not params:
        return block
    lines = ["  conditionParams:"]
    for k, v in sorted(params.items()):
        lines.append(f"    {k}: {v}")
    insert = "\n".join(lines) + "\n"
    cond = re.search(r"^  condition: .+$", block, re.MULTILINE)
    if not cond:
        return block
    idx = cond.end()
    return block[:idx] + "\n" + insert + block[idx:]


def apply_mapping(entry: dict, blocks_by_id: dict[str, tuple[Path, str]]) -> None:
    ach_id = entry["id"]
    if ach_id not in blocks_by_id:
        print(f"WARN: {ach_id} not found in YAML")
        return

    path, block = blocks_by_id[ach_id]
    block = strip_fields(block, {"allowGenericTrigger", "conditionParams"})
    block = upsert_field(block, "condition", entry.get("condition", "interaction"))

    params = entry.get("conditionParams") or {}
    block = upsert_params(block, params)

    if pt := entry.get("progressTarget"):
        block = upsert_field(block, "progressTarget", str(pt))

    if entry.get("status") == "blocked":
        block = strip_fields(block, {"conditionParams"})
        block = upsert_field(block, "condition", "interaction")

    blocks_by_id[ach_id] = (path, block)


def main() -> None:
    mappings = json.loads(MAPPING.read_text(encoding="utf-8"))
    blocks_by_id: dict[str, tuple[Path, str]] = {}

    for path in sorted(ACH_DIR.glob("*.yml")):
        prefix, blocks = load_yaml_blocks(path)
        for block in blocks:
            data = parse_block(block)
            ach_id = data.get("id")
            if ach_id:
                blocks_by_id[ach_id] = (path, block)
        # store prefix on path — hack via dict on path
        blocks_by_id[f"__prefix__{path.name}"] = (path, prefix)

    for entry in mappings:
        apply_mapping(entry, blocks_by_id)

    # rewrite files
    by_path: dict[Path, list[str]] = {}
    prefixes: dict[Path, str] = {}
    for key, (path, block) in blocks_by_id.items():
        if key.startswith("__prefix__"):
            prefixes[path] = block
            continue
        by_path.setdefault(path, []).append(block)

    for path, blocks in by_path.items():
        prefix = prefixes.get(path, "")
        path.write_text(prefix + "".join(blocks), encoding="utf-8")

    # merge audit
    audit = json.loads(AUDIT.read_text(encoding="utf-8"))
    audit_by_id = {row["id"]: row for row in audit}
    for entry in mappings:
        ach_id = entry["id"]
        condition = entry.get("condition", "interaction")
        trigger = entry.get("fishTrigger") or TRIGGER_BY_CONDITION.get(condition, condition)
        row = audit_by_id.get(ach_id, {"id": ach_id})
        row.update(
            {
                "condition": condition,
                "trigger": trigger,
                "conditionParams": entry.get("conditionParams") or {},
                "status": entry["status"],
                "reason": entry.get("blockedReason") or entry.get("sourceTrigger", "ss13-mapping"),
                "description": ftl_desc(ach_id),
                "ss13Source": entry.get("source"),
                "ss13Trigger": entry.get("sourceTrigger"),
                "fishEquivalent": entry.get("fishEquivalent"),
            }
        )
        audit_by_id[ach_id] = row

    AUDIT.write_text(json.dumps(list(audit_by_id.values()), indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    counts: dict[str, int] = {}
    for row in audit_by_id.values():
        st = row.get("status", "unknown")
        counts[st] = counts.get(st, 0) + 1

    print("Applied", len(mappings), "SS13 mappings")
    for st, n in sorted(counts.items()):
        print(f"  {st}: {n}")


if __name__ == "__main__":
    main()
