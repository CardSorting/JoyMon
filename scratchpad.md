# JSDP TASK 005 — Tilemap Format + Starter Town Rendering

## Architecture Mapping

| Layer | Files | Purpose |
|-------|-------|---------|
| **Domain** (JoyMon.Core) | (none) | No new Core types — maps are infrastructure + rendering |
| **Content** (JoyMon.Content) | `MapContent.cs`, `MapLoader.cs` | JSON model + load/validate |
| **Game** (JoyMon.Game) | `TileAtlas.cs`, `MapRenderer.cs`, `Camera.cs` | Programmatic tileset, map drawing, viewport |
| **Data** (content/) | `maps/starter-town.json` | Seed map |

## Sovereign Triad Audit

### THE ARCHITECT
- Collision layer is parsed but not used by rendering — clean separation (readiness for future player movement).
- Map rendering logic lives entirely in Game, map validation lives in Content. No crossing.

### THE CRITIC
- Assumption: programmatic tileset texture generation works without external files. If TileAtlas fails to create the texture (e.g. GPU driver issues), rendering is blank. **Hardening**: TileAtlas uses `Texture2D.SetData` which always produces valid pixels; no asset path dependency.
- Assumption: MapLoader path is correct at runtime. **Hardening**: csproj copies JSON to output.

### THE SRE
- If map JSON is malformed, MapLoader throws `InvalidContentException` (same pattern as ContentLoader). Game1 catches at load time; fallback to an empty placeholder would be ideal but is out of scope.

*Double down on this concept, audit and revise in its entirety.*