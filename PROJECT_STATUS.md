# HimeBot Project Status

> Last verified: 2026-11-05

This file is the handoff for resuming HimeBot development in a new conversation. Read `AGENTS.md` first, then this file. Read `docs/project-structure.md` before changing layout, references, module responsibilities, automation flow, or implementation phases.

## Workspace roles

- The repository-root `HimeBot.sln` is the active hands-on project created by the user in Visual Studio. Guide the user to make teaching-sized changes here; do not silently replace their work.
- `Tutorial/` is the Agent-generated reference implementation required by `AGENTS.md`. It is not the active hands-on project unless the user explicitly asks to use it.
- `learn/` contains the user's learning notes. Preserve them unless the user explicitly requests an edit.

## Current milestone

The empty architecture required for the first feature is complete. The feature itself has not been implemented.

The intended first feature is:

```text
Select or configure the game directory
    → locate the game executable
    → start the game
    → report success or a useful failure
```

## Completed in the latest learning session

The user created and configured the following manually in Visual Studio:

1. Root solution `HimeBot.sln`.
2. WPF executable project `src/HimeBot.App`.
3. Shared `Directory.Build.props` for `.NET 7`, `net7.0-windows`, x64, nullable reference types, and implicit usings.
4. Class library `src/HimeBot.Core`.
5. Class library `src/HimeBot.Platform.Windows`.
6. One-way project references with no cycle.
7. Corrected `HimeBot.Platform.Windows` after it was initially created outside `src/`.

No game-launching, directory-picker, MVVM command, OCR, capture, or input code exists yet.

## Current repository shape

```text
HimeBot/
├─ AGENTS.md
├─ PROJECT_STATUS.md
├─ Directory.Build.props
├─ HimeBot.sln
├─ docs/
│  └─ project-structure.md
├─ learn/
│  └─ 如何安全移动项目.md
├─ src/
│  ├─ HimeBot.App/
│  ├─ HimeBot.Core/
│  └─ HimeBot.Platform.Windows/
└─ Tutorial/                  # Agent-generated reference project
```

`HimeBot.App` still contains the default WPF `App.xaml` and `MainWindow.xaml`. `HimeBot.Core` and `HimeBot.Platform.Windows` contain no functional C# source files.

## Verified project references

```text
HimeBot.App
├─> HimeBot.Core
└─> HimeBot.Platform.Windows
                      └─> HimeBot.Core
```

- `HimeBot.Core` references no project.
- `HimeBot.Platform.Windows` references only `HimeBot.Core`.
- `HimeBot.App` references both lower modules and remains the future composition root.

## Shared build baseline

Root `Directory.Build.props` currently provides:

```xml
<TargetFramework>net7.0-windows</TargetFramework>
<Platforms>x64</Platforms>
<PlatformTarget>x64</PlatformTarget>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
```

The Solution still contains both `Any CPU` and `x64` configuration names, but the project baseline and intended active platform are x64. Use an explicit x64 build when verifying from the command line.

The two class-library project files retain harmless empty `<PropertyGroup>` elements. They may be removed during later cleanup but do not block development.

## Last verification

Run from the repository root:

```powershell
dotnet build HimeBot.sln --configuration Debug --property:Platform=x64
```

Last result:

```text
HimeBot.Core              -> net7.0-windows/HimeBot.Core.dll
HimeBot.Platform.Windows  -> net7.0-windows/HimeBot.Platform.Windows.dll
HimeBot.App               -> net7.0-windows/HimeBot.App.dll

Build succeeded: 0 warnings, 0 errors
```

There are no test projects or test cases yet.

## Immediate next goal

Before writing feature code, discuss and settle these requirements with the user:

1. Does the user choose a game folder or the executable file directly?
2. What is the exact executable filename?
3. Must HimeBot run a launcher or the game executable itself?
4. Are command-line arguments or elevated permissions required?

Completion criterion for the next step: the four launch rules are known, and the responsibility and file placement of the smallest launch contract are agreed before implementation begins.

Likely later locations, subject to that discussion:

- `src/HimeBot.Core/Ports/`: small launch seam if both a Windows adapter and deterministic test adapter are justified.
- `src/HimeBot.Platform.Windows/Launching/`: path validation and Windows process-launch adapter.
- `src/HimeBot.App/ViewModels/`: user-selected path, launch command, and observable status.

Do not create `HimeBot.Game.Koihime`, `HimeBot.Vision`, OCR, capture, or input code for this launch-only step.
