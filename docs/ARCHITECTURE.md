<!-- [LAYER: UI] -->

# JoyMon Architecture

## Layer Map

```
┌──────────────────────────────────────────────┐
│               JoyMon.Game                     │  ← MonoGame desktop app
│   (orchestration, rendering, input)           │     depends on Core + Content
├──────────────────────────────────────────────┤
│             JoyMon.Content                    │  ← MonoGame-aware library
│   (asset pipeline, sprite wrappers, audio)    │     depends on Core
├──────────────────────────────────────────────┤
│              JoyMon.Core                      │  ← Pure C# domain
│   (GameClock, Result<T>, game rules, state)   │     zero MonoGame dependency
├──────────────────────────────────────────────┤
│               JoyMon.Tests                    │  ← xUnit tests
│   (tests Core + Content)                      │
├──────────────────────────────────────────────┤
│               content/                        │  ← Raw asset files
│   (.png, .wav, .xnb sources)                 │     loaded at build time
└──────────────────────────────────────────────┘
```

## Boundary Rules

### ✅ JoyMon.Core (Domain Layer)

- **Pure C#** — no MonoGame, no XNA, no `Microsoft.Xna.Framework.*` imports
- Cannot reference any other project in the solution
- Only uses `System.*` namespaces
- Contains business logic, value objects, domain models, and pure state machines
- Must be testable with zero mocks — no I/O, no database, no rendering

### ✅ JoyMon.Content (Infrastructure / Content Layer)

- Can reference **JoyMon.Core** only (from source projects)
- May reference `MonoGame.Framework.DesktopGL` and `MonoGame.Content.Builder.Task`
- Contains asset loading, sprite batch wrappers, audio managers
- Bridges pure domain types from Core into MonoGame-compatible representations

### ✅ JoyMon.Game (Core / Application Layer)

- Can reference **JoyMon.Core** and **JoyMon.Content**
- References `MonoGame.Framework.DesktopGL` and `MonoGame.Content.Builder.Task`
- Orchestrates the game loop (`Game1.cs`)
- Contains NO business logic — delegates all domain work to Core
- Contains NO asset pipeline code — delegates all content work to Content

### ✅ JoyMon.Tests (Test Layer)

- References **JoyMon.Core** and **JoyMon.Content**
- xUnit-based test suite
- Tests both pure domain logic (Core) and content integration (Content)
- Does not test Game (no headless MonoGame runner at this stage)

### ✅ content/ directory

- Unprocessed game assets (images, sounds, fonts)
- Not compiled into any project directly — consumed through Content pipeline
- `.mgcb` files define which assets get processed

## Dependency Graph (What can import what)

```
JoyMon.Tests ────> JoyMon.Core
                └─> JoyMon.Content ──> JoyMon.Core

JoyMon.Game ─────> JoyMon.Core
                └─> JoyMon.Content ──> JoyMon.Core

JoyMon.Content ──> JoyMon.Core

JoyMon.Core ─────> (nothing — pure domain)
```

## Current Placeholder Types

| Type               | Layer  | Purpose                                     |
|--------------------|--------|---------------------------------------------|
| `GameClock`        | Core   | Pure domain clock — tracks elapsed time     |
| `Game1`            | Game   | Minimal MonoGame window with clock tick     |

## Evolution Guide

When adding a new system:

1. **Define the domain contract** in `JoyMon.Core` — pure C# interface or model
2. **Implement infrastructure** in `JoyMon.Content` — wrap assets, load files, bridge to MonoGame
3. **Wire it up** in `JoyMon.Game` — create, update, draw via the game loop
4. **Test it** in `JoyMon.Tests` — both domain logic and content integration

### Layer Violation Checklist

Before adding a `using` directive, ask:

1. **Am I in JoyMon.Core importing MonoGame?** → ❌ Move to Content or Game.
2. **Am I in JoyMon.Game writing business logic?** → ❌ Move to Core.
3. **Am I in JoyMon.Content reaching for raw file paths?** → ❌ Pass paths via Core abstractions.
4. **Can I test this without a GPU?** → If yes, put it in Core. If no, Content or Game.