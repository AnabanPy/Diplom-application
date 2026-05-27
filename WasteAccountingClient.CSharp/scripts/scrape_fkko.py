#!/usr/bin/env python3
"""Scrape full FKKO catalog from rpn.gov.ru and export CSV + JSON."""

from __future__ import annotations

import csv
import json
import re
import sys
import time
import urllib.parse
import urllib.request
from pathlib import Path

BASE_URL = "https://rpn.gov.ru"
TOTAL_PAGES = 455
DELAY_SEC = 0.15

ROW_RE = re.compile(
    r'<div class="registryCard__itemTableRow">\s*'
    r'<div class="registryCard__itemTableCol _code">\s*([^<]+?)\s*</div>\s*'
    r'<div class="registryCard__itemTableCol _name">\s*'
    r'<a class="fkko-item"\s+href="(/fkko/\d+/)"[^>]*>([^<]+)</a>',
    re.DOTALL,
)


def page_url(page: int) -> str:
    if page <= 1:
        return f"{BASE_URL}/fkko/"
    return f"{BASE_URL}/fkko/nav-more-fkko/page-{page}/"


def fetch_page(page: int) -> str:
    url = page_url(page)
    data = urllib.parse.urlencode({"AJAX": "Y"}).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=data,
        method="POST",
        headers={
            "User-Agent": "WasteAccountingClient-FKKO-Import/1.0",
            "Content-Type": "application/x-www-form-urlencoded",
        },
    )
    with urllib.request.urlopen(req, timeout=60) as resp:
        return resp.read().decode("utf-8", errors="replace")


def parse_rows(html: str) -> list[dict]:
    rows: list[dict] = []
    for match in ROW_RE.finditer(html):
        fkko_code = " ".join(match.group(1).split())
        href = match.group(2)
        name = match.group(3).strip()
        numeric = href.strip("/").split("/")[-1]
        digits = re.sub(r"\s+", "", fkko_code)
        hazard = int(digits[-1]) if digits else 0
        rows.append(
            {
                "code": f"FKKO-{numeric}",
                "name": name,
                "fkko_code": fkko_code,
                "hazard_class": hazard,
                "description": "",
            }
        )
    return rows


def main() -> int:
    out_dir = Path(__file__).resolve().parent.parent / "data"
    out_dir.mkdir(parents=True, exist_ok=True)
    csv_path = out_dir / "fkko_full.csv"
    json_path = out_dir / "fkko_full.json"

    all_rows: list[dict] = []
    seen_codes: set[str] = set()

    for page in range(1, TOTAL_PAGES + 1):
        try:
            html = fetch_page(page)
        except Exception as exc:
            print(f"ERROR page {page}: {exc}", file=sys.stderr)
            return 1

        rows = parse_rows(html)
        added = 0
        for row in rows:
            key = row["fkko_code"]
            if key in seen_codes:
                continue
            seen_codes.add(key)
            all_rows.append(row)
            added += 1

        print(f"page {page}/{TOTAL_PAGES}: +{added} (total {len(all_rows)})")
        if page < TOTAL_PAGES:
            time.sleep(DELAY_SEC)

    all_rows.sort(key=lambda r: re.sub(r"\s+", "", r["fkko_code"]))

    with csv_path.open("w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=["code", "name", "fkko_code", "hazard_class", "description"],
            delimiter=";",
            quoting=csv.QUOTE_MINIMAL,
        )
        writer.writeheader()
        writer.writerows(all_rows)

    with json_path.open("w", encoding="utf-8") as f:
        json.dump(all_rows, f, ensure_ascii=False, indent=2)

    print(f"Done: {len(all_rows)} records")
    print(f"CSV:  {csv_path}")
    print(f"JSON: {json_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
