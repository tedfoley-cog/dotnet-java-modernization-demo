"""Tests for the assessment analyzer and state builder."""

from __future__ import annotations

import analyze_dotnet
import build_state


def test_inventory_finds_legacy_files():
    inv = analyze_dotnet.build_inventory()
    assert inv["totals"]["files"] >= 15, "expected the full legacy source inventory"
    assert inv["totals"]["lines"] > 0
    assert inv["files_by_kind"].get("ef_entity", 0) >= 4
    assert inv["files_by_kind"].get("service", 0) >= 3
    assert inv["files_by_kind"].get("controller", 0) >= 2


def test_constructs_detected():
    inv = analyze_dotnet.build_inventory()
    t = inv["totals"]
    # The legacy app uses async/await, LINQ, EF annotations, MVC attrs and Razor.
    assert t["async_methods"] >= 3
    assert t["linq_ops"] >= 5
    assert t["ef_annotations"] >= 5
    assert t["mvc_attrs"] >= 3
    assert t["razor_directives"] >= 3
    assert inv["complexity_band"] in {"LOW", "MEDIUM", "HIGH"}


def test_classify_kinds():
    from pathlib import Path
    assert analyze_dotnet.classify(Path("dotnet_source/x/Models/Order.cs")) == "ef_entity"
    assert analyze_dotnet.classify(Path("dotnet_source/x/Services/OrderService.cs")) == "service"
    assert analyze_dotnet.classify(Path("dotnet_source/x/Controllers/OrdersController.cs")) == "controller"
    assert analyze_dotnet.classify(Path("dotnet_source/x/Views/Orders/Index.cshtml")) == "razor_view"
    assert analyze_dotnet.classify(Path("database/schema.sql")) == "sql"


def test_state_has_seven_phases_five_gates():
    state = build_state.build_state()
    assert len(state["phases"]) == 7
    assert len(state["gates"]) == 5
    assert all(p["status"] == "pending" for p in state["phases"])
    assert all(g["status"] == "pending" for g in state["gates"])
    # Every gate references a real phase.
    phase_ids = {p["id"] for p in state["phases"]}
    assert all(g["after"] in phase_ids for g in state["gates"])


def test_state_embeds_assessment_and_mapping():
    state = build_state.build_state()
    assert state["assessment"]["files"] >= 15
    assert state["mapping"]["total_rules"] >= 20
    assert state["mapping"]["applied_rules"] == 0
