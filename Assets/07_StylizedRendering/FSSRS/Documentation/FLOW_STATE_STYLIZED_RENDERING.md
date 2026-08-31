# FLOW STATE Stylized Rendering System

## Visual premise

FSSRS treats the world as an incomplete industrial print. The environment is restrained and structured, the player carries unresolved color registration, and graffiti remains the most saturated and authored layer.

## Render order

1. URP shadows, depth, normals, opaque geometry, and DBuffer decals.
2. `FLOWSTATE/FSSRS/Stylized Lit` quantizes lighting and applies stable print patterns.
3. `FSSRSRendererFeature` composites depth, normal, and luminance outlines before transparencies.
4. Transparent graffiti and particles render over the print treatment.
5. A restrained URP Volume performs final post-processing.
6. UI renders without FSSRS treatment.

## Isolation

- Production scene: `Assets/02_Escenas/Game.unity`.
- LookDev scene: `Assets/02_Escenas/Game_LookDev_FSSRS.unity`.
- Default PC renderer remains renderer index 0.
- FSSRS uses a duplicated renderer at index 1, selected only by the LookDev camera.
- Existing materials are never rewritten. LookDev materials are duplicated under `Materials/LookDev`.

## Core controls

- A `FlowPaletteProfile` defines the six color plates: paper, ink, shadow, mid, highlight, and accent.
- `FlowStatePaletteController` blends palette profiles without owning gameplay state.
- `FSSRSStylePreset` configures outline, print density, posterization, and palette influence.
- `FSSRSVolumeComponent` exposes the renderer controls and diagnostic views.

## Quality guidance

- Clean: silhouette and restrained grain.
- Comic: stronger internal edges and value quantization.
- Street: balanced production target for PC.
- Punk: high-density print texture for authored impact beats.
- Identity: stronger palette convergence and halftone.

Do not use Punk as a permanent global state. It is intentionally designed for short, high-energy moments.

## Performance rules

- Keep the composite to one full-screen pass.
- Disable normal edges and print layers first on constrained hardware.
- Profile while painting graffiti; its dynamic mesh rebuild is independent from FSSRS and can dominate CPU time.
- Avoid TAA until temporal stability has been evaluated. The LookDev camera starts with SMAA High.
- Mobile requires a reduced renderer path because its current pipeline does not provide the same depth and opaque inputs.

## Extension points

- Gameplay calls `FlowStatePaletteController.SetPalette` when an emotional state changes.
- A future impact controller should add temporary impulses over the base palette instead of replacing it.
- Graffiti response and the proposed Ink Debt effect should remain separate render modules.
