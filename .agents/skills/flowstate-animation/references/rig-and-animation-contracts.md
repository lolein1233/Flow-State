# Rig, clips y contratos de animación

## Punto de partida

Revalida el [snapshot de Editor](../../../../Docs/Design/Animation/Editor-Animation-Snapshot-2026-10-28.json) cuando afecte a la tarea. MAIKOL está en Player/MAIKOL_Visual, usa Assets/06_Modelos/MAIKOL/MAIKOL.controller, rig Generic y Avatar null en la lectura de edición. FBX inspeccionados: Generic/NoAvatar. Clips referenciados: humanMotion=false, legacy=false, rutas de transform coincidentes con la jerarquía. Esto permite animación por rutas; no demuestra Humanoid, retargeting, corrección de orientaciones o calidad perceptual.

## Antes de retargetear o editar

Comprueba importer.animationType/avatarSetup/sourceAvatar, escala/unidades/ejes, jerarquía/bind pose, hueso raíz y skinning. Si existe Avatar Humanoid, verifica isValid/isHuman y mapeado; no lo presupongas por el nombre del FBX. Generic puede necesitar compatibilidad exacta de rutas; Humanoid requiere su contrato propio. Un Avatar null en Generic no es automáticamente un bug que deba «arreglarse» convirtiendo el rig.

Traza clip → estado/Blend Tree → Animator asignado → personaje real. Distingue clip suelto de clip usado; Idle actual referencia Happy Idle, aunque existan otros idles. Registra duración/sample rate, loop, root curves, eventos y paths. Verifica poses y contactos a varias fases cuando esté autorizado; ausencia de paths faltantes no prueba que el movimiento sea correcto.

Lee transición con todas sus condiciones, HasExitTime, duración fija/normalizada, interrupciones y prioridades. Un trigger enviado no prueba entrada al estado. Las velocidades de clip/estado, LocomotionRate, Animator.speed y escalas de Playables pueden cambiar la fase efectiva. Duración/speed es una aproximación estática; no incluye entrada, blending, interrupciones o velocidad variable.

## Autoridad del movimiento

FPSController gobierna CharacterController y trayectorias de vault/top-out/wall jump por código; Animator presenta huesos con applyRootMotion=false en la instancia observada. No actives Root Motion esperando corregir sliding: podría mover dos veces el cuerpo y romper casts, colisión, salida o distancias de Level Design. Cualquier cambio de autoridad requiere contrato con Game Design/programación y prueba de colisiones/recuperación.

La locomoción usa Blend Tree por Speed y LocomotionRate limitado a 1.15. Existen pruebas que preservan esa cadencia. No aumentes automáticamente velocidad de clips para igualar cápsula ni cambies locomoción de todo el juego por un fallo local de contacto; mide el compromiso entre cadencia, zancada y velocidad real.

RenderPoseController mezcla una pose sentada y AnimatorController mediante Playables; no es otro clip de locomoción. Revisa mezcla, cancelación, pausa y retorno antes de añadir capas que compitan por huesos. FlowStateCinematicPainter manipula huesos para la cinemática y puede resetear pose: no conectarlo al Animator jugable como IK genérico sin analizar escritores/orden.

## Capas, aditivos e IK

Evalúa masks, referencia aditiva, pesos, Write Defaults y huesos afectados antes de añadir una capa. MAIKOL observado tiene una capa base sin IK pass; no hay Animation Rigging declarado en Packages. `Animator.SetIK*` y Foot IK Humanoid no son una solución automática para su Generic actual. Considera primero poses/timing y adaptación acotada al rig compatible. Un solver procedural necesita límites, pole vectors, mezcla, orden posterior al Animator, contactos y reversión; no escribir root/cápsula desde corrección cosmética sin contrato explícito.
