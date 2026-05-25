# JoyMon

A joy-monitoring (joystick-to-monitor) game project built with **MonoGame** and **.NET 8**. JoyMon is a modular game framework designed around strict architectural boundaries — keeping core logic framework-agnostic while layering rendering and platform concerns on top.

## Project Structure

```
JoyMon/
├── src/
│   ├── JoyMon.Game/       — MonoGame desktop application (entry point)
│   ├── JoyMon.Core/       — Pure domain logic, no framework dependency
│   └── JoyMon.Content/    — Game content library (sprites, audio, etc.)
├── tests/
│   └── JoyMon.Tests/      — xUnit test suite
├── content/                — Raw content assets (sprites, fonts, audio)
├── docs/
│   └── ARCHITECTURE.md    — Layer boundary documentation
└── JoyMon.sln             — Solution file
```

## Prerequisites

- .NET 8 SDK or later
- MonoGame 3.8.2+

## Build & Run

```bash
# Build everything
dotnet build

# Run the game
dotnet run --project src/JoyMon.Game

# Run tests
dotnet test
```

## Architecture Philosophy

JoyMon follows a strict layered architecture inspired by Domain-Driven Design and Joy-Zoning principles:

- **JoyMon.Core** — Pure C#. Zero framework dependencies. Contains game models, rules, and state machines that can be tested without mocking I/O.
- **JoyMon.Content** — MonoGame-aware library that wraps assets and depends on JoyMon.Core. Bridges domain types to game-ready representations.
- **JoyMon.Game** — The executable MonoGame application. Orchestrates Core and Content but contains no business logic.
- **JoyMon.Tests** — Validates Core and Content logic through xUnit.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for full boundary rules.