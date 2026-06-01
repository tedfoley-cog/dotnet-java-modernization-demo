# Demo Cheat Sheet — .NET → Java Modernization

## Setup (do this before joining the call)
- [ ] Open the running dashboard tab (`uvicorn dashboard.app:app --port 5000` → http://127.0.0.1:5000) and the repo tab.
- [ ] Have a fresh Devin session open, pointed at this repo, ready to prompt.

## Demo Flow
1. Frame the problem: this is a real legacy ASP.NET MVC / EF6 app (`dotnet_source/`) and an empty Spring Boot skeleton (`java_target/`) — Devin modernizes it live.
2. Show the flowchart (`docs/flowchart.html`): seven phases with an **amber human checkpoint between each one** — this phased, human-in-the-loop process is the whole pitch.
3. Trigger Devin to start (Phase 1 intake → Phase 2 assessment); the dashboard shows complexity scoring and phases flipping to `done`.
4. Stop at the gates: Devin pauses at each amber checkpoint (assessment, mapping rules, architecture) — play the human, review, and approve to release the next phase.
5. Push the button on implementation: Devin generates Spring Boot code into `java_target/`, writes + runs equivalence tests in a fix loop, and opens the migration PR.
6. Close with the last two gates — approve the PR, approve the release — and show the modernized Java app as the output.

## Per-Phase Prompts (copy/paste to Devin)

Keep one Devin session open pointed at this repo and the dashboard tab visible
(`uvicorn dashboard.app:app --port 5000`). Each prompt does the real work for that phase
and flips the dashboard; `scripts/advance_demo.py` is the mechanical "button" that moves the
dashboard one notch (phase active→done, gate waiting→cleared). The amber gates are you approving.

**Trigger / Phase 1 — Intake**
> You're modernizing the legacy .NET app in `dotnet_source/` to the Spring Boot skeleton in `java_target/`. Start Phase 1: confirm the codebase scope (the OrderManagement ASP.NET MVC app + `database/`), then mark Phase 1 done on the dashboard by running `python scripts/advance_demo.py` until Phase 1 shows done. Don't start Phase 2 yet.

**Phase 2 — Assessment & Analysis**
> Run Phase 2: execute `python scripts/analyze_dotnet.py` and `python scripts/build_state.py`, then summarize the inventory (file count, LOC, complexity band, async/LINQ/EF/Razor counts). Advance the dashboard so Phase 2 is done and Gate 1 is waiting. Stop at the gate and present the assessment for my review.

_Gate 1 (Tech Lead approves):_ `Clear Gate 1 and continue.`

**Phase 3 — Construct Mapping**
> Run Phase 3 using `migration-playbook.md` and `docs/mapping_rules.json`: walk through the C#→Java / EF6→JPA / LINQ→Streams / async→CompletableFuture / Razor→Thymeleaf mappings for this app's constructs. Advance the dashboard so Phase 3 is done (mapping coverage fills) and Gate 2 is waiting. Stop for architect approval of the mapping rules.

_Gate 2 (Architect approves):_ `Clear Gate 2 and continue.`

**Phase 4 — Gap Analysis & Architecture**
> Run Phase 4: identify gaps (auth, stored proc `sp_OrderSummary`, decimal/rounding, lazy loading) and propose the target Spring Boot package architecture for `java_target/`. Advance the dashboard so Phase 4 is done and Gate 3 is waiting. Stop for architecture sign-off.

_Gate 3 (Architect signs off):_ `Clear Gate 3 and continue.`

**Phase 5 — Implementation ("push one button")**
> Run Phase 5: generate the idiomatic Java/Spring Boot code into `java_target/` (entities, JPA repositories, services preserving the pricing/credit/invoice logic, controllers) following the approved mappings. Compile it with JDK 17. Advance the dashboard so Phase 5 is done and the Java-files counter updates.

**Phase 6 — Testing & Validation**
> Run Phase 6: generate and run JUnit tests, including equivalence checks against the legacy pricing/tax/rounding rules; fix any failures in a loop until green. Advance the dashboard so Phase 6 is done, the test counters update, and Gate 4 is waiting. Open a migration PR and stop for PR review.

_Gate 4 (Reviewer approves the PR):_ `Clear Gate 4 and continue.`

**Phase 7 — Output / Release**
> Run Phase 7: finalize release/runbook docs for the modernized app, advance the dashboard so Phase 7 is done and Gate 5 is waiting. Stop for release approval.

_Gate 5 (Release Manager approves):_ `Clear Gate 5 — release.`
