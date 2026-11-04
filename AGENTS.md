# FLOWSTATE — colaboración de desarrollo

## Proyecto y límites
- Unity **6000.0.41f1**, C#, URP **17.0.4**, Input System **1.13.1**, Cinemachine **3.1.7**. Confirma las versiones en `Packages` antes de usar APIs.
- Conserva parkour, graffiti, reputación, cámaras, UI y rendering existentes. Implementa solo el alcance solicitado; no reconstruyas sistemas por defecto.
- Revisa `git status` antes de editar y preserva cambios ajenos. No cambies versión del Editor, paquetes, GUID, escenas, prefabs, assets ni ajustes sin que el trabajo lo requiera y esté autorizado.
- No copies autenticación entre cuentas ni envíes código, assets o capturas a servicios externos sin autorización. Las consultas de documentación deben ser genéricas y sin contenido del proyecto.

## Ciclo de trabajo
Para cambios de código o comportamiento aplica el ciclo completo. En tareas documentales o de configuración, verifica los archivos y herramientas afectados sin compilar o ejecutar el juego innecesariamente.

1. Comprende el sistema: busca con `rg`, identifica propietarios, dependencias, asmdefs y pruebas relevantes. Lee solo lo necesario.
2. Define el comportamiento esperado y criterios verificables. Para gameplay, considera respuesta al input, controles claros, feedback, legibilidad, game feel, mecánicas interesantes y sus compromisos.
3. Implementa el cambio mínimo coherente con el código existente. Mantén responsabilidades claras, referencias serializadas válidas y límites entre Runtime y Editor. Evita asignaciones por frame cuando importen, sin optimizar a ciegas.
4. Compila en Unity y lee la consola sin borrarla. No declares compilación correcta basándote solo en `dotnet` o en archivos `.csproj` generados.
5. Ejecuta pruebas existentes que cubran el cambio: EditMode primero; PlayMode y comprobación visual cuando el comportamiento lo necesite. Añade regresiones útiles para fallos reales; no pruebas que solo reflejen la implementación.
6. Revisa el diff y verifica en Unity el criterio de aceptación. Reporta evidencia, fallos previos, límites y pruebas no ejecutadas; no presentes una lista de tests como aprobada sin su resultado.

## Herramientas y roles
- Prioriza Unity MCP existente para operaciones del Editor. Comprueba instancias, proyecto y estado antes de actuar; si hay varias, fija FLOWSTATE. No añadas un segundo servidor Unity MCP para resolver una desconexión.
- Unity Official Plugin/CLI es complementario: usa terminal para tareas repetibles y descubre los comandos disponibles. No instales ni actualices `com.unity.pipeline` para una simple comprobación.
- Usa Context7 o documentación oficial para APIs de la versión real. Usa Computer Use para verificación visual local cuando corresponda; no guardes capturas en `Assets` durante auditorías.
- Separa las preguntas de diseño, implementación, revisión y QA cuando eso detecte riesgos distintos. Usa subagentes solo para tareas independientes útiles; evita ediciones simultáneas del Editor o del mismo archivo.
- Las skills de Superpowers se aplican de forma proporcional y bajo demanda. Estas instrucciones prevalecen sobre sus ceremonias genéricas: no fuerces brainstorming, TDD, worktrees, commits, PR, selección de modelo ni subagentes para cada tarea. No cambies configuración global por una recomendación genérica de una skill.
- Conserva herramientas de otros dominios, como Blender y navegador, sin activarlas como dependencias de toda tarea Unity. No cargues todos los manuales o skills al inicio.

## Rendimiento y entrega
- Para profiling usa `$unity-performance-evidence`: CPU, GPU, memoria y GC con capturas comparables, preferentemente Player en hardware objetivo. No afirmes mejoras sin mediciones antes/después.
- Entrega qué cambió, por qué, validación realizada y riesgos pendientes. Mantén informes y resultados fuera de `Assets`; no subas ni publiques resultados automáticamente.
