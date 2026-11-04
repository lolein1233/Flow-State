# Validación — flowstate-animation

Fecha: 2026-10-28. Resultado: instalada exclusivamente en el repositorio, estructura válida, descubrimiento e invocación implícita observados y evaluación independiente completada sin cambios del juego.

## Ubicación y estructura

`C:/Users/alons/FLOWSTATE/.agents/skills/flowstate-animation`

```text
flowstate-animation/
  SKILL.md
  agents/openai.yaml
  references/
    rig-and-animation-contracts.md
    parkour-contacts-and-feel.md
    camera-and-cinematics.md
    production-and-validation.md
```

Creada con `skill-creator/scripts/init_skill.py`; validada con `quick_validate.py` oficial: **Skill is valid!**. Python/PyYAML ya disponibles, sin instalación. Metadata contiene `$flowstate-animation` y `allow_implicit_invocation: true`.

Documentación de aplicación:

- [Análisis de MAIKOL, cámaras y tres propuestas](../Design/Animation/Animation-and-Camera-Analysis.md).
- [Snapshot de consultas del Editor y escena cinematográfica](../Design/Animation/Editor-Animation-Snapshot-2026-10-28.json).

Las cuatro referencias cubren problemas distintos y se cargan selectivamente. Las cinco skills existentes conservan su responsabilidad: identidad/autoridad, reglas, espacio/métricas, imagen/rendering y UI/interacción. La nueva skill se ocupa de poses, presentación temporal, rig, contactos y sincronización.

## Descubrimiento y uso automático

Consulta local de Codex app-server `skills/list`, con `forceReload: true`, sin crear chats/turnos del modelo:

| Contexto | Resultado |
|---|---|
| FLOWSTATE | Una flowstate-animation, scope repo, enabled true; errores [] |
| TEMP externo | Ninguna flowstate-* |
| Convivencia | Creative Direction, Game Design, Level Design, Art & Lighting y UI/UX siguen habilitadas |

No se creó copia global ni se modificaron AGENTS u otras skills. La política permite selección automática; no obliga a usarla en preguntas ajenas a animación.

## Evaluación independiente

Agente sin historial del autor ni respuesta esperada. Encargo natural: revisar apoyo de manos de MAIKOL al pasar un obstáculo y vuelta a caminar, manteniendo control/estilo, con diagnóstico y prototipo antes de producir cambios. No se indicó el nombre de la nueva skill. Se proporcionaron fuentes primarias y el snapshot crudo; se excluyó el informe de conclusiones. Se prohibieron escrituras y operaciones de Unity/Blender.

**Selección observada:** eligió `flowstate-animation` por su descripción desde el catálogo inicial y leyó sus referencias pertinentes. No la descubrió mediante búsqueda de archivos. Se acredita invocación implícita en ese caso concreto, sin prometer selección universal en otros contextos.

**Criterio demostrado:**

- Separó duración serializada de defaults: VaultMedium .62 s físico frente a .619 s aproximados del clip/velocidad; no diagnosticó desfase por mirar .42 s del código.
- No confundió Generic/Avatar null con Humanoid ni propuso convertir rig, activar Root Motion o IK Humanoid automáticamente.
- Distinguió compatibilidad de rutas y coordinación de duración de contacto real de palma/borde.
- Identificó salida a Idle y reinicio de velocidad como hipótesis; advirtió que una transición Speed>0 puede no activarse en el momento esperado.
- Acotó prototipo a un vault/obstáculo, conservando trayectoria y cámara FPSController; no impuso Cinemachine o una toma de control cinematográfica.
- Propuso medir contacto, penetración, primer paso y respuesta, con A/B temporal y cámaras pertinentes; contempló pausa/interrupción y RenderPoseController.
- Separó test funcional, observación visual y aprobación. No afirmó fluidez o mejora conseguida.

No escribió archivos, operó Unity/Blender ni ejecutó pruebas. La respuesta fue coherente con límites y fuentes; no se detectó un fallo de decisión que exigiera rehacer la skill.

## Herramientas comprobadas

