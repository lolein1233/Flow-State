# Validación — flowstate-art-lighting

Fecha: 2026-10-28. Resultado: instalada en repositorio, estructura válida y selección implícita observada en una prueba independiente. No se implementaron las propuestas visuales.

## Instalación y estructura

Ubicación: `C:/Users/alons/FLOWSTATE/.agents/skills/flowstate-art-lighting`.

```text
flowstate-art-lighting/
  SKILL.md
  agents/openai.yaml
  references/
    visual-review-and-standards.md
    urp-fssrs-production.md
    lookdev-and-performance.md
```

Creada con `skill-creator/scripts/init_skill.py`; validada con `quick_validate.py` oficial: **Skill is valid!**. Se utilizó Python/PyYAML ya disponible en caché; no se instalaron paquetes. Metadata UI coherente y `policy.allow_implicit_invocation: true`. No se creó una copia global ni se editaron las otras skills.

Documentación de aplicación:

- [Estudio arquitectónico, visual y tres propuestas](../Design/ArtLighting/Rendering-Architecture-and-Visual-Review.md).
- [Snapshot de lecturas del Editor](../Design/ArtLighting/Editor-Rendering-Snapshot-2026-10-28.json).
- Cinco PNG originales bajo `Docs/Design/ArtLighting/Captures`, incluidos MainCamera y vistas auxiliares. No son comparaciones A/B de modificaciones.

## Descubrimiento real

Consulta local al app-server de Codex mediante `skills/list` con `forceReload: true`, sin crear conversaciones ni ejecutar turnos del modelo:

| Contexto | Resultado |
|---|---|
| C:/Users/alons/FLOWSTATE | Una flowstate-art-lighting, scope repo, enabled true, sin errores de carga |
| Carpeta TEMP externa | Ninguna skill flowstate-* |
| Convivencia | Creative Direction, Game Design y Level Design siguen habilitadas en FLOWSTATE |

La política permite selección automática; no implica que toda pregunta de Unity deba activarla. La prueba siguiente comprueba un caso natural dentro de su especialidad.

## Prueba independiente de invocación y criterio

Se inició un agente sin historial del autor de la skill ni respuesta esperada. Recibió un caso de Technical Art: evaluar legibilidad de lata/obstáculos en el PNG MainCamera y valorar una sugerencia de aumentar bloom global y sustituir materiales por otro toon. No se indicó el nombre de la nueva skill. Se permitió leer fuentes locales, sin escribir, operar Unity, Play Mode ni consultar el informe visual nuevo.

**Selección observada:** eligió `flowstate-art-lighting` como principal y `flowstate-creative-direction` como apoyo. Declaró que ambas figuraban ya en su catálogo inicial y que las seleccionó por descripción; después leyó sus instrucciones. Es una selección implícita comprobada para ese caso, no una promesa de selección universal en todos los chats o clientes.

**Resultados útiles:** distinguió observación e hipótesis; describió lata clara, planos de apoyo estrechos y competencia del logo; no afirmó clipping del PNG. Rechazó sustituir el toon sin causa y advirtió que más bloom podría reforzar masas ya dominantes. Propuso A/B de luz local con cámara/renderer/exposición fijos; dejó runtime, accesibilidad y coste pendientes. No operó Unity ni escribió archivos.

**Defecto detectado en el primer intento:** propuso un fixture de obstáculos con materiales FSSRS, aunque las fuentes registran URP Lit en los originales. Eso contaminaría una prueba de luz. El autor no dio por válida esa parte: añadió a la referencia LookDev conservación explícita de shaders originales y baseline A dentro del fixture. También delimitó que un entorno simplificado no prueba mejora frente a una captura de producción distinta.

**Revisión independiente:** al contrastar el paso con el snapshot primario, el agente reconoció la variable indebida y corrigió el protocolo: conservar URP Lit en suelo/vaults/muro y FSSRS en la lata, parámetros y contexto iguales; variar solo luz local. Distinguió renderer FSSRS de shader de cada material y trató BumpScale 8 como pista, no defecto probado. Esta prueba muestra un acierto de selección y un fallo de criterio corregido; no certifica infalibilidad.

## Referencias, dependencias y herramientas

Comprobados 22 enlaces locales de los cuatro Markdown de la skill y del informe: cero destinos ausentes. Snapshot JSON parseable. Las referencias a las tres skills existentes se resuelven; otros roles son colaboración mediante briefs, no dependencias instaladas. La skill de profiling existente se consulta solo cuando corresponde.

MCP de Unity existente: proyecto/instancia FLOWSTATE comprobados, Gameplay activa y sin dirty. Consultas explícitas y capturas funcionaron. CLI oficial `editor status`: ready, compiling false, domainReloadInProgress false, playMode stopped, Unity 6000.0.41f1, proyecto correcto. Las escrituras de luces/materiales/shaders no se ejercitaron ni se presentan como validadas.

Se comprobó visualmente la captura directa MainCamera 1920×1080. Pose, FOV, aspecto, tamaño, targetTexture y dirty iguales antes/después. Scene View fue usado como vista auxiliar y se restauró a pivot [1514.8667,135.83522,.005405029], quaternion identidad, size 471.4034, ortográfica/2D/sceneLighting/gizmos true. No se cambiaron GameObjects para encuadrar las vistas. La diferencia de renderer Scene View/MainCamera está documentada y evita una comparación inválida.

## Integridad y límites de verificación

Manifiesto SHA256 tomado antes de esta tarea: **3387 archivos preexistentes** de Assets, Packages, ProjectSettings, AGENTS, skills y Docs. Comparación posterior: **cero modificados y cero ausentes**. Solo se añadieron archivos de esta skill, su documentación y capturas fuera de Assets. Cambios ajenos de GraffitiPainter/GraffitiInteractionTests conservados. AGENTS y las otras skills permanecen intactos.

`git diff --check` sin errores de whitespace; avisos de conversión LF/CRLF de los dos archivos previamente modificados, sin tocarlos. Se validaron además los archivos nuevos, que al ser untracked no los cubre ese comando.

La consola se leyó sin borrar: cuatro warnings provenientes de consultas/serialización MCP (`cameraDepth`, dos accesos Animator IK y GetPlaybackTime fuera de playback). No se atribuyen al juego. Una primera consulta de encuadre falló al compilar en memoria por usar una sobrecarga inexistente de LookAtDirect; no ejecutó instrucciones. Se verificó la firma real, se corrigió y el viewport quedó restaurado. Esto no equivale a un error de compilación de scripts del proyecto.

No se ejecutaron compilación del proyecto, tests EditMode/PlayMode, builds, bakes, profiling, cambios de shader ni prototipos: es una tarea documental y de configuración de skill. No se declara mejora visual ni de rendimiento. Las tres propuestas permanecen PROPUESTA; cualquier integración necesita el alcance y revisión correspondientes.

## Capacidades adquiridas y pendientes

Adquiridas: análisis de arquitectura y capturas, jerarquía/composición/color, iluminación/sombras/probes, materiales/HLSL/Shader Graph, URP/Render Graph, postprocesado, LookDev reversible, protocolo A/B, legibilidad, accesibilidad y planificación de medición. Se conservan FSSRS e Industrial Dusk y la autoridad de Creative Direction.

Pendientes de evidencia en producción: calidad temporal y en acción, respuesta de probes por shader, paridad móvil, costes CPU/GPU/VRAM/GC en hardware objetivo y operación de escritura de cada herramienta concreta. Una skill mejora el procedimiento; no añade por sí sola herramientas, aprobación artística o resultados de pruebas no realizadas.
