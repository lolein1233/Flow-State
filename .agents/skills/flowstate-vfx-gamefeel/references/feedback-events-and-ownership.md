# Eventos, feedback y propietarios

## Cadena real de FLOWSTATE

Consulta selectivamente el [análisis](../../../../Docs/Design/VFX/VFX-and-Gamefeel-Analysis.md) y el [snapshot de lectura](../../../../Docs/Design/VFX/Editor-VFX-Snapshot-2026-10-28.json). No copies sus constantes como presupuestos aprobados.

| Acción/capa | Propietario | Contrato importante |
|---|---|---|
| Pintura permitida | GraffitiPainter + SprayResourceSystem | isPainting/input no equivale a CanEmitPaint o muestra depositada |
| Aerosol visual | GraffitiSprayVFX | Cuatro ParticleSystems, perfiles de boquilla/cono/distancia/presión/color; no deposita pintura |
| Pintura libre | Pintando + GraffitiSurfaceCanvas/stamps | Obra persistente, continuidad, capacidad y chorreos; no tratarla como FX descartable |
| Obra rápida | GraffitiQuickPainter + QuickGraffitiPiece | Progress/IsComplete y Advance true al completar; soltar pausa progreso |
| Recurso/HUD/lata | SprayResourceSystem, SprayResourceHUD, GraffitiAnimation | Estado, representación y sonidos tienen propietarios distintos |
| Parkour/cámara | FPSController | Motor y cámara actuales; Land puede anticiparse al impacto |
| Reputación | ReputationSystem | OnReputationChanged es notificación genérica, no evento de obra terminada |

GraffitiPainter crea/bindea sistemas runtime si faltan. En edición puede verse solo la partícula heredada inactiva; su escala/lifetime no describen el aerosol final. No llames a EnsureRuntimeSystems/Bind para completar un inventario de solo lectura.

## Contrato de evento

Define productor y momento de confirmación, datos mínimos, referencia/identidad de acción, posición/normal/superficie, intensidad permitida, reloj, consumidores y política de repetición/cancelación. Comprueba suscripción/desuscripción, reentrada y destrucción. No impongas un event bus global para un efecto puntual ni publiques eventos desde VFX como fuente de verdad jugable.

Para spray distingue presionar válvula, emisión estable, dropout, soltar, seco, agitado y cambio de lata. SetEmission false→true puede ocurrir por dropout, no solo por nueva pulsación. El código actual dispara burst al reactivarse: cualquier ajuste debe conservar permiso de pintura y evitar que VFX parezca emitir tinta cuando no puede depositarse.

Para aterrizar usa contacto/transición física confirmada y datos de caída pertinentes. Land del Animator es predictivo; Grounded true permanente tampoco es un evento por frame. Excluye inicialización, teletransporte, ajuste al suelo y cancelaciones que no sean un impacto. Las reglas de daño, duración o recuperación pertenecen a Game Design.

## Finalización y recompensas

En rápido, QuickGraffitiPiece llega a Progress 1; GraffitiQuickPainter.Advance corta StrokeActive y añade CityImpact 5 una vez en esa ruta. La ruta legacy de GraffitiFade también tiene finalización; el modo libre no tiene un criterio de «obra terminada» identificado en los propietarios inspeccionados.

No dispares celebración al soltar, abrir libreta, cambiar modo, agotar lata o recibir cualquier OnReputationChanged: carga/reset/otros cambios también notifican. Además, si reputación está al tope puede no emitirse cambio aunque una obra termine. El efecto necesita el evento semántico de esa obra, sin recalcular ni volver a otorgar la recompensa.

Si falta un evento específico, propón un punto de integración mínimo y deduplicación por pieza/acción, sin implementarlo en un encargo teórico. Para pintura libre, definir finalización manual/automática exige decisión de diseño; no la inventes desde una celebración visual. No ratifiques bucles o premios de propuestas anteriores.
