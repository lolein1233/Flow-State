# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:/Users/alons/FLOWSTATE`
- Last analyzed: 2026-09-01
- Last analyzed commit: `34b3b50`
- Small third-person prototype centered on movement, graffiti, combat lock-on, music, and stylized rendering.

## Confirmed Environment

- Unity version: 6000.0.41f1
- Render pipeline: Universal Render Pipeline 17.0.4 with project FSSRS renderer additions
- Input system: Both (`activeInputHandler: 2`); gameplay code currently reads the legacy `UnityEngine.Input` API
- Target platform: Standalone Windows 64-bit

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.0.4 plus first-party FSSRS renderer feature | Confirmed | `Packages/manifest.json`, `Assets/07_StylizedRendering/FSSRS` |
| Input | Input System 1.13.1 installed; gameplay uses legacy input | Confirmed | `Packages/manifest.json`, `Assets/01_Scripts/FPSController.cs` |
| Tests | Unity Test Framework 1.4.6 | Confirmed | `Packages/manifest.json`, Unity MCP test inventory |
| Unity automation | Coplay Unity MCP connected and operational | Confirmed | `Packages/manifest.json`, Unity MCP editor state |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/01_Scripts` | Runtime gameplay MonoBehaviours | Confirmed | Source inspection |
| `Assets/02_Escenas` | Gameplay, intro, and look-development scenes | Confirmed | Scene inventory |
| `Assets/03_Prefabs` | Reusable gameplay and presentation prefabs | Confirmed | Repository structure |
| `Assets/06_Modelos/MAIKOL` | Player model, Animator Controller, and movement/parkour clips | Confirmed | Player Animator inspection |
| `Assets/07_StylizedRendering/FSSRS` | First-party renderer feature, editor tooling, and tests | Confirmed | Assembly definitions and source inspection |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `Assembly-CSharp` | Main gameplay code | Unity runtime, Input APIs | No gameplay asmdef; MonoBehaviour-centric |
| `FlowState.StylizedRendering` | Runtime stylized rendering | URP | Isolated by asmdef |
| `FlowState.StylizedRendering.Editor` | Renderer authoring tools | Runtime rendering assembly | Editor-only asmdef |
| `FSSRS.EditorTests` | EditMode rendering tests | Runtime/editor rendering assemblies | Test asmdef |

## Scenes And Startup Flow

- Build scenes: `Assets/02_Escenas/Game.unity` (index 0)
- Active development scene: `Assets/02_Escenas/Game_LookDev_FSSRS.unity`
- Likely startup flow: direct load of `Game.unity`; no separate bootstrap assembly was found

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Gameplay ownership | Scene-composed MonoBehaviours with direct Inspector references | Confirmed | `FPSController`, graffiti, music, and combat components on `Player` |
| Player movement | `FPSController` owns locomotion, camera, jump, parkour, and Animator parameters | Confirmed | `Assets/01_Scripts/FPSController.cs` |
| Parkour surfaces | Optional `ClimbableSurface` marker controls climb permission, side movement, and speed | Confirmed | `Assets/01_Scripts/ClimbableSurface.cs` |
| Animation | Generic Animator with script-owned movement and root motion disabled | Confirmed | `MAIKOL.controller`, player Animator component |

## Coding Conventions

- Namespace style: gameplay scripts are in the global namespace
- Serialized fields: public Inspector fields are common
- Async: coroutines for timed gameplay motion
- Comments/docs: concise Spanish headers; minimal inline comments

## Testing And Validation

- EditMode tests: three renderer tests discovered
- PlayMode tests: one top-level `FLOWSTATE` entry discovered; parkour has no dedicated regression coverage
- CI/build validation: no repository CI configuration found

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Editor state, console, scenes, GameObjects, assets, tests, Play Mode | available | Connected Unity MCP instance `FLOWSTATE@3a6232501f358c97` |
| Animation inspection and modification | available | Unity MCP animation and editor-code tools |
| Profiler and screenshots | available | Unity MCP profiler/camera tools |

## Important Constraints

- Preserve existing scene references and serialized field names.
- Keep movement authority in `FPSController`; Animator root motion remains disabled.
- Parkour changes must not alter graffiti, combat, music, UI, rendering, or project settings.
- Scene and animation asset edits require import, Console, and runtime validation.

## Unknowns And Confidence

- There is no existing parkour test suite or authored acceptance document.
- Controller/animation assets are generic rather than Humanoid, so bone paths are asset-specific.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/QualitySettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/01_Scripts/FPSController.cs`
- `Assets/01_Scripts/ClimbableSurface.cs`
- `Assets/06_Modelos/MAIKOL/MAIKOL.controller`
- `Assets/06_Modelos/MAIKOL/MAIKOL_Wall_TopOut.anim`
- `Assets/02_Escenas/Game_LookDev_FSSRS.unity`

<!-- unity-onboarding:generated:end -->
