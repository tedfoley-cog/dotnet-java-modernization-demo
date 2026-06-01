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
