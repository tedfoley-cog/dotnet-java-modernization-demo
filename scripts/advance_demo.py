"""Advance the migration state by one step — the hook Devin drives during the demo.

In the live demo Devin updates ``dashboard/state.json`` as it completes each phase
and as humans clear each checkpoint gate; the dashboard reflects the change within
3 seconds. This script encapsulates one "step" so the motion is reproducible:

  - the first pending phase goes ``pending -> active``;
  - an ``active`` phase goes ``active -> done`` and, if a gate follows, that gate
    goes ``pending -> waiting`` (awaiting a human);
  - a ``waiting`` gate is cleared (``waiting -> cleared``) — this models the human
    approving at the checkpoint.

It also nudges the mapping coverage and output counters so the dashboard tells a
coherent story as the pipeline progresses.

Usage:
    python scripts/advance_demo.py          # one step
    python scripts/advance_demo.py --reset  # back to the initial state
    python scripts/advance_demo.py --to p5  # fast-forward through phase p5
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
STATE = REPO_ROOT / "dashboard" / "state.json"


def load() -> dict:
    return json.loads(STATE.read_text(encoding="utf-8"))


def save(state: dict) -> None:
    STATE.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")


def gate_after(state: dict, phase_id: str) -> dict | None:
    for g in state["gates"]:
        if g["after"] == phase_id:
            return g
    return None


def bump_mapping(state: dict) -> None:
    """Apply one category's worth of rules per construct-mapping step."""
    for c in state["mapping"]["categories"]:
        if c["applied"] < c["rule_count"]:
            c["applied"] = c["rule_count"]
            break
    state["mapping"]["applied_rules"] = sum(c["applied"] for c in state["mapping"]["categories"])


def step(state: dict) -> str:
    # 1) Clear any gate that is waiting on a human.
    for g in state["gates"]:
        if g["status"] == "waiting":
            g["status"] = "cleared"
            return f"Human cleared checkpoint: {g['name']}"

    # 2) Finish an active phase (and open the following gate, if any).
    for p in state["phases"]:
        if p["status"] == "active":
            p["status"] = "done"
            if p["id"] == "p3":
                state["mapping"]["applied_rules"] = state["mapping"]["total_rules"]
                for c in state["mapping"]["categories"]:
                    c["applied"] = c["rule_count"]
            if p["id"] == "p5":
                state["outputs"]["java_files_generated"] = 14
            if p["id"] == "p6":
                state["outputs"]["tests_generated"] = 22
                state["outputs"]["tests_passing"] = 22
                state["outputs"]["pr_url"] = (
                    "https://github.com/tedfoley-cog/dotnet-java-modernization-demo/pull/1"
                )
            g = gate_after(state, p["id"])
            if g and g["status"] == "pending":
                g["status"] = "waiting"
                return f"Completed {p['name']} -> awaiting human at: {g['name']}"
            return f"Completed phase: {p['name']}"

    # 3) Start the next pending phase.
    for p in state["phases"]:
        if p["status"] == "pending":
            p["status"] = "active"
            if p["id"] == "p3":
                bump_mapping(state)
            return f"Started phase: {p['name']}"

    return "Pipeline complete — modernized Java app released."


def reset() -> None:
    subprocess.run([sys.executable, str(REPO_ROOT / "scripts" / "analyze_dotnet.py")], check=True)
    subprocess.run([sys.executable, str(REPO_ROOT / "scripts" / "build_state.py")], check=True)
    print("Reset to initial state.")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--reset", action="store_true", help="reset to the initial state")
    ap.add_argument("--to", metavar="PHASE_ID", help="advance until the given phase id is done")
    args = ap.parse_args()

    if args.reset:
        reset()
        return

    state = load()
    if args.to:
        for _ in range(40):
            msg = step(state)
            save(state)
            print(msg)
            done = next((p for p in state["phases"] if p["id"] == args.to), None)
            if done and done["status"] == "done":
                break
        return

    print(step(state))
    save(state)


if __name__ == "__main__":
    main()
