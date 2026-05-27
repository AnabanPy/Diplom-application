#!/usr/bin/env python3
"""Build slim gzipped FKKO pack for operator client (~200 KB)."""

from __future__ import annotations

import gzip
import json
from pathlib import Path

DATA_DIR = Path(__file__).resolve().parent.parent / "data"


def build_operator_pack(source: Path | None = None) -> Path:
    source = source or DATA_DIR / "fkko_full.json"
    out = DATA_DIR / "fkko_operator.json.gz"

    rows = json.loads(source.read_text(encoding="utf-8"))
    slim = [
        {
            "code": r["code"],
            "name": r["name"],
            "fkko_code": r["fkko_code"],
            "hazard_class": r["hazard_class"],
        }
        for r in rows
        if r.get("hazard_class") in (1, 2, 3, 4, 5)
    ]

    with gzip.open(out, "wt", encoding="utf-8") as f:
        json.dump(slim, f, ensure_ascii=False, separators=(",", ":"))

    print(f"Operator pack: {out} ({out.stat().st_size // 1024} KB, {len(slim)} records)")
    return out


if __name__ == "__main__":
    build_operator_pack()
