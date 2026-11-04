# Herramientas de diseño y validación en Unity

Consulta las skills indicadas cuando su tarea aplique; no cargues todas ni instales herramientas por esta lista. La exposición de un comando acredita capacidad disponible, no una operación ya verificada.

## Inspección y prototipo autorizado

1. Inspecciona con `rg` propietarios, dependencias, asmdefs, pruebas y valores serializados. Verifica Unity/paquetes reales en el proyecto. Usa [mapa de Creative Direction](../../flowstate-creative-direction/references/project-evidence.md) para localizar sistemas sin duplicar su inventario.
2. Para Editor usa `unity-mcp-orchestrator`: descubre instancias en `mcpforunity://instances` y comprueba `mcpforunity://project/info` y `mcpforunity://editor/state` para proyecto, escena, Play Mode, compilación y frescura. Con varias instancias fija FLOWSTATE. Si la información está obsoleta, contrasta con otra lectura; no añadas un segundo MCP.
3. Para CLI usa `unity:unity-cli`, descubre comandos mediante `unity command --query <término>` y fija `--project-path`. Etiqueta las llamadas con `--caller plugin --skill flowstate-game-design`. Ejemplo de lectura: `unity command editor status --project-path <raíz> --caller plugin --skill flowstate-game-design --format json`. No instales ni actualices Pipeline para comprobar estado.
4. Una evaluación puede usar solo archivos. No entres en Play Mode, cambies escenas, ejecutes builders o edites GameObjects por tener acceso a herramientas. En un prototipo autorizado modifica la porción acordada mediante las herramientas existentes y conserva referencias serializadas, GUID y límites Runtime/Editor.
5. Para APIs, consulta Context7 o documentación oficial de la versión real con preguntas genéricas, sin enviar código/assets del proyecto. Para observación visual local usa Computer Use si está disponible y corresponde. Guarda evidencia fuera de Assets; no publiques ni envíes recursos a servicios sin autorización.

## Skills oficiales y flujo de ingeniería

| Necesidad real | Apoyo pertinente |
| --- | --- |
| Colisión, soporte o triggers 3D | `unity:physics-3d-collision`; verificar detección y configuración antes de rediseñar la mecánica |
| Feedback HUD/menu | `unity:ui` y el framework existente; FLOWSTATE usa principalmente uGUI/TMP. Mantener la propiedad de información por estado |
| Feedback sonoro | `unity:audio-setup-mixers`, `unity:optimize-audio` según alcance; no presumir que hay un mixer o categorías ya creados |
| Animator, clips y Timeline | Herramientas de animación de MCP/CLI según catálogo y esquema actuales; inspeccionar rig/contactos y permisos antes de editar |
| Inspección de objetos y superficies | `unity:generate-editor-search-query` cuando se necesite búsqueda del Editor; `rg` para código/archivos |
| Cambio de postprocesado aprobado | `unity:urp-postprocessing`; consultar Creative Direction para intención, no trasladar identidad a esta skill |
| Coste medido de una variante | `unity-performance-evidence`; no declarar mejora por intuición o FPS de un frame |

Usa Superpowers de forma proporcional conforme AGENTS.md: brainstorming para incertidumbre de diseño, writing-plans para implementación compleja, systematic-debugging para fallos, TDD para regresiones significativas y verification-before-completion para afirmaciones finales. No impongas worktrees, commits, PR, selección de modelo o subagentes como ceremonia de cada mecánica. No cambies configuración global.

## Compilación, pruebas y evidencia

- Una edición autorizada de código exige compilación de Unity, espera de recarga/importación y lectura de consola sin borrar entradas. `dotnet` o un `.csproj` generado no acreditan compilación del juego.
- Usa Test Framework existente y las herramientas MCP de descubrimiento/ejecución disponibles, comprobando esquemas actuales. Un inventario o job iniciado no es un resultado: espera finalización y lee errores, fallos y totales. EditMode primero; después PlayMode/visual cuando lo requiera el comportamiento.
- Si Unity ya está abierto, prioriza ejecutar las pruebas por el Editor; no cierres el Editor del usuario para hacer sitio a una ejecución batch. No ejecutes suites durante un Play Mode ajeno ni des por aprobadas pruebas históricas.
- Pruebas cercanas: `Assets/Editor/MovementFeelTests.cs`, `ParkourDetectionTests.cs`, `GraffitiInteractionTests.cs`, `QuickGraffitiTests.cs`, `PresentationTeleportTests.cs`. Menús y rendering tienen sus propios asmdefs/tests. Revalida las referencias que use la mecánica.
- Distingue éxito funcional, evaluación visual, playtest con personas y profiling. Declara pruebas no ejecutadas, límites del dispositivo y contexto. Si falta una herramienta, continúa el trabajo independiente y deja la comprobación dependiente como pendiente, sin resultados inventados.

## Contrato con otras especialidades

Creative Direction mantiene pilares, identidad y registro de aprobación; Game Design especifica estados, incentivos, balance y experimento. Level Design recibe capacidades y restricciones espaciales, no una altura derivada del nombre de un asset. Animation recibe eventos de entrada/salida, contacto y cancelación, sin cederle autoridad del movimiento por defecto. UI/UX recibe jerarquía, prompts y prioridades de input por estado. Entrega briefs si esas skills aún no existen; no simules que están instaladas.
