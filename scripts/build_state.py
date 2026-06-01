"""Regenerate ``dashboard/state.json`` from the assessment inventory + mapping rules.

The dashboard reads ``state.json`` for the seven phases, the five human-checkpoint
gates, the assessment summary, and the construct-mapping coverage. This script
produces the INITIAL state (every phase pending, every gate awaiting a human) so
the demo always starts from a clean slate. Devin updates ``state.json`` live as it
works through the phases.

Run:
    python scripts/analyze_dotnet.py && python scripts/build_state.py
"""

from __future__ import annotations

import json
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
INVENTORY = REPO_ROOT / "analysis_output" / "inventory.json"
MAPPING = REPO_ROOT / "docs" / "mapping_rules.json"
STATE = REPO_ROOT / "dashboard" / "state.json"

PHASES = [
    {"id": "p1", "name": "Intake", "summary": "Point Devin at one app / codebase"},
    {"id": "p2", "name": "Assessment & Analysis", "summary": "Comprehension, inventory, complexity scoring"},
    {"id": "p3", "name": "Construct Mapping", "summary": "C#->Java, EF->JPA, LINQ->Streams, async->CompletableFuture, Razor->Thymeleaf"},
    {"id": "p4", "name": "Gap Analysis & Architecture", "summary": "Identify gaps; propose target architecture"},
    {"id": "p5", "name": "Implementation", "summary": "Generate Java/Spring Boot into java_target"},
    {"id": "p6", "name": "Testing & Validation", "summary": "Generate + run tests, equivalence checks, fix loop"},
    {"id": "p7", "name": "Output / Release", "summary": "Modernized app + release/runbook docs"},
]

# Each gate sits AFTER the named phase; "before" is the next phase.
GATES = [
    {"id": "g1", "name": "Human reviews assessment", "after": "p2", "role": "Tech Lead"},
    {"id": "g2", "name": "Architect approves mapping rules", "after": "p3", "role": "Architect"},
    {"id": "g3", "name": "Architect signs off on architecture", "after": "p4", "role": "Architect"},
    {"id": "g4", "name": "Human reviews & approves PR before merge", "after": "p6", "role": "Reviewer"},
    {"id": "g5", "name": "Human approves release", "after": "p7", "role": "Release Manager"},
]


def load_json(path: Path) -> dict:
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    return {}


def build_state() -> dict:
    inventory = load_json(INVENTORY)
    mapping = load_json(MAPPING)

    totals = inventory.get("totals", {})
    mapping_categories = mapping.get("categories", [])
    total_rules = sum(len(c.get("rules", [])) for c in mapping_categories)

    return {
        "status": "ready",
        "title": ".NET -> Java Modernization",
        "subtitle": "Phased, human-in-the-loop migration of a legacy ASP.NET MVC / EF6 app to Spring Boot",
        "phases": [{**p, "status": "pending"} for p in PHASES],
        "gates": [{**g, "status": "pending"} for g in GATES],
        "assessment": {
            "files": totals.get("files", 0),
            "lines": totals.get("lines", 0),
            "complexity": totals.get("complexity", 0),
            "complexity_band": inventory.get("complexity_band", "UNKNOWN"),
            "async_methods": totals.get("async_methods", 0),
            "linq_ops": totals.get("linq_ops", 0),
            "ef_annotations": totals.get("ef_annotations", 0),
            "mvc_attrs": totals.get("mvc_attrs", 0),
            "razor_directives": totals.get("razor_directives", 0),
            "files_by_kind": inventory.get("files_by_kind", {}),
        },
        "mapping": {
            "categories": [
                {"id": c["id"], "label": c["label"], "rule_count": len(c.get("rules", [])),
                 "applied": 0}
                for c in mapping_categories
            ],
            "total_rules": total_rules,
            "applied_rules": 0,
        },
        "outputs": {
            "java_files_generated": 0,
            "tests_generated": 0,
            "tests_passing": 0,
            "pr_url": None,
        },
    }


def main() -> None:
    state = build_state()
    STATE.parent.mkdir(parents=True, exist_ok=True)
    STATE.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {STATE.relative_to(REPO_ROOT)} "
          f"({len(state['phases'])} phases, {len(state['gates'])} gates, "
          f"{state['mapping']['total_rules']} mapping rules)")


if __name__ == "__main__":
    main()
