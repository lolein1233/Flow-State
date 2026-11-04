# Mapa de evidencia de FLOWSTATE

Referencia de navegación, revisada el 28 de octubre de 2026. Las rutas se expresan respecto de la raíz del repositorio; las líneas pueden cambiar. Busca los símbolos indicados con `rg` y comprueba el estado actual antes de decidir. La [biblia](../../../../Docs/Design/FLOWSTATE-Game-Art-Direction-Bible.md) es el documento creativo canónico inicial; este mapa evita duplicar su contenido.

| Pregunta | Fuente primaria y términos de búsqueda |
| --- | --- |
| Límites y versiones | `AGENTS.md`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectVersion.txt` |
| Flujo de producto | `ProjectSettings/EditorBuildSettings.asset`; `Assets/09_MainMenu/Options/00_JUGAR.asset` |
| Escena y tuning reales | `Assets/02_Escenas/Gameplay.unity`: `Player`, `graffitiMoveSpeed`, `walkSpeed`, `sprintSpeed`, `Parkour_Test_Blockout`, `Sewer_TP_Network` |
| Movimiento, parkour y cámara | `Assets/01_Scripts/FPSController.cs`: `StepMovement`, `coyoteTime`, `jumpBufferTime`, `LocomotionRate`, `SphereCastNonAlloc`; `ClimbableSurface.cs` |
| Autoría con aerosol | `Assets/01_Scripts/GraffitiPainter.cs`: `Start`, `EnsureRuntimeSystems`; `Pintando.cs`, `GraffitiGestureDynamics.cs`, `GraffitiSurfaceCanvas.cs`, `GraffitiWallCursor.cs` |
| Libreta y piezas rápidas | `Assets/01_Scripts/QuickGraffitiBook.cs`, `GraffitiQuickPainter.cs`, `QuickGraffitiPiece.cs`; `Assets/03_Prefabs/DrawPaintMenu.prefab` |
| Reputación y persistencia | `Assets/01_Scripts/Reputation/ReputationData.cs`, `ReputationSystem.cs`, `ReputationUI.cs`; `Assets/Resources/UI/StreetReputationMenu.prefab`; llamadas `AddCityImpact`, `AddRecognition`, `AddCrewRespect` |
| FSSRS y renderer | `Assets/Settings/PC_RPAsset.asset`: `m_RendererDataList`; `Assets/07_StylizedRendering/FSSRS/Renderer/FSSRS_PC_Renderer.asset`; Gameplay `m_RendererIndex`; `RendererFeatures/FSSRSRendererFeature.cs` |
| Paleta, emoción y silueta | `Assets/07_StylizedRendering/FSSRS/Runtime/FlowStatePaletteController.cs`: `FlowEmotion`, `SetEmotion`; `PlayerComicBorder.cs`; `Assets/01_Scripts/DemoEmotionMenu.cs` |
| Intención gráfica escrita | `Assets/07_StylizedRendering/FSSRS/Documentation/FLOW_STATE_STYLIZED_RENDERING.md`: `Visual premise`, `Render order`, `Ink flecks`; sus apartados de aislamiento y número de emociones requieren contraste |
| Industrial Dusk | `Assets/ArtDirection/IndustrialDusk/Gameplay_IndustrialDusk.asset`, `IndustrialDusk_Sky.mat`, `Practical_Amber.mat`, `Practical_Cyan.mat`, `Practical_ServiceGreen.mat`, `Practical_Violet.mat`; Gameplay `FLOWSTATE \| INDUSTRIAL DUSK`, niebla y Volume |
| Menú y accesibilidad | `Assets/09_MainMenu/Runtime/MenuInput.cs`, `MenuSubmenu.cs`, `MainMenuController.cs`, `MenuTransition.cs`; `Assets/01_Scripts/PauseMenuController.cs`, `GraffitiModeHudVisibility.cs`, `SprayResourceHUD.cs` |
| Animación | `Assets/06_Modelos/MAIKOL/MAIKOL.controller`; Animator en `Player/MAIKOL_Visual`; parámetros `Speed`, `Grounded`, `Parkour`, `Climbing`, `LocomotionRate`; clips y rig reales antes de proponer retargeting/IK |
| Spray, VFX y audio | `Assets/01_Scripts/GraffitiSprayVFX.cs`, `SprayResourceSystem.cs`: `muteSprayAudio`, `infiniteCanSupply`; `DiegeticMusicPlayer.cs`; `Assets/09_MainMenu/Runtime/MenuAudio.cs`; `Assets/08_Musica` |
| Trailer y presentación | `Assets/10_Cinematics/Scenes/FlowState_Trailer_Cinematic.unity`, `Timelines/FlowState_Trailer_25s_98s.playable`, `Runtime`, `RENDER_NOTES.md`; mantener evidencia histórica separada de gameplay |
| Regresiones existentes | `Assets/Editor/MovementFeelTests.cs`, `ParkourDetectionTests.cs`, `GraffitiInteractionTests.cs`, `QuickGraffitiTests.cs`, `PresentationTeleportTests.cs`; tests de `Assets/09_MainMenu` y FSSRS |

## Fuentes secundarias y referencias visuales

- `Docs/AI/Movement.md`: describe autoridad del controlador, cámara y cadencia; sus valores de velocidad difieren de Gameplay en esta revisión.
- `Docs/AI/UnityProjectContext.md`: inventario histórico de septiembre; inicio, escenas y cobertura de pruebas están desactualizados.
- `Docs/AI/MainMenu.md` y sus fases: intención y evolución del menú. El mapeo antiguo de emociones no sustituye al enum actual.
- `Docs/AI/GraffitiPhysicalSprayUpgrade.md`: intención del gesto, origen de boquilla, capas del aerosol y controles. Sus resultados de pruebas son históricos.
- `Docs/AI/GraffitiMenuNeoSprayLab.md`: decisiones editoriales del selector físico de caps y refinamientos.
- `Docs/AI/MainMenuPreview.png`, `Assets/Screenshots/fssrs-v4-border-compact-clean-b.png`, `Assets/Screenshots/GraffitiMenu_Refined_Close.png`: referencias históricas existentes; no pruebas actuales de calidad o rendimiento.
- Auditoría creativa previa de esta conversación: contexto inicial, no archivo independiente encontrado ni sustituto de las fuentes primarias. La biblia conserva sus hallazgos recontrastados y límites.

## Trampas de lectura ya observadas

- El recurso MCP de renderer features puede describir el renderer predeterminado. Comprueba también el índice de la cámara y su asset enlazado.
- Un script no expandido en YAML puede pertenecer a un prefab; un sistema ausente en serialización puede crearse al iniciar. Comprueba las referencias y el código de inicialización.
- Gameplay serializa `useDecalMode`, pero `GraffitiPainter.Start` puede cambiarlo. No derives el modo final del Inspector aislado.
- `StreetReputationMenu` existe como instancia de prefab inactiva; no diagnostiques ausencia de reputación por no encontrar su GUID de script en Gameplay.
- La lectura MCP de propiedades de Animator/cámara produjo avisos de getters/serialización durante la auditoría previa. Atribuye cada aviso mediante su stack y evita getters innecesarios; no limpies la consola.
- Disponibilidad MCP marcada `stale_status` exige corroboración; un catálogo de operaciones no demuestra que sus escrituras estén verificadas.
