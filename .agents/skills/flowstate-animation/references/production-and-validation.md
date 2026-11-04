# Producción, herramientas y validación

## Herramientas reales

Prioriza Unity MCP existente: comprueba instancia/proyecto/estado y descubre custom tools. Consultas de Animator/Avatar/importer/clips/controller deben leer propiedades explícitas; no serialices getters IK/playback ni recorras todo el objeto sin filtros. UnityEngine.Object tiene semántica especial de null: comprueba `obj == null` antes de leer name/propiedades. Una consulta fallida no demuestra bug del juego.

MCP ofrece manage_animation y execute_code en la sesión inspeccionada; verifica esquema/capacidad concreta antes de usar. CLI oficial complementa tareas repetibles con ayuda/project-path. Consultas en memoria pueden leer datos sin crear scripts, pero también pueden mutar poses: revisa código y limita la acción. No llames Animator.Play/Update/Rebind, SampleAnimation o PlayableDirector.Evaluate en una auditoría de solo lectura.

El dispatcher local de manage_animation distingue `animator_get_info/get_parameter`, `controller_get_info` y `clip_get_info` de acciones de escritura: controller_add_state/add_transition/add_layer/create_blend_tree_1d/2d y clip_create/set_curve/add_event, entre otras. Descubre parámetros y verifica autorización antes de invocarlas; inspeccionar su implementación no acredita haber probado escrituras. Play/crossfade/set_parameter también cambian estado aunque no guarden un asset.

Computer Use permite verificación local y pruebas de input autorizadas. Usa las skills oficiales pertinentes de Unity y documentación/Context7 para APIs reales; consultas externas genéricas sin recursos del proyecto. Superpowers se aplica proporcionalmente según AGENTS, sin ceremonias o cambios globales obligatorios.

## Blender y FBX

Verifica bridge/session y documento objetivo antes de operar. Health check de esta auditoría devolvió connected=false: no se acreditan edición/exportación de MAIKOL ni retargeting DCC. No actives addons, importes modelos o arranques servicios sin alcance autorizado. Las herramientas de generación/descarga de assets no son necesarias para animación de este rig.

Con producción autorizada, conserva fuente y exporta variante acotada; verifica jerarquía/nombres, bind/rest pose, unidades/ejes/escala, root bone, huesos deformantes, constraints bakeadas, rango/FPS, loops y referencias. No apliques transforms/rest pose indiscriminadamente a un rig con clips existentes. Si no hay fuente editable de MAIKOL, declara la limitación antes de prometer modificación de rig.

Reimportar FBX puede afectar subasset fileIDs, Avatar, skinning y clips que comparten origen. Compara referencias/import settings y poses tras round-trip en Unity; no sobrescribas el FBX base ni conviertas Generic→Humanoid por defecto. Revisa compresión/curvas/optimizeGameObjects solo con evidencia de contacto/calidad y dependencias de huesos accesibles.

## Probar sin confundir evidencias

Para cambios autorizados aplica AGENTS: compilación real en Unity, consola sin borrar, EditMode relevante y PlayMode/observación cuando corresponda. Fuentes existentes incluyen MovementFeelTests, ParkourDetectionTests, GraffitiInteractionTests y suites de menú. Confirma nombres/cobertura antes de ejecutar; busca cobertura pertinente de RenderPoseController sin inventar una suite dedicada. No declares pruebas pasadas por existir.

Registra baseline/revisión, rig/controller/clip, datos de escena, input/ruta, frecuencia/resolución, cámara, pose/fase y estado físico. Captura movimiento a velocidad normal y cuadro a cuadro; compara condiciones iguales. Verifica interrupción, pausa, pérdida de contacto, aterrizaje, entrada mantenida y retorno a pintura. Playtest observa comprensión/control/satisfacción, no solo que un estado se alcanzó.

Solo con mediciones comparables mediante `unity-performance-evidence` afirma optimización: Animator/Playables, skinning, procedural/IK, CPU/GPU/memoria/GC y hardware objetivo según la pregunta. Menos huesos/curvas/capas no prueban mejor rendimiento ni calidad conservada. Evita optimización a ciegas.

## Entrega

Proporciona estado de evidencia, función jugable, recursos/autoridades, poses/timing/contactos, propuesta, riesgos, prototipo/reversión y aceptación. Expón pruebas ejecutadas/no ejecutadas, imágenes/clips con contexto y aprobación pendiente. La skill no instala Animation Rigging, habilita Blender ni acredita automáticamente que una animación sea profesional.
