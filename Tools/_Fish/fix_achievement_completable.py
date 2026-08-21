#!/usr/bin/env python3
"""Дедуп stub-ачивок: один completable primary на (condition, progressTarget), остальные → manual."""

from __future__ import annotations

import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ACH_DIR = ROOT / "Resources" / "Prototypes" / "_Fish" / "Achievements"
LOC_EN = ROOT / "Resources" / "Locale" / "en-US" / "_fish" / "achievements.ftl"
LOC_RU = ROOT / "Resources" / "Locale" / "ru-RU" / "_fish" / "achievements.ftl"

BLOCK_SPLIT = re.compile(r"(?m)(?=^- type: achievement\b)")

HONEST_RU = {
    "interaction": "Совершите {n} взаимодействий рукой с объектами.",
    "kill": "Убейте {n} игроков-гуманоидов.",
    "death": "Умрите {n} раз (не суицидом).",
    "round-end-alive": "Доживите до конца раунда живым {n} раз.",
    "heal": "Вылечите других игроков {n} раз.",
    "shuttle-arrive": "Прибудьте на аварийном шаттле живым {n} раз.",
    "station-event": "Застаньте начало {n} станционных событий, будучи в раунде.",
    "job-play": "Сыграйте {n} смен (спавн на роль).",
    "explosion": "Окажитесь рядом с {n} взрывами.",
    "damage-dealt": "Нанесите урон игрокам-гуманоидам {n} раз (тиков).",
    "craft": "Скрафтите {n} предметов.",
    "item-pickup": "Экипируйте предметы {n} раз.",
    "first-late-join": "Зайдите в уже идущий раунд.",
    "slip-death": "Умрите, поскользнувшись.",
    "counter": "Наберите нужный счётчик.",
    "manual": "Выдаётся вручную или станет доступно позже.",
}

HONEST_EN = {
    "interaction": "Perform {n} hand interactions with objects.",
    "kill": "Kill {n} player humanoids.",
    "death": "Die {n} times (not by suicide).",
    "round-end-alive": "Survive to round end alive {n} times.",
    "heal": "Heal other players {n} times.",
    "shuttle-arrive": "Arrive alive on the emergency shuttle {n} times.",
    "station-event": "Be in-round for the start of {n} station events.",
    "job-play": "Spawn into a job {n} times.",
    "explosion": "Be near {n} explosions.",
    "damage-dealt": "Deal damage to player humanoids {n} times.",
    "craft": "Craft {n} items.",
    "item-pickup": "Equip items {n} times.",
    "first-late-join": "Late-join an ongoing round.",
    "slip-death": "Die after slipping.",
    "counter": "Reach the required counter.",
    "manual": "Granted manually or unlocked in a future update.",
}


def parse_field(block: str, name: str) -> str | None:
    m = re.search(rf"(?m)^  {name}:\s*(.+)$", block)
    return m.group(1).strip() if m else None


def has_condition_params(block: str) -> bool:
    return bool(re.search(r"(?m)^  conditionParams:", block))


def set_or_add_field(block: str, name: str, value: str) -> str:
    if re.search(rf"(?m)^  {name}:", block):
        return re.sub(rf"(?m)^  {name}:\s*.*$", f"  {name}: {value}", block, count=1)
    # insert after condition line
    if re.search(r"(?m)^  condition:", block):
        return re.sub(
            r"(?m)^(  condition:\s*.*)$",
            rf"\1\n  {name}: {value}",
            block,
            count=1,
        )
    return block.rstrip() + f"\n  {name}: {value}\n"


def remove_field(block: str, name: str) -> str:
    return re.sub(rf"(?m)^  {name}:\s*.*\n", "", block)


