# Implementation Plan — .NET → Java Modernization Demo

> Initial-state scaffold for a live Devin demo. This document is committed **before** the rest of
> the repo and describes what the demo proves, what Devin does live, the stack, the file layout, the
> flowchart, the runtime, CI, and open risks. No "after"/migrated artifacts exist in this scaffold —
> the `java_target/` tree is an intentionally empty Spring Boot skeleton that Devin fills in live.

## 1. What the demo proves

An enterprise can modernize a legacy .NET (C#) line-of-business application to a modern Java / Spring
Boot stack through a **phased, auditable, human-in-the-loop process** — not a black-box "throw it at
AI and pray" conversion. Each phase produces a reviewable artifact, and a human gate sits between
every phase so an architect or tech lead stays in control: they approve the assessment, sign off on
the construct-mapping rules and the target-architecture proposal, review the generated code and the
PR, and approve the release. Devin executes every phase; the humans steer.

## 2. What Devin does live (one sentence)

Devin points at the legacy `dotnet_source/` app, runs the seven-phase modernization pipeline —
intake → assessment → construct mapping → gap analysis & architecture proposal → implementation →
test/validation → release — pausing at each human checkpoint, updating the live migration dashboard
as it goes, generating the Java/Spring Boot code into `java_target/`, and opening a PR for human
review before merge.

## 3. Stack and rationale (with sources confirmed during research)

### Legacy "before" stack (`dotnet_source/`)
| Choice | Why / source |
|---|---|
| ASP.NET MVC 5 on .NET Framework 4.8 | Canonical legacy enterprise web stack; `System.Web.Mvc`, `Controller`, `ActionResult`, `[Authorize]` per ASP.NET MVC docs — https://learn.microsoft.com/en-us/aspnet/mvc/ |
| Entity Framework 6 (Code-First) | `DbContext`/`DbSet<T>`, data-annotation mappings (`[Key]`, `[Required]`, `[Table]`) per EF6 docs — https://learn.microsoft.com/en-us/ef/ef6/ |
| Razor views (`.cshtml`) | Server-side rendering with `@model`, `@Html.*`, `@foreach` per Razor syntax docs — https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor |
| LINQ-to-Entities | `Where`/`Select`/`GroupBy`/`OrderBy` query operators per LINQ docs — https://learn.microsoft.com/en-us/dotnet/csharp/linq/ |
| `async`/`await` + `Task<T>` | Task Asynchronous Programming model per https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model |
| SQL Server (T-SQL) schema + stored proc | `CREATE TABLE`, `IDENTITY`, `CREATE PROCEDURE` per T-SQL reference — https://learn.microsoft.com/en-us/sql/t-sql/ |
| Domain | Customer order management & invoicing for a generic enterprise distributor (no customer identifiers) |

### Target "after" stack (`java_target/` — empty skeleton, Devin fills live)
| Choice | Why / source |
|---|---|
| Java 17 + Spring Boot 3.x | Current LTS Java + current Spring Boot generation; `@SpringBootApplication`, MVC controllers per https://docs.spring.io/spring-boot/reference/web/servlet.html |
| Spring Data JPA / Hibernate (Jakarta Persistence 3.1) | `@Entity`, `@Id`, `EntityManager`, repositories per Jakarta Persistence 3.1 spec — https://jakarta.ee/specifications/persistence/3.1/ and https://docs.spring.io/spring-data/jpa/reference/ |
| Java Streams API | LINQ → `stream().filter().map().collect()` per https://docs.oracle.com/javase/8/docs/api/java/util/stream/package-summary.html |
| `CompletableFuture<T>` | async/await → `CompletableFuture` per https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/CompletableFuture.html |
| Thymeleaf | Razor → Thymeleaf templates per https://www.thymeleaf.org/documentation.html |
| Maven | Standard Spring Boot build per Spring Initializr defaults — https://start.spring.io |

### Demo tooling
| Choice | Why |
|---|---|
| Python 3.11 + FastAPI + Jinja2 dashboard | Matches prior demo repos (`ada-rust-migration-demo`); single `uvicorn` command, auto-refreshing visual artifact |
| Python analysis script (`scripts/analyze_dotnet.py`) | Powers the ASSESSMENT phase: inventories the C# source, computes a complexity score, emits `analysis_output/inventory.json` |
| `scripts/build_state.py` | Generates `dashboard/state.json` (phases + checkpoints) from the inventory |

## 4. Repo layout (target ~20 source files)

```
docs/
  IMPLEMENTATION_PLAN.md        # this file
  flowchart.html                # phased pipeline w/ human-checkpoint gates (Mermaid, htmlLabels:false)
  flowchart.png                 # full-page render of flowchart.html
  mapping_rules.json            # machine-readable C#->Java construct mapping (drives dashboard phase 3)
dotnet_source/OrderManagement.Web/
  OrderManagement.Web.csproj    # .NET Framework 4.8 project file
  Web.config                    # connectionStrings + EF + auth config
  Global.asax.cs                # app startup / route + EF init
  App_Start/RouteConfig.cs      # MVC route table
  Models/Customer.cs            # EF entity (credit limit, terms)
  Models/Product.cs             # EF entity (price, tax class)
  Models/Order.cs               # EF entity (status, header totals)
  Models/OrderLine.cs           # EF entity (qty, unit price, discount)
  Models/Invoice.cs             # EF entity (issued invoice)
  Data/OrderContext.cs          # EF6 DbContext (Code-First)
  Services/PricingService.cs    # tiered discounts + tax (LINQ)
  Services/OrderService.cs      # order workflow, credit check (async/await, LINQ)
  Services/InvoiceService.cs    # invoice generation
  Controllers/OrdersController.cs    # async MVC actions, [Authorize]
  Controllers/CustomersController.cs # async MVC actions
  Views/Shared/_Layout.cshtml        # Razor layout
  Views/Orders/Index.cshtml          # Razor list
  Views/Orders/Details.cshtml        # Razor detail
database/
  schema.sql                    # T-SQL schema
  sp_OrderSummary.sql           # T-SQL stored procedure (reporting)
java_target/                    # EMPTY skeleton — Devin migrates here LIVE
  pom.xml                       # Spring Boot 3.x / Java 17 build (deps declared, no business code)
  src/main/java/com/acme/ordermgmt/OrderManagementApplication.java   # @SpringBootApplication only
  src/main/java/com/acme/ordermgmt/{model,repository,service,controller,config}/.gitkeep
  src/test/java/com/acme/ordermgmt/SmokeTest.java   # context-free placeholder test
  src/main/resources/application.properties
  README.md                     # "target is empty; Devin fills it live"
dashboard/
  app.py                        # FastAPI app (reads state.json)
  state.json                    # 7 phases + 6 human-checkpoint gates, all initial
  templates/index.html          # auto-refreshing phase/gate visual
scripts/
  analyze_dotnet.py             # ASSESSMENT: inventory + complexity scoring
  build_state.py                # regenerate dashboard/state.json from inventory
  test_analyze.py               # pytest for the analyzer
migration-playbook.md           # C#->Java mapping tables + phase contract (TRANSLATION ask)
README.md                       # flowchart-first overview
DEMO_NOTES.md                   # presenter cheat sheet
.github/workflows/ci.yml        # lint + tests + java skeleton compile + structure check
```

## 5. Flowchart outline (nodes & edges)

Linear top-down pipeline with a visually distinct **human checkpoint** node before each phase
transition. Three node classes (distinct colors): **trigger**, **Devin phase**, **human checkpoint**.

```
START (trigger: prompt Devin at the .NET repo)
  -> P1 Intake (point at one app/codebase)
  -> P2 Assessment & Analysis (comprehension, inventory, complexity)
  -> GATE1 [Human reviews assessment]
  -> P3 Construct Mapping (C#->Java, EF->JPA, LINQ->Streams, async->CompletableFuture, Razor->Thymeleaf)
  -> GATE2 [Architect approves mapping rules]
  -> P4 Gap Analysis & Architecture Proposal
  -> GATE3 [Architect signs off on architecture]
  -> P5 Implementation (generate Java/Spring Boot into java_target)
  -> P6 Testing & Validation (generate + run tests, equivalence checks, fix loop)
  -> GATE4 [Human reviews & approves PR before merge]
  -> P7 Output / Release (modernized app + release docs)
  -> GATE5 [Human approves release]
  -> DONE (released Java app)
```
Legend explains the three node classes and that gates are human-in-the-loop.

## 6. Runtime plan

- **Runnable visual artifact**: the FastAPI migration dashboard. `uvicorn dashboard.app:app --port 5000`
  serves a page showing the 7 phases and 5 human-checkpoint gates with live status, the inventory
  from the assessment, and the construct-mapping coverage. Auto-refreshes every 3s from `state.json`.
- **"Appears runnable" elements** (called out in README & handoff): the legacy `.NET` app is
  **read-only source** — there is no .NET runtime in the demo box, and it represents day-zero
  customer code. The ASSESSMENT phase is genuinely runnable: `scripts/analyze_dotnet.py` parses the
  real C# files and produces `analysis_output/inventory.json`, which feeds the dashboard. The
  Java target is a compiling skeleton (`mvn compile` passes) that Devin fills in live.
- **Devin is the runtime**: orchestration, mapping, code generation, test generation/execution, and
  the fix loop all run inside the live Devin session. The dashboard, localhost, and the migration PR
  are the *outputs* the presenter shows.

## 7. CI plan (simple, green)

`.github/workflows/ci.yml`:
1. **python** job: `pip install -r requirements.txt`, `ruff check`, `pytest scripts/`.
2. **java-skeleton** job: `actions/setup-java@v4` (Temurin 17), `mvn -q -B compile` in `java_target`,
   `mvn -q -B test` (runs the placeholder smoke test).
3. **structure** job: assert `dotnet_source/` and `java_target/` exist and the dashboard state is
   valid JSON.

Kept well under the complexity bar; no matrix, no deployment.

## 8. Risks and unknowns

- The legacy .NET app is not compiled in this environment (no .NET SDK). It is authentic ASP.NET
  MVC 5 / EF6 / Razor source confirmed against Microsoft docs, but is intended as read-only "before"
  state, not a buildable target. Called out in the README.
- Vendor syntax is all from first-party Microsoft / Oracle / Jakarta / Spring / Thymeleaf docs
  (cited in §3 and `migration-playbook.md`); no invented syntax.
- Human-checkpoint mechanics in the live demo are conversational (presenter approves in chat / via PR
  review). The dashboard reflects gate state but does not gate execution programmatically — this is a
  demo of the *process*, and is noted in DEMO_NOTES.
