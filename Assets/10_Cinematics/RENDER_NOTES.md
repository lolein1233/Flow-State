# FLOW STATE cinematic — official logo graffiti revision

Final output: `Captures/FlowState_Logo_Graffiti_Cinematic_v6_FSSRS.mp4`.
73 seconds, H.264 1920x1080 at 30 fps, AAC stereo audio. Source music range 00:25–01:38.

V5 replaces the generic linear `FLOW` strokes and the separate final title card with one wall-integrated official FLOW STATE graffiti. `FlowStateLogoGraffitiReveal.shader` reveals the official transparent logo along a continuous, irregular paint front while Maikol's procedural arm, body and spray-can motion follows a fluid 520-point path. The completed logo remains physically on the brick wall for the final shots.

V6 fixes the actual FSSRS camera connection. The cinematic camera now always receives `UniversalAdditionalCameraData`, enables post-processing plus color/depth textures, sees every Volume layer and explicitly selects `FSSRS_PC_Renderer`. This avoids the source scene's duplicate `MainCamera` choosing a camera without URP additional data.

The current scene's global FSSRS Volume is forced to the maximum Creative Flow look (outline, posterization, ink, halftone, hatching, palette, paper and accent treatment). The on-screen diagnostic label was moved to the lower-left so it does not cover the artwork. The real graffiti selection UI remains in the early showcase.

V6 validation on 2026-10-01:

- The active recording camera reported URP post-processing enabled, color/depth textures enabled and Volume mask `-1`.
- Runtime renderer inspection showed `ScreenSpaceAmbientOcclusion`, `DecalRendererFeature` and `FSSRSRendererFeature/True` on the camera.
- During recording at 25.07 seconds, `FSSRSVolumeComponent.IsActive()` returned true.
- Encoded contact sheet: `Captures/FlowState_v6_FSSRS_contact.png`; visible outlines, posterization, halftone/ink flecks, palette shift and chromatic registration were checked across the menu, painting and final mural.
- ffprobe confirmed 73.000 seconds, H.264 1920x1080 at 30 fps and AAC 48 kHz stereo. Full decode completed without errors and Unity returned to Edit Mode with zero Console errors.

V5 validation on 2026-10-01:

- Preview frames at 14.5, 21.5, 30.5, 40.5, 50.5, 60.0 and 68.5 seconds were visually checked for the graffiti menu, painting pose, progressive logo reveal and final mural.
- Live recording inspection at 31.93 seconds confirmed Play Mode active, the wall logo active and `FlowStatePaletteController.CurrentEmotion == CreativeFlow`.
- Contact sheet: `Captures/FlowState_v5_contact.png`.
- ffprobe confirmed 73.000 seconds, H.264 1920x1080 at 30 fps, AAC 48 kHz stereo and an 8.09 Mbps combined bitrate.
- A full video/audio decode completed with no errors; Unity returned to Edit Mode and the post-render console contained zero errors.

The earlier preview-only paint disappeared because `GraffitiSurfaceCanvas.Awake()` replaced its transient mesh when entering Play Mode. The cinematic builder now saves every paint mesh under `Meshes/` and removes the runtime canvas component from these cinematic instances. Gameplay painting code is unchanged.

Maikol uses a deterministic procedural painting pose with arm/hand targeting, limited crouching and foot placement. The model faces its imported +Z direction toward the wall. Wrist and supporting-arm rotations retain the imported pose; ankle orientation is preserved after leg IK. The can follows the hand; the particle cone aims at the active paint point. Painting camera shots track the paint path. The opening and the final reveal show the completed mural. The original Spray Lab prefab, FSSRS emotions and official title texture are retained.

The v3 pose incorrectly turned the model almost 180 degrees away from the wall and forced its wrist and feet to rotate. V4 removes that reversal, changes the elbow pole to the forward/right side, and caps crouching at 0.48 units. Rendering explicitly restores the Cinemachine brain to LateUpdate to prevent a paused inspection's ManualUpdate state from freezing the recorded camera.

V4 validation on 2026-10-01:

- High, middle and low painting poses visually checked in the Editor and again in frames extracted from the final MP4 at 21.5, 30.5, 40.5 and 50.5 seconds. Evidence: `Captures/FlowState_v4_pose_check.png`.
- During recording at 55.6 seconds: model-forward dot wall-forward = 0.994; right toe direction dot wall-forward = 0.739; right hand was 0.40 units forward of the shoulder; hand viewport coordinates were (0.47, 0.28), inside the shot.
- ffprobe confirmed H.264 1920x1080, 30 fps, AAC audio, and duration 73.000 seconds.
- Recorder completed and returned to Edit Mode.

Previous paint persistence validation on 2026-09-30:

- Full Unity Recorder run completed and exited Play Mode.
- 226 persistent paint meshes confirmed during Play Mode; at 51.53 seconds, 205 paint groups and 2460 vertices were visible.
- Nine frames extracted from the encoded MP4 were visually inspected. Evidence: `Captures/FlowState_v3_contact.png`.
- ffprobe confirmed 73.000 seconds, 1920x1080, 30 fps, H.264 and AAC.
- Full video/audio decode completed. Audio mean -18.8 dB, peak -1.2 dB.
- Console retained the pre-existing URP Volume inspector `SerializedObjectNotCreatableException` messages. No different error appeared in the post-render error inspection.

Regenerate through `Flow State/Cinematics/01 Build Trailer Scene`, then `03 Render Trailer MP4`. The preview menu produces Editor stills; review frames from the encoded movie when verifying runtime behavior.