def main() -> None:
    files = sorted(ACH_DIR.glob("*.yml"))
    # id -> (file, block_index, block)
    entries: list[dict] = []
    file_blocks: dict[Path, list[str]] = {}

    for path in files:
        text = path.read_text(encoding="utf-8")
        parts = BLOCK_SPLIT.split(text)
        preamble = parts[0]
        blocks = parts[1:]
        file_blocks[path] = [preamble, *blocks]
        for i, block in enumerate(blocks, start=1):
            if not block.startswith("- type: achievement"):
                continue
            ach_id = parse_field(block, "id")
            if not ach_id:
                continue
            cond = parse_field(block, "condition") or "manual"
            pt_raw = parse_field(block, "progressTarget")
            pt = int(pt_raw) if pt_raw and pt_raw.isdigit() else 1
            entries.append(
                {
                    "id": ach_id,
                    "path": path,
                    "index": i,
                    "condition": cond,
                    "progress_target": pt,
                    "has_params": has_condition_params(block),
                    "allow_generic": bool(
                        re.search(r"(?m)^  allowGenericTrigger:\s*true\s*$", block)
                    ),
                }
            )

    # Groups that need a primary
    groups: dict[tuple[str, int], list[dict]] = defaultdict(list)
    for e in entries:
        if e["has_params"] or e["condition"] == "manual":
            continue
        if e["progress_target"] <= 1 and e["allow_generic"]:
            continue  # seed binaries already OK
        if e["progress_target"] <= 1 and not e["allow_generic"]:
            # binary without allow — leave; MatchesContext rejects
            continue
        groups[(e["condition"], e["progress_target"])].append(e)

    primary_ids: set[str] = set()
    duplicate_ids: set[str] = set()
    primary_meta: dict[str, tuple[str, int]] = {}

    for (cond, pt), members in groups.items():
        members_sorted = sorted(
            members,
            key=lambda m: (
                0 if "source: Fish-original" in file_blocks[m["path"]][m["index"]] else 1,
                0 if m["id"].startswith("FishAch") and "_" not in m["id"][7:] else 1,
                m["id"],
            ),
        )
        # Prefer seed-like short ids / fish original comment
        for m in members:
            block = file_blocks[m["path"]][m["index"]]
            m["_fish_orig"] = "# source: Fish-original" in block
            m["_seedish"] = m["path"].name == "seed.yml"
        members_sorted = sorted(
            members,
            key=lambda m: (
                0 if m["_seedish"] else 1,
                0 if m["_fish_orig"] else 1,
                m["id"],
            ),
        )
        primary = members_sorted[0]
        primary_ids.add(primary["id"])
        primary_meta[primary["id"]] = (cond, pt)
        for m in members_sorted[1:]:
            duplicate_ids.add(m["id"])

    # Singles with progress>1 and no params also need allowGeneric
    for e in entries:
        if e["has_params"] or e["allow_generic"] or e["condition"] == "manual":
            continue
        if e["progress_target"] > 1 and e["id"] not in duplicate_ids:
            primary_ids.add(e["id"])
            primary_meta[e["id"]] = (e["condition"], e["progress_target"])

    # Rewrite blocks
    for path, parts in file_blocks.items():
        changed = False
        for i in range(1, len(parts)):
            block = parts[i]
            ach_id = parse_field(block, "id")
            if not ach_id:
                continue
            if ach_id in primary_ids:
                cond, pt = primary_meta[ach_id]
                new_block = set_or_add_field(block, "allowGenericTrigger", "true")
                # Point description to generic honest loc key
                gen_key = f"achievement-fish-generic-{cond.replace('-', '')}-{pt}-desc"
                new_block = set_or_add_field(new_block, "description", gen_key)
                if new_block != block:
                    parts[i] = new_block
                    changed = True
            elif ach_id in duplicate_ids:
                new_block = set_or_add_field(block, "condition", "manual")
                new_block = set_or_add_field(new_block, "progressTarget", "1")
                new_block = remove_field(new_block, "allowGenericTrigger")
                new_block = set_or_add_field(
                    new_block, "description", "achievement-fish-catalog-pending-desc"
                )
                # Keep name/flavor; mark comment
                if "# catalog-duplicate" not in new_block:
                    new_block = new_block.rstrip() + "\n  # catalog-duplicate: progress goes to primary sibling\n"
                if new_block != block:
                    parts[i] = new_block
                    changed = True
        if changed:
            path.write_text("".join(parts), encoding="utf-8")
            print(f"updated {path.name}")

    # Locale keys
    def ensure_locale(path: Path, lang: str) -> None:
        text = path.read_text(encoding="utf-8")
        additions: list[str] = []
        pending = (
            "achievement-fish-catalog-pending-desc = Catalog flavor entry. Auto-tracking uses the related generic achievement."
            if lang == "en"
            else "achievement-fish-catalog-pending-desc = Коллекционная запись каталога. Автопрогресс идёт в связанное общее достижение."
        )
        if "achievement-fish-catalog-pending-desc" not in text:
            additions.append(pending)

        honest = HONEST_EN if lang == "en" else HONEST_RU
        for ach_id, (cond, pt) in sorted(primary_meta.items()):
            key = f"achievement-fish-generic-{cond.replace('-', '')}-{pt}-desc"
            if key in text:
                continue
            template = honest.get(cond, honest["manual"])
            additions.append(f"{key} = {template.format(n=pt)}")

        if additions:
            path.write_text(text.rstrip() + "\n\n# generic completable / pending\n" + "\n".join(additions) + "\n", encoding="utf-8")
            print(f"locale {path.name}: +{len(additions)}")

    ensure_locale(LOC_EN, "en")
    ensure_locale(LOC_RU, "ru")

    print(f"primaries={len(primary_ids)} duplicates={len(duplicate_ids)}")


if __name__ == "__main__":
    main()
