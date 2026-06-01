"""Migration dashboard — FastAPI application.

Serves a live view of the seven-phase, human-in-the-loop .NET -> Java
modernization. State is read from ``dashboard/state.json``, which Devin updates
as it works through the phases and as humans clear the checkpoint gates. The page
auto-refreshes every 3 seconds.

Usage:
    uvicorn dashboard.app:app --host 0.0.0.0 --port 5000 --reload
"""

from __future__ import annotations

import json
from pathlib import Path

from fastapi import FastAPI, Request
from fastapi.responses import HTMLResponse, JSONResponse
from fastapi.templating import Jinja2Templates

app = FastAPI(title=".NET -> Java Modernization Dashboard")

BASE_DIR = Path(__file__).resolve().parent
templates = Jinja2Templates(directory=str(BASE_DIR / "templates"))
STATE_FILE = BASE_DIR / "state.json"


def load_state() -> dict:
    if STATE_FILE.exists():
        with open(STATE_FILE, encoding="utf-8") as f:
            return json.load(f)
    return {"status": "uninitialized", "phases": [], "gates": []}


def merged_timeline(state: dict) -> list[dict]:
    """Interleave phases and gates into a single ordered timeline for rendering."""
    gates_by_after = {g["after"]: g for g in state.get("gates", [])}
    timeline: list[dict] = []
    for phase in state.get("phases", []):
        timeline.append({"type": "phase", **phase})
        gate = gates_by_after.get(phase["id"])
        if gate:
            timeline.append({"type": "gate", **gate})
    return timeline


@app.get("/", response_class=HTMLResponse)
async def index(request: Request):
    state = load_state()
    return templates.TemplateResponse(
        "index.html",
        {"request": request, "state": state, "timeline": merged_timeline(state)},
    )


@app.get("/api/state")
async def api_state() -> JSONResponse:
    return JSONResponse(load_state())
