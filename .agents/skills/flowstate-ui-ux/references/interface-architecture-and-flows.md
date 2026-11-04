# Arquitectura y contratos de interfaz

## Propietarios que inspeccionar

Consulta selectivamente el [análisis fechado](../../../../Docs/Design/UIUX/Interface-Analysis-and-Proposals.md) y el [snapshot serializado](../../../../Docs/Design/UIUX/Serialized-UI-Snapshot-2026-10-28.json); no cargues el JSON completo si basta buscar un objeto/tipo.

| Interfaz | Propietarios y naturaleza | Límite importante |
|---|---|---|
| Principal/config/galería | Assets/09_MainMenu/Runtime: MainMenuController, MenuInput, MenuSubmenu, MenuTransition; geometría, materiales y TMP en mundo | UI actions clonadas, no uGUI convencional ni prueba de controles de gameplay |
| Pausa/ayuda | Assets/01_Scripts/PauseMenuController.cs, PunkMenuMotion, PunkMenuButton; uGUI/TMP | Prefiere menús editables existentes; fallback runtime no describe siempre la escena actual |
| Libreta | QuickGraffitiBook y GraffitiQuickPainter; páginas/botones/referencias serializadas | Cambiar página/selección/cerrar afecta foco, cursor, movimiento y pintura |
| Selector dibujado | GraffitiMenuDrawer, GraffitiDrawMenu, GraffitiDrawnMenuButton, GraffitiColorWheel/ValueSlider; meshes/colliders/raycast | Navegación uGUI no le añade soporte de mando automáticamente |
| Recursos | SprayResourceHUD generado por GraffitiPainter y datos SprayResourceSystem | Un mockup serializado puede desactivarse; editarlo no prueba cambiar HUD final |
| Contexto | GraffitiRaycastUI, GraffitiModeHudVisibility, GraffitiWallCursor | Detección de pared y rango real de pintura son estados distintos |
| Reputación | ReputationUI, ReputationSystem, Resources/UI/StreetReputationMenu.prefab | Prefab/API Open no prueban acceso runtime, foco, input o recompensas completas |

FSSRS integra impresión del mundo; UI overlay y textos/meshes 3D pueden tener caminos distintos. Verifica cámara/layer/sorting/stencil/post antes de atribuir contraste al shader. Conserva OWNED para gestos/títulos y tipografía de lectura donde esté configurada; no sustituyas fuentes globales por conveniencia.

## Contrato de flujo

Especifica para cada pantalla: quién abre/cierra, precondición, qué se muestra, foco inicial, acciones válidas y confirmación/cancelación, datos afectados y estado de retorno. Distingue foco de navegación, hover y selección de una obra; un destaque visual no es una confirmación.

Modela estados pertinentes: navegación, pintura libre/rápida, selector, libreta, pausa, ayuda y transiciones. Una matriz pequeña de entrada × estado ayuda a detectar conflicto de Q/E/B/Escape/rueda. No inventes un gestor modal obligatorio para una corrección puntual; primero entiende los guards actuales, orden de ejecución y bloqueos.

Comprueba que cerrar una capa consume la entrada cuando corresponde: Escape no debe cerrar/abrir múltiples pantallas ni el clic de elegir una obra convertirse en pintura involuntaria. Verifica foco restaurado, cursor, canMove, menuOpen, timescale y audio anteriores; no fuerces un estado genérico si otro modo lo había cambiado.

La pausa usa tiempo no escalado para transiciones y suspende input de FPS. Preserva ese contrato. La libreta bloquea movimiento, pero eso no demuestra pausa global del tiempo o del audio. No homogeneices ambos comportamientos sin decisión de Game Design.

## Datos e información

La UI consume estados de sus propietarios; no inventa progreso, stock o consecuencias. Considera normal, crítico, vacío, agitando, cambiando lata, acción bloqueada y recuperación. Deriva prompts de bindings reales y capacidades existentes: cambiar un glifo no implementa una entrada de mando ausente.

Para inventarios/mapas/diarios/progresión futuros, define datos, categorías, búsqueda, vacío/error, persistencia y retorno solo dentro del encargo. Evita imponer desbloqueos o puntuación de belleza desde la interfaz. La persistencia de reputación y la imagen guardada del menú no prueban guardado de pintura del mundo.
