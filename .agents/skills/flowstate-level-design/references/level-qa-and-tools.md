# QA de nivel, herramientas y colaboración

## Inspección y edición autorizada

Usa `rg` para scripts, serialización, propietarios, asmdefs y regresiones. Antes de Editor aplica `unity-mcp-orchestrator`: lee `mcpforunity://instances`, proyecto y estado; fija FLOWSTATE si hay varias instancias y contrasta snapshots obsoletos. No crees otro servidor para una desconexión.

Las consultas MCP de jerarquía/componentes y código en memoria deben ser de lectura en una auditoría: no llames Move/Teleport/SetActive, Physics.SyncTransforms, guardados, builders o creación de objetos para obtener métricas. Un snippet compilado en memoria para leer datos no es una compilación de scripts del juego ni un playtest. Prefiere consultas acotadas a serialización indiscriminada de getters de Animator/cámara, que ya produjo avisos de diagnóstico.

Para CLI aplica `unity:unity-cli`, descubre mediante `unity command --query <término>` y usa `--project-path <raíz>` con `--caller plugin --skill flowstate-level-design`. La disponibilidad de comandos no certifica escrituras. No instales/actualices Pipeline o paquetes para inspeccionar.

ProBuilder 6.1.2 figura en el manifest de esta revisión; revalida antes de usarlo. MCP ofrece `get_mesh_info`/`validate_mesh` como consultas y creación/edición como mutaciones; revisa el esquema real. Un collider válido no implica que el objeto sea ProBuilder. En un greybox autorizado usa el sistema existente cuando aporte geometría editable; no conviertas assets originales ni instales paquetes por defecto.

Para APIs usa Context7/documentación oficial de la versión real, con consultas genéricas. Para verificación visual local usa Computer Use cuando corresponda; capturas fuera de Assets. Si no hay vista útil o acceso al Editor, documenta límite y sigue con medidas de archivo sin certificar calidad visual. No envíes código/assets/capturas a servicios externos sin autorización.

## Pruebas y playtest

Define escenario, inicio, ruta, controles, configuración, evidencia y criterio antes de probar. Separa:

| Frente | Comprobación pertinente |
| --- | --- |
| Colisión/soporte | Cápsula y clearance real; layers, triggers, normals, juntas, recepción y barrido de trayectoria; revisar `unity:physics-3d-collision` ante fallos |
| Cámara/contactos | Entrada, salida, reversa, esquina, techo, compresión del boom, visibilidad de destino, pies/manos y transición a pintura |
| Recuperación | Caída, abandono, pausa, reentrada y retorno sin atrapamiento, pérdida de obra o inputs residuales; checkpoints solo si existen o están autorizados |
| Dificultad/aprendizaje | Acción prevista frente a intentada, fallos y causa, ayuda necesaria, tiempo de recuperación y mejora tras práctica |
| Orientación | Primera ruta elegida, destino/retorno entendidos, desvíos intencionales frente a desorientación, lectura de secretos y señales alternativas al color |
| Graffiti | Distancia efectiva, soporte de pieza, espacio de trabajo, cámara, luz/contraste y salida a explorar |
| Rendering/rendimiento | Visibilidad y oclusión tras arte; para coste, captura comparable CPU/GPU/memoria/GC mediante `unity-performance-evidence` |

Un espacio aparentemente abierto, un número de luces/colliders o una estadística de Editor no demuestran buen rendimiento. Define hardware/presupuesto y escenario antes de profiling. No abras una investigación de rendimiento para cada revisión espacial si no hay una pregunta técnica real.

Cuando haya código autorizado, sigue AGENTS.md: compila en Unity y lee consola sin limpiar; EditMode pertinente primero, PlayMode/visual según comportamiento. Pruebas cercanas: MovementFeelTests, ParkourDetectionTests, GraffitiInteractionTests, QuickGraffitiTests y PresentationTeleportTests en Assets/Editor. No ejecutes tests/juego innecesariamente en una entrega documental; un catálogo de tests no es una suite aprobada.

Para playtest con personas nuevas registra ruta, intentos, ayudas, caídas, causas y decisiones; distingue evaluación experta de aprendizaje real. Una muestra exploratoria pequeña descubre problemas, no acredita porcentajes generalizables. Adopta métricas cómodas a partir de intentos comparables, no solo un salto máximo exitoso. Refina una incertidumbre y vuelve a probar lo afectado.

## Entregables y coordinación

| Especialidad | Brief que recibe de Level Design |
| --- | --- |
| Creative Director | Propósito, coherencia FSSRS/Industrial Dusk, referencias y compromisos de lectura |
| Game Designer | Conexiones/métricas, acciones previstas, incentivos pendientes y coste de fallo; reglas nuevas siguen siendo su ámbito |
| Animation Director | Objetos/alturas reales, contacto, velocidades y transiciones problemáticas, cámara de observación |
| Lighting Artist | Grafo, orden de vistas, landmarks y lectura requerida; atmósfera que conservar |
| UI/UX Director | Prompts/señales por estado, accesibilidad, información de ruta y recuperación |
| Technical Artist | Materiales/renderers afectados, oclusión, capas y pregunta concreta de presupuesto/validación |

Usa las skills realmente disponibles; si una especialidad no tiene skill, entrega el brief sin inventar una dependencia. Superpowers se aplica proporcionalmente conforme AGENTS.md: planificación para cambios amplios, debugging ante fallo y verificación antes de afirmar resultados. No impongas commits, PR, worktrees o subagentes ni alteres configuración global.

Antes de integrar, entrega versión de layout, diferencias del greybox, métricas probadas/candidatas, resultados, fallos previos, reversión y pendientes. Aprobación del prototipo, aprobación artística y nivel aprobado para Gameplay son alcances distintos. La aceptación explícita del desarrollador debe cubrir la integración concreta; cita autorización vigente y evita preguntas repetidas.
