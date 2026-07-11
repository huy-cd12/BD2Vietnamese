#!/usr/bin/env python3
import json
import shutil
import sys
from datetime import datetime
from pathlib import Path

if len(sys.argv) != 3:
    raise SystemExit(
        "Usage: merge_missing_skill_entries.py ACTIVE_JSON FIXED_JSON"
    )

active_path = Path(sys.argv[1])
fixed_path = Path(sys.argv[2])

if active_path.exists():
    with active_path.open(encoding="utf-8") as file:
        active = json.load(file)
else:
    active = {}

with fixed_path.open(encoding="utf-8") as file:
    fixed = json.load(file)

if not isinstance(active, dict) or not isinstance(fixed, dict):
    raise SystemExit("Both JSON roots must be objects.")

if active_path.exists():
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup = active_path.with_name(
        f"{active_path.stem}.before_fix_{stamp}.json"
    )
    shutil.copy2(active_path, backup)
    print("Backup:", backup)

added = []

for key, value in fixed.items():
    if key not in active:
        active[key] = value
        added.append(key)

active = dict(
    sorted(
        active.items(),
        key=lambda item: (
            int(item[0].split(":", 1)[0])
            if item[0].split(":", 1)[0].isdigit()
            else 10**30,
            item[0],
        ),
    )
)

active_path.parent.mkdir(parents=True, exist_ok=True)
temporary = active_path.with_suffix(active_path.suffix + ".tmp")
temporary.write_text(
    json.dumps(active, ensure_ascii=False, indent=4) + "\n",
    encoding="utf-8",
)
temporary.replace(active_path)

print("Added missing keys:", len(added))
for key in added:
    print(" +", key)

if "20038025" in active:
    print("Confirmed: 20038025 exists.")
