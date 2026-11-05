# HimeBot Agent Guide

## Project context

HimeBot is a C# WPF application for OCR-driven game automation. The first supported game is Koihime Taisen. The user is building a C# project for the first time and wants to discuss the reasoning before implementing each small step.

Use Chinese for explanations and English for code identifiers. Introduce unfamiliar C# and WPF concepts when they first appear, with a short explanation tied to the current change.

## Teaching workspace

Treat `Tutorial/` as the reference implementation workspace. Place every agent-generated .NET solution, project, source file, test, asset, and project-level build configuration under `Tutorial/`. Keep repository-level instructions and documentation, including `AGENTS.md` and `docs/`, at the repository root.

The reference implementation supports learning rather than replacing it. Before generating each teaching-sized change, explain the command or file the user would create manually, the decision it represents, and how the user can reproduce or verify it. Create implementation code outside `Tutorial/` only after an explicit user request.

## Fixed technical baseline

- Keep the target framework at `.NET 7` with `net7.0-windows`.
- Keep WPF as the desktop UI framework and x64 as the default platform.
- Select NuGet package versions that support `.NET 7` and pin their versions.
- Change the framework version only after an explicit user request.
- Use MVVM for UI state and commands; keep game automation out of code-behind.

The fixed `.NET 7` target is a deliberate project constraint. Do not repeatedly suggest a framework upgrade while completing unrelated work.

## Collaboration workflow

Work in teaching-sized increments:

1. Inspect the current repository and the relevant documentation.
2. Explain the immediate goal, the design choice, and the files involved before a substantial code change.
3. Implement only the smallest vertical step requested by the user.
4. Build the affected project and run relevant tests when they exist.
5. Explain what changed, how to verify it, and the next logical step.

When the user asks to discuss or plan, provide options and trade-offs without modifying code. When the user directly asks for implementation, proceed with the agreed small step instead of scaffolding later phases.

Teach from the concrete code. For a new concept such as solution, project, namespace, interface, dependency injection, async/await, binding, command, or adapter, explain:

- what it is;
- why it is needed in the current step;
- where it lives in this repository;
- how the user can verify it works.

Prefer small patches that compile. Avoid introducing multiple frameworks or architectural patterns in the same step unless they are required for the requested result.

## Architecture

The planned architecture is a modular monolith with these projects under `Tutorial/src/`:

- `HimeBot.App`: WPF views, ViewModels, resources, and the composition root.
- `HimeBot.Core`: shared models and the small interfaces at external seams.
- `HimeBot.Game.Koihime`: Koihime game state, perception, task state machines, and semantic intents.
- `HimeBot.Platform.Windows`: window discovery, capture, DPI conversion, and input adapters.
- `HimeBot.Vision`: OCR, image preprocessing, and template matching adapters.

Keep references one-way:

```text
HimeBot.App
├─> HimeBot.Game.Koihime
├─> HimeBot.Platform.Windows
└─> HimeBot.Vision

HimeBot.Game.Koihime ──────> HimeBot.Core
HimeBot.Platform.Windows ──> HimeBot.Core
HimeBot.Vision ────────────> HimeBot.Core
```

Treat this as the target structure, not a requirement to create every project immediately. Add a project when the current implementation step needs its responsibility.

Keep modules deep: expose a small interface and hide OCR, Win32, retry, coordinate, and verification details in the implementation. Add an interface at a seam when at least two adapters are justified, usually a production adapter and a deterministic test adapter.

Game tasks produce semantic intents such as `OpenMissionPanel` or `ClaimReward`. Platform adapters translate intents into coordinates and input. Keep raw coordinates, fixed delays, and OCR-engine types out of task logic.

## Documentation map

Resume work by reading [`PROJECT_STATUS.md`](PROJECT_STATUS.md) at the start of a new conversation or before continuing implementation. It records the last verified milestone, unresolved decisions, and immediate next goal; update it after each completed learning milestone.

Read [`docs/project-structure.md`](docs/project-structure.md) before changing project layout, references, module responsibilities, the automation flow, or implementation phases. It is the single source of truth for the planned repository structure.

Organize future documentation as follows, creating a directory only when its first document is needed:

```text
docs/
├─ project-structure.md       # Architecture, directory plan, and implementation phases
├─ decisions/                 # Durable architecture decisions and their reasons
└─ guides/                    # Setup, debugging, capture, OCR, and release procedures
```

Update `docs/project-structure.md` in the same change when project layout or reference direction changes. Put a decision in `docs/decisions/` when the reason must survive beyond the current conversation. Put repeatable human procedures in `docs/guides/`.

## C# and WPF conventions

- Enable nullable reference types and implicit usings.
- Use async I/O and propagate `CancellationToken` through long-running automation.
- Keep the WPF UI thread responsive; run capture, OCR, and automation work off the UI thread.
- Marshal UI updates through binding-friendly progress or observable state rather than referencing WPF types from the automation modules.
- Dispose captured frames, native handles, and other owned resources deterministically.
- Store coordinates in game-client pixels and perform WPF DIP or screen-coordinate conversion inside the Windows module.
- Record OCR text, normalized values, confidence, regions, actions, retries, and failure screenshots for diagnosis.

## Verification

Once the solution exists, use these default checks from the repository root:

```powershell
dotnet build Tutorial/HimeBot.sln
dotnet test Tutorial/HimeBot.sln
```

A change is complete when the affected project builds, relevant tests pass, architecture documentation reflects structural changes, and the user receives beginner-friendly verification steps. If a check cannot run because the project has not been created yet, state that plainly instead of inventing a successful result.