Unity MCP: una instancia FLOWSTATE@3a6232501f358c97, Unity 6000.0.41f1, Gameplay en edición, sin dirty. Custom tools presentes, incluidas execute_code y manage_animation. Consultas explícitas en memoria leyeron Animator, Avatar, skin, controller, clips/bindings, importers, FPSController y Timeline, sin scripts escritos ni poses muestreadas.

CLI oficial `editor status`: ready, compiling false, domainReloadInProgress false, playMode stopped y proyecto correcto. No se actualizó CLI, Pipeline ni paquetes.

Computer Use: ventana de Unity inicialmente minimizada; se activó y observó Game View en edición con ayuda visible. No se reprodujo ni se encuadró una maniobra. El contact sheet v6 del trailer se observó como imagen histórica, sin usarlo como prueba de parkour actual.

Blender health check: **connected=false**. Las herramientas existen en el catálogo, pero no se acreditan conexión, rig editable, exportación o retargeting DCC. Animation Rigging no figura declarado en manifest/lock ni se encontraron componentes en Gameplay cargada; no se instaló.

Se inspeccionó el dispatcher local de manage_animation: consultas y operaciones sobre estados/transiciones/layers/Blend Trees/curvas/eventos. Sus escrituras no se probaron en esta tarea. Lectura operativa, implementación inspeccionada y escritura verificada son niveles de evidencia diferentes.

## Incidencias de consultas, sin atribuirlas al juego

La primera consulta extensa de clips falló al compilar en memoria porque usó tres propiedades inexistentes de AnimationClipSettings. No ejecutó instrucciones. Se retiraron esas propiedades y se leyó solo la información soportada.

Una consulta de referencias intentó leer name de un UnityEngine.Object no asignado usando un guard de null inadecuado. cameraOverride es opcional y tiene fallback en FPSController; ese error fue de inspección, no un bug del juego demostrado. Se corrigió usando la semántica Unity `obj == null` y la lectura completó. Ninguna de esas consultas contenía escrituras.

La consola se leyó sin borrar y devolvió cero entradas error/warning en la comprobación. No se confunde con haber compilado o probado toda la aplicación.

## Integridad y validación documental

Manifiesto SHA256 de inicio: **3408 archivos preexistentes** de Assets, Packages, ProjectSettings, .agents, Docs y AGENTS. Comparación posterior: **cero modificados y cero ausentes**. Solo se añadieron esta skill y sus documentos. Los cambios previos en GraffitiPainter y GraffitiInteractionTests se preservaron.

Validador oficial aprobado, JSON parseable y enlaces locales a skills/recursos/documentos comprobados. Metadata YAML válida, prompt correcto y descripción breve dentro de longitud. Sin placeholders ni whitespace final en archivos nuevos. `git diff --check` no mostró errores; sus avisos LF/CRLF pertenecen a los dos archivos modificados previamente, sin tocarlos.

No se ejecutaron Play Mode, previews, Animator.Play/Update/Rebind, SampleAnimation, Timeline.Evaluate, builds, bakes o profiling. No se cambiaron escenas, rigs, clips, controllers, cámaras, materiales, scripts o configuraciones. No se compilaron scripts del proyecto ni se ejecutaron suites: las compilaciones mencionadas corresponden solo a consultas Roslyn en memoria. Se localizaron pruebas relevantes sin declararlas aprobadas.

## Capacidades adquiridas y pendientes

La skill cubre principios de poses/timing/spacing/peso/arcos/follow-through; locomoción/blends/capas/aditivos, rig/IK/retargeting compatible, autoridad root/cápsula, contactos/sliding/clipping, manos/lata/procedural, eventos audio/VFX, cámara/Timeline/Cinemachine, producción Blender/FBX y medición/QA.

Pendientes: observación temporal actual, medidas reales de contacto y recuperación, pruebas de control/cámara/accesibilidad, exportación/reimportación, retargeting validado y rendimiento en hardware objetivo. Las tres mejoras se mantienen PROPUESTA; el análisis y la skill no autorizan implementación ni certifican calidad profesional conseguida.
