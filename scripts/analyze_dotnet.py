"""Assessment-phase analyzer for the legacy .NET source.

Walks ``dotnet_source/`` and ``database/``, inventories every file, classifies it,
counts the migration-relevant constructs (EF annotations, LINQ operators, async
methods, MVC action attributes, Razor directives), and computes a per-file
complexity score. Emits ``analysis_output/inventory.json``, which feeds the
migration dashboard's Assessment and Construct-Mapping phases.

This is genuinely runnable — it is the "real" part of the otherwise read-only
legacy app. Run it directly:

    python scripts/analyze_dotnet.py
"""

from __future__ import annotations

import json
import re
from dataclasses import asdict, dataclass, field
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DOTNET_DIR = REPO_ROOT / "dotnet_source"
DB_DIR = REPO_ROOT / "database"
OUTPUT = REPO_ROOT / "analysis_output" / "inventory.json"

# Construct patterns we care about for the migration mapping.
LINQ_OPS = ["Where", "Select", "OrderBy", "OrderByDescending", "GroupBy",
            "First", "FirstOrDefault", "Sum", "Count", "Take", "Skip", "Any"]
EF_ANNOTATIONS = ["[Table", "[Key]", "[Column", "[Required]", "[ForeignKey",
                  "[StringLength", "DbSet<", "DbContext"]
MVC_ATTRS = ["[HttpGet]", "[HttpPost]", "[Authorize]", "ActionResult",
             "[ValidateAntiForgeryToken]"]
RAZOR_DIRECTIVES = ["@model", "@foreach", "@Html.", "@RenderBody", "@if", "@using"]


@dataclass
class FileReport:
    path: str
    kind: str
    language: str
    lines: int
    async_methods: int = 0
    linq_ops: int = 0
    ef_annotations: int = 0
    mvc_attrs: int = 0
    razor_directives: int = 0
    complexity: int = 0
    constructs: list[str] = field(default_factory=list)


def classify(path: Path) -> str:
    p = str(path).replace("\\", "/")
    if p.endswith(".csproj") or p.endswith(".sln") or p.endswith(".config"):
        return "project_config"
    if p.endswith(".cshtml"):
        return "razor_view"
    if p.endswith(".sql"):
        return "sql"
    if "/Models/" in p:
        return "ef_entity"
    if "/Data/" in p:
        return "ef_context"
    if "/Services/" in p:
        return "service"
    if "/Controllers/" in p:
        return "controller"
    if p.endswith(".cs"):
        return "csharp"
    return "other"


def language_of(path: Path) -> str:
    suffix = path.suffix.lower()
    return {
        ".cs": "C#",
        ".cshtml": "Razor",
        ".sql": "T-SQL",
        ".config": "XML",
        ".csproj": "MSBuild",
        ".sln": "Solution",
    }.get(suffix, "other")


def count_occurrences(text: str, needles: list[str]) -> tuple[int, list[str]]:
    found: list[str] = []
    total = 0
    for needle in needles:
        n = text.count(needle)
        if n:
            total += n
            found.append(f"{needle.strip('[')}×{n}")
    return total, found


def analyze_file(path: Path) -> FileReport:
    text = path.read_text(encoding="utf-8", errors="replace")
    lines = text.count("\n") + 1
    rel = str(path.relative_to(REPO_ROOT))
    report = FileReport(path=rel, kind=classify(path),
                        language=language_of(path), lines=lines)

    report.async_methods = len(re.findall(r"async\s+Task", text))
    report.linq_ops, linq_found = count_occurrences(text, [f".{op}(" for op in LINQ_OPS])
    report.ef_annotations, ef_found = count_occurrences(text, EF_ANNOTATIONS)
    report.mvc_attrs, mvc_found = count_occurrences(text, MVC_ATTRS)
    report.razor_directives, razor_found = count_occurrences(text, RAZOR_DIRECTIVES)

    # Weighted complexity: structural constructs cost more than raw lines.
    report.complexity = (
        report.lines // 10
        + report.async_methods * 3
        + report.linq_ops * 2
        + report.ef_annotations * 2
        + report.mvc_attrs * 2
        + report.razor_directives
    )
    report.constructs = (linq_found + ef_found + mvc_found + razor_found)[:8]
    return report


def collect_files() -> list[Path]:
    patterns = ["*.cs", "*.cshtml", "*.csproj", "*.config", "*.sln"]
    files: list[Path] = []
    for pattern in patterns:
        files.extend(DOTNET_DIR.rglob(pattern))
    files.extend(DB_DIR.rglob("*.sql"))
    return sorted(set(files))


def build_inventory() -> dict:
    reports = [analyze_file(f) for f in collect_files()]

    by_kind: dict[str, int] = {}
    for r in reports:
        by_kind[r.kind] = by_kind.get(r.kind, 0) + 1

    totals = {
        "files": len(reports),
        "lines": sum(r.lines for r in reports),
        "async_methods": sum(r.async_methods for r in reports),
        "linq_ops": sum(r.linq_ops for r in reports),
        "ef_annotations": sum(r.ef_annotations for r in reports),
        "mvc_attrs": sum(r.mvc_attrs for r in reports),
        "razor_directives": sum(r.razor_directives for r in reports),
        "complexity": sum(r.complexity for r in reports),
    }

    # Simple band: drives the dashboard complexity badge.
    score = totals["complexity"]
    band = "LOW" if score < 60 else "MEDIUM" if score < 140 else "HIGH"

    return {
        "source_root": "dotnet_source",
        "files_by_kind": by_kind,
        "totals": totals,
        "complexity_band": band,
        "files": [asdict(r) for r in sorted(reports, key=lambda r: -r.complexity)],
    }


def main() -> None:
    inventory = build_inventory()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(inventory, indent=2) + "\n", encoding="utf-8")
    t = inventory["totals"]
    print(f"Analyzed {t['files']} files / {t['lines']} lines "
          f"(complexity {t['complexity']} = {inventory['complexity_band']})")
    print(f"  async methods: {t['async_methods']}  LINQ ops: {t['linq_ops']}  "
          f"EF annotations: {t['ef_annotations']}  MVC attrs: {t['mvc_attrs']}  "
          f"Razor directives: {t['razor_directives']}")
    print(f"Wrote {OUTPUT.relative_to(REPO_ROOT)}")


if __name__ == "__main__":
    main()
