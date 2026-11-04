# Validación — flowstate-vfx-gamefeel

Fecha: 2026-10-28. Resultado: instalada con alcance repo, estructura válida, habilitada y seleccionada implícitamente en una evaluación independiente. No se implementaron propuestas ni se reprodujeron efectos.

## Instalación y estructura

Ubicación: `C:/Users/alons/FLOWSTATE/.agents/skills/flowstate-vfx-gamefeel`.

```text
flowstate-vfx-gamefeel/
  SKILL.md
  agents/openai.yaml
  references/
    feedback-events-and-ownership.md
    stylized-vfx-production.md
    lifecycle-performance-and-validation.md
```

Creada con el inicializador oficial de skill-creator. `quick_validate.py`: **Skill is valid!**. Se utilizó Python/PyYAML ya disponible, sin instalación. Metadata contiene `$flowstate-vfx-gamefeel` y `policy.allow_implicit_invocation: true`. No se creó copia global ni se editaron otras skills.

Documentación de aplicación:

- [Auditoría y tres propuestas](../Design/VFX/VFX-and-Gamefeel-Analysis.md).
- [Snapshot de Editor y presets de código](../Design/VFX/Editor-VFX-Snapshot-2026-10-28.json).

El entrypoint es conciso; las tres referencias separan eventos/autoridad, producción visual y lifecycle/medición. La skill coordina las seis disciplinas existentes: no redefine reglas, recompensas, dirección artística, rigs, cámara o navegación desde VFX.

## Descubrimiento y selección automática

Consulta de Codex app-server local mediante `skills/list` y `forceReload: true`, sin crear chats o turnos del modelo:

| Contexto | Resultado observado |
|---|---|
| FLOWSTATE | Una flowstate-vfx-gamefeel, scope repo, enabled true, errores [] |
| TEMP externo | Ninguna flowstate-* |
| Convivencia | Las seis skills anteriores siguen habilitadas |

La política permite selección implícita y la descripción discrimina efectos/feedback de reglas o identidad. No obliga a usarla para cualquier pregunta de Unity.

## Evaluación independiente sin invocación explícita

Se inició un agente sin historial del autor ni respuesta esperada. Encargo natural: cierre visual discreto al terminar una obra y legibilidad del aerosol, con prototipo pequeño sin implementación. No se indicó nombre de skill objetivo. Se proporcionaron fuentes primarias y snapshot crudo; se excluyó el informe de conclusiones y se prohibieron escrituras/operaciones Unity.

**Selección observada:** eligió `flowstate-vfx-gamefeel` desde su catálogo inicial por pertinencia y leyó sus referencias. No la descubrió buscando archivos. Se acredita selección automática en ese caso real; no se promete activación universal en todos los contextos/clientes.

**Criterio demostrado:**

- Separó finalización rápida, pausa del trazo, ruta legacy y falta de criterio de finalización identificado para pintura libre.
- Propuso productor semántico en GraffitiQuickPainter al completar la pieza, independiente de reputación y sin volver a otorgarla; contempló reputación al tope, Load/Reset y evento único.
- Diferenció isPainting/permiso de emisión/pintura depositada y reconoció que dropout puede reactivar bursts sin nueva pulsación.
- No interpretó cero componentes VFX en Edit Mode como sistema ausente runtime ni recomendó sustituir el aerosol por Graph.
- Acotó una pieza rápida y dos comparaciones separadas: cierre impreso breve y alfa de niebla como variable única. Sus timings/valores se marcaron como candidatos de prueba, sin incorporarlos al juego ni aprobarlos.
- Contempló superficie, color/contraste, pausa/relojes, lifecycle, clipping/solapamiento y conservación de obra persistente.
- Separó pruebas funcionales, A/B visual y playtest; no afirmó mejora perceptual o de rendimiento.

No modificó archivos ni operó Unity o verificó conectividad por su cuenta; no ejecutó pruebas. Su resultado se ajustó a fuentes/alcance. No se detectó fallo de decisión que exigiera rehacer la skill.

## Herramientas y comprobaciones realizadas

MCP: instancia FLOWSTATE@3a6232501f358c97, Gameplay en edición, no playing/compiling, escena sin dirty. Se leyeron propiedades explícitas de ParticleSystem, materiales compartidos, referencias del painter, soporte de shader, renderer y assets VFX mediante execute_code en memoria. No se crearon scripts de inspección ni se llamaron Bind/Play/Emit/Simulate.

Shader de aerosol encontrado/isSupported=true en Editor y renderer index 1. Son comprobaciones del Editor actual, no prueba de variantes/inclusión en Player. VFX Graph 17.0.4 confirmado en Packages, sin .vfx/VisualEffectAsset encontrados en Assets ni VisualEffect en Gameplay cargada. No se instaló ni sustituyó ningún sistema.

Dispatcher local manage_vfx inspeccionado: consultas/edición/control de ParticleSystem, VFX Graph y Line/Trail. Las escrituras y reproducción no se ejercitaron. manage_profiler está expuesto, sin iniciar captura. La capacidad declarada/inspeccionada no equivale a operación de escritura o rendimiento verificados.

CLI oficial `editor status`: ready, compiling false, domainReloadInProgress false, playMode stopped, Unity 6000.0.41f1 y proyecto correcto. Sin actualización de CLI, Pipeline o paquetes. Computer Use queda como vía disponible para futura observación/input autorizados; no se operó la UI de Unity durante esta tarea.

Se observaron dos PNG guardados del proyecto como evidencia histórica de lata/HUD. No se usaron para certificar el aerosol actual, su emisión, timing, presión o legibilidad. No se tomaron nuevas capturas, ni se guardaron archivos en Assets.

La consola se leyó sin borrar y devolvió cero error/warning en esa comprobación. Las consultas en memoria completaron correctamente. Esto no equivale a compilar scripts del proyecto, ejecutar el juego o aprobar suites.

## Integridad y referencias

Manifiesto SHA256 al inicio: **3417 archivos preexistentes** bajo Assets, Packages, ProjectSettings, .agents, Docs y AGENTS. Comparación posterior: **cero modificados y cero ausentes**. Solo se añadieron la nueva skill y sus documentos. Se preservaron cambios previos de GraffitiPainter/GraffitiInteractionTests, AGENTS y todas las otras skills.

Validador oficial aprobado; metadata YAML/JSON parseables, prompt/policy correctos y enlaces locales comprobados sin destinos ausentes. Sin placeholders ni whitespace final en archivos nuevos. `git diff --check` sin errores; avisos LF/CRLF corresponden a los dos archivos ya modificados antes de esta tarea, sin alterarlos.

No se ejecutaron Play Mode, compilación del proyecto, tests, builds, perfilado, previews ni simulación. Se identificaron suites existentes para fases futuras, sin ratificar sus resultados históricos. Las constantes de capacidad/rate no se presentaron como mediciones o presupuestos aprobados.

## Capacidades y límites

La skill cubre efectos estilizados/ParticleSystem/Graph justificado, shaders dinámicos, partículas/impactos/trails/transiciones, feedback funcional/decorativo, timing/intensidad/recuperación, eventos, reutilización/pooling/lifecycle, URP/FSSRS, legibilidad, movimiento reducido y validación CPU/GPU/overdraw/memoria/GC.

Pendientes: observación temporal real, comprensión/satisfacción con jugadores, variantes accesibles verificadas, pruebas de pausa/completion/lifecycle, Player/plataforma objetivo y medidas de coste. Las tres propuestas siguen PROPUESTA, sin cambios o decisiones creativas definitivas.
