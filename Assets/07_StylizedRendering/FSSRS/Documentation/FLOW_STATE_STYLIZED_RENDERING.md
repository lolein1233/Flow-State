# FLOW STATE Stylized Rendering System

## Visual premise

FSSRS treats the world as an incomplete industrial print. Paper and ink establish the value structure, while cyan, magenta, acid yellow, and coral act as emotional color plates. Graffiti remains the most saturated and authored layer.

## Render order

1. URP shadows, depth, normals, opaque geometry, and DBuffer decals.
2. `FLOWSTATE/FSSRS/Stylized Lit` quantizes lighting and applies stable print patterns.
3. `FSSRSRendererFeature` composites depth, normal, luminance, and restrained color-registration edges before transparencies.
4. `PlayerComicBorder` draws paper, emotional color, and broken-ink silhouette plates for the player.
5. Other transparent graffiti and particles render over the print treatment.
6. A restrained URP Volume performs final post-processing.
7. UI renders without FSSRS treatment.

## Isolation

- Production scene: `Assets/02_Escenas/Game.unity`.
- LookDev scene: `Assets/02_Escenas/Game_LookDev_FSSRS.unity`.
- Default PC renderer remains renderer index 0.
- FSSRS uses a duplicated renderer at index 1, selected only by the LookDev camera.
- Existing materials are never rewritten. LookDev materials are duplicated under `Materials/LookDev`.

## Core controls

- A `FlowPaletteProfile` defines the six color plates: paper, ink, shadow, mid, highlight, and accent.
- `FlowStatePaletteController` blends palette profiles and exposes the five visual emotions without owning gameplay logic.
- `FSSRSStylePreset` configures outline, print density, posterization, and palette influence.
- `FSSRSVolumeComponent` exposes the renderer controls and diagnostic views.
- `PlayerComicBorder` is player-only. It is intentionally separate from combat target outlines.

## Quality guidance

- Clean: silhouette with no print texture.
- Comic: stronger internal edges and value quantization.
- Street: balanced production target for PC.
- Punk: high-density halftone, hatching, and sparse ink flecks for authored impact beats.
- Identity: stronger palette convergence and halftone.

Do not use Punk as a permanent global state. It is intentionally designed for short, high-energy moments.

## Performance rules

- Keep the composite to one full-screen pass.
- Disable normal edges and print layers first on constrained hardware.
- Profile while painting graffiti; its dynamic mesh rebuild is independent from FSSRS and can dominate CPU time.
- Avoid TAA until temporal stability has been evaluated. The LookDev camera starts with SMAA High.
- Mobile requires a reduced renderer path because its current pipeline does not provide the same depth and opaque inputs.

## Extension points

- Gameplay calls `FlowStatePaletteController.SetEmotion(FlowEmotion emotion)` when an emotional state changes. This updates the world palette and player border together.
- The controller Inspector exposes four live preview buttons: `Normal`, `B/N`, `Ira`, and `Flow max`. The selected preview is stored in the scene and is also used as the starting state in Play Mode.
- Parameterless methods `SetNormalState`, `SetMonochromeState`, `SetAngerState`, and `SetFlowMaximumState` can be connected directly to UnityEvents, animation events, or gameplay triggers.
- The player border uses compact paper, color, ink, and broken chromatic echo plates. The echo grows in traveling bands around the silhouette instead of separating the whole border from the mesh.
- Ink flecks and halftone dots are disabled in the current art direction; hatching and material breakup remain available without the scattered point noise.
- `SetPalette` remains available for non-emotional scripted color transitions.
- A future impact controller should add temporary impulses over the base palette instead of replacing it.
- Graffiti response and the proposed Ink Debt effect should remain separate render modules.

```csharp
paletteController.SetEmotion(FlowEmotion.Doubt);
paletteController.SetEmotion(FlowEmotion.Anger, 0.2f);
paletteController.SetEmotion(FlowEmotion.CreativeFlow, 0.65f);
```
