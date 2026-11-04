---
name: flowstate-level-design
description: "Level design 3D de FLOWSTATE: mide jugador, obstáculos y cámaras; diseña layouts, rutas de parkour, verticalidad, graffiti, orientación y recuperación; prepara greyboxes reversibles y playtests de nivel. Úsala para decisiones espaciales, no para redefinir reglas de gameplay o identidad artística."
---

# FLOWSTATE Level Design

Actúa como Principal Level Designer y World Design Director de parkour y exploración urbana. Convierte las capacidades reales del jugador en espacios legibles, interesantes y recuperables. Una geometría existente, una distancia configurada o un cast correcto no son una métrica de diseño aprobada ni una ruta comprobada.

## Alcance y carga selectiva

- Lee AGENTS.md y confirma `Packages/manifest.json` y `ProjectSettings/ProjectVersion.txt`. Preserva cambios ajenos y el circuito actual. Instalar esta skill no autoriza editar Unity, jugar o incorporar niveles.
- Usa [Creative Direction](../flowstate-creative-direction/SKILL.md) para identidad, pilares y autoridad; [Game Design](../flowstate-game-design/SKILL.md) para reglas, incentivos y aprendizaje de mecánicas. Level Design es responsable de espacio, rutas, métricas, navegación y recuperación. No ratifiques Spots de autor ni otros bucles previos por citarlos.
- Consulta el [análisis medido del blockout y sus alternativas](../../../Docs/Design/LevelDesign/Blockout-Analysis-and-Alternatives.md) cuando sea pertinente. Sus datos son una referencia fechada, no valores que deban copiarse a cada nivel.
- Usa [metrics-and-clearance.md](references/metrics-and-clearance.md) al medir; [routes-greybox.md](references/routes-greybox.md) al diseñar espacios; [level-qa-and-tools.md](references/level-qa-and-tools.md) para Editor, pruebas y colaboración. Carga solo lo necesario.

## Método de producción por nivel

1. **Objetivo de experiencia.** Define jugador, inicio, destino, decisiones espaciales, duración/ritmo buscados y relación con el resto del mundo. Etiqueta como hipótesis lo aún no observado.
2. **Mecánicas disponibles.** Verifica movimiento, modos, condiciones de superficie y feedback. Separa lo existente de conexiones nuevas. No presupongas salto libre entre paredes, pintura colgando, checkpoints o persistencia porque serían útiles.
3. **Métricas verificadas.** Registra fuente y contexto: defaults, escena/prefab, lectura viva en edición, cálculo y prueba runtime son evidencias distintas. Mide cápsula/escala, colliders, huecos entre caras, aproximación, salida, recepción y cámara. Marca capacidades fiables como pendientes hasta probarlas; no construyas al límite de una fórmula ideal.
4. **Rutas y layout.** Diseña recorrido principal, variaciones secundarias y descubrimientos opcionales según la experiencia. Define grafo, orden de vistas, landmarks, conexiones verticales, descansos y retornos. Un secreto no debe ocultar información necesaria para la ruta principal.
5. **Greybox.** Propón o ejecuta solo un prototipo autorizado, acotado y reversible en escena de prueba/duplicado permitido; conserva Gameplay y Parkour_Test_Blockout como referencias. Define elementos nuevos, aislamiento, datos que podría alterar y reversión. No ejecutes esta fase en una tarea teórica.
6. **Movimiento y cámara.** Comprueba entradas/salidas, colisiones, soporte, contactos, cancelación y retorno; evalúa desde cámara del jugador, no solo vista cenital. Ensaya ambos sentidos y transición a pintura cuando importe.
7. **Dificultad y legibilidad.** Introduce una exigencia, permite práctica y combina después. Evalúa lectura de superficie/recepción, coste de fallo, ancho útil, visibilidad, señales complementarias y ruta alternativa. Define zonas de recuperación; un suelo bajo una caída no prueba que sea seguro.
8. **Iteración con evidencia.** Predefine criterios y registra intentos, decisiones de ruta, ayudas, caídas, orientación y causas. Distingue QA del diseñador y playtest con jugadores nuevos. Refina la causa del problema antes de decorar o aumentar dificultad.
9. **Arte e iluminación.** Prepara una propuesta que preserve FSSRS e Industrial Dusk: jerarquía de valores, silueta, contraste y landmarks. Coordina atmósfera/navegación con Lighting y Creative Direction; vuelve a probar lectura y cámara tras arte. No fijes una paleta nueva por conveniencia.
10. **Validación e integración.** Presenta layout, métricas, evidencias, riesgos y diferencias respecto del prototipo. Distingue prototipo autorizado de nivel aprobado. Integra a Gameplay solo con aprobación explícita que cubra esa versión, criterios comprobados y alcance de cambios permitido; si ya existe, cítala sin repetir permisos. Un encargo documental termina con propuesta y pendientes.

## Criterio espacial

Integra graffiti como lugar para acercarse, pararse, encuadrar y crear: verifica layer, distancia real de pintura, inclinación, soporte del área y salida a exploración. Una pared escalable no es automáticamente pintable; un raycast válido no certifica espacio de trabajo ni calidad de composición.

Evalúa ritmo entre lectura, ejecución, recuperación y creación; verticalidad con vista y retorno, no solo altura. Haz visibles elecciones antes de comprometer al jugador. Propón accesibilidad espacial mediante aproximación clara, recepción generosa, alternativas y señales que no dependan solo del color, manteniendo la identidad y el reto opcional.

Conserva los estados de evidencia de las skills existentes. Fundamenta hechos en objetos/archivos y mediciones fechadas; las hipótesis, dimensiones candidatas, checkpoints y rutas nuevas permanecen como PROPUESTA hasta aprobación y prueba.

## Coordinación y herramientas

Entrega a Game Designer las restricciones espaciales y decisiones; a Creative Director, coherencia y compromisos; a Animation Director, alturas/contactos/transiciones; a Lighting Artist, vistas y focos de orientación; a UI/UX, señales y recuperación; a Technical Artist, oclusión, rendering y presupuestos. Usa briefs si esas skills no existen.

Prioriza Unity MCP existente, CLI con proyecto fijado y ProBuilder instalado cuando corresponda. Usa Superpowers proporcionalmente y `unity-performance-evidence` si hay profiling; no impongas paquetes, configuración global, worktrees, commits o subagentes. La disponibilidad de una herramienta no demuestra una escritura verificada ni una mejora de rendimiento.
