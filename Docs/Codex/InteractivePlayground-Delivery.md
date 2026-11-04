# Codex Interactive Playground — entrega

Fecha: 28 de octubre de 2026. Estado: prototipo aislado IMPLEMENTADO; no integrado en Gameplay.

## Abrir y jugar

Abrir `Assets/Experiments/CodexInteractivePlayground/InteractivePlayground.unity` y entrar en Play Mode. No requiere añadir la escena a Build Settings.

- Movimiento: WASD; cámara: ratón; salto: Espacio; sprint: Shift. Son los controles del FPSController existente.
- Interacción contextual: E, obtenida de `GraffitiMenuDrawer.selectKey` al construir la escena. Acercarse al pulsador y mantenerlo en la vista. El indicador aumenta su intensidad cuando se puede usar.
- Abrir el acceso, seguir la guía ocre, empujar la caja hacia el borde de mantenimiento, saltar a la caja y después a la plataforma. Activar el botón elevado, bajar y atravesar la compuerta hasta la baliza final.
- La puerta de acceso tiene pulsadores en ambos lados. La compuerta de salida permanece abierta tras activar el botón; futuras aplicaciones pueden cerrar el mismo mecanismo mediante su evento/método público.
- Para reiniciar, salir y volver a entrar en Play Mode. No se añadió un menú ni un HUD.

## Skills seleccionadas

Se aplicaron `flowstate-game-design`, `flowstate-level-design`, `flowstate-art-lighting`, `flowstate-animation` y `flowstate-vfx-gamefeel`. Se consultó `flowstate-creative-direction` como referencia de identidad y límites. Para herramientas y QA: `unity-mcp-orchestrator`, `unity:unity-cli`, `computer-use:computer-use`, depuración sistemática y verificación antes de completar. Las instrucciones de AGENTS prevalecieron sobre las ceremonias genéricas; no se crearon worktrees, commits ni subagentes.

Versiones verificadas en el proyecto y en el Editor: Unity 6000.0.41f1, URP 17.0.4, Input System 1.13.1, Cinemachine 3.1.7 y Test Framework 1.4.6.

## Arquitectura e integración

`PlaygroundPlayerBridge` se añade solamente a la copia local del jugador. No cambia FPSController. Lee E, comprueba alcance desde el pecho (1,7 m), campo de visión, objeto visible, línea de visión y disponibilidad. Bloquea la interacción durante pausa, graffiti y parkour. Sus contactos `OnControllerColliderHit` transmiten empuje lateral al objeto físico.

`PlaygroundInteractable` define disponibilidad, posición y activación. `MechanismHandle` mantiene el pulsador fijo mientras la hoja se mueve. `SlidingMechanism` usa Rigidbody cinemático, interpolación, movimiento suavizado en FixedUpdate y estados Closed/Opening/Open/Closing. Rechaza nuevas órdenes durante la transición. Comprueba el volumen del siguiente paso contra personajes y cuerpos dinámicos; ante una obstrucción al cerrar, invierte hacia apertura. Las colisiones de la hoja permanecen activas.

`MechanicalButton` comprime su tapa en 80 ms, emite `activated` en el contacto mecánico y recupera su posición a los 380 ms. Rechaza repeticiones durante ese ciclo. Un UnityEvent persistente de la escena conecta el botón con `SlidingMechanism.RequestOpen`; no hay una dependencia del botón hacia una puerta concreta. Repetir la pulsación con la compuerta abierta es idempotente.

`PushableBody` aplica fuerza limitada en el paso físico y restringe velocidad horizontal. Caja: 65 kg, tamaño 2 × 1,6 × 2 m, rotaciones bloqueadas, damping 2,8, fricción estática 0,65/dinámica 0,55, rebote cero y ContinuousDynamic. El motor original conserva gravedad, colisiones, salto, movimiento, cámara y animación.

`DiegeticSignal` usa MaterialPropertyBlock, sin modificar materiales compartidos. `PlaygroundGoal` acepta la llegada del jugador cuando hubo una activación real y la compuerta está abierta; emite la finalización una sola vez. No añade reputación ni persistencia al juego.

La copia de MAIKOL conserva rig, Avatar, Animator Controller y animaciones originales. Se eliminaron solamente de esa copia las funciones de graffiti, combate y presentación ajenas al experimento, para evitar colisiones de input y referencias externas a otras escenas.

## Espacio y presentación

Cápsula observada: aproximadamente 3,02 m de altura y 1,51 m de diámetro mundial, con la escala original 1,51 del jugador. Acceso: 4,2 m de ancho; salida: 4,6 m; plataforma: 3,3 m de altura. El ensayo continuo verifica los dos saltos con el motor real y la altura de salto original de 2,02 m.

Patio industrial con ladrillo, hormigón, chapa, conductos, siluetas de fábrica, drenajes, guías de caja y cable visible entre interruptor y salida. Se reutilizan FSSRS, renderer 1 de la cámara y el perfil/cielo originales de Industrial Dusk. Los nuevos materiales están dentro del experimento; las referencias originales se leen sin editarlas. El shader nuevo WorldStencil sólo resuelve la oclusión de la rotulación física, conservando los shaders originales.

El feedback combina movimiento de tapa/hoja, luz ocre/verde y una barra de llegada. No se añadieron partículas, Canvas, menús ni pantallas de finalización a la escena.

## Validación

- Unity MCP y Unity CLI reconocieron el proyecto correcto y el Editor 6000.0.41f1. Se usó el servidor MCP existente.
- Compilación real en Unity y lectura de consola; no se usó dotnet como sustituto.
- 34 pruebas existentes de MovementFeelTests y ParkourDetectionTests: aprobadas.
- Cinco regresiones de mecanismos: repetición al abrir/cerrar, obstrucción, referencias ausentes, pulsador independiente y empuje limitado.
- Ensayo continuo real en Play Mode mediante entradas sintéticas a `FPSController.StepMovement`: acceso, cruce, cierre interior, empuje mediante contactos reales, salto a caja, salto a plataforma, botón/evento, repetición y llegada. Sin teletransportes entre inicio y final.
- Las comprobaciones posteriores de seguridad colocan explícitamente al jugador en posiciones de ensayo: cierre obstruido, bloqueo de la cápsula con compuerta cerrada, alcance desde suelo/distancia y pausa del botón. Son pruebas aisladas, separadas del recorrido continuo.
- Resultado NUnit final: **6/6 aprobadas, 0 fallos**, guardado en `Logs/CodexInteractivePlayground/playground-nunit.xml` (19:36:46 UTC / 16:36:46 America/Santiago). El caso de integración entra realmente a Play Mode, aunque su categoría de runner es EditMode. Sumadas a las 34 regresiones existentes: **40 pruebas aprobadas**.
- La escena y los prefabs se inspeccionaron para detectar scripts faltantes y referencias de eventos. La cámara usa el renderer FSSRS existente y MAIKOL.controller original.

Evidencia en `Logs/CodexInteractivePlayground/`: `runtime-acceptance.json`, `playground-nunit.xml`, resultados individuales `.txt`, `obstruction-live-trace.txt`, inventario de archivos y cinco capturas de la cámara del juego (`01-access.png` a `05-complete.png`). Los informes y capturas están fuera de Assets y no se publicaron.

Computer Use permitió observar Unity localmente. La prueba de pulsar E mediante esa herramienta no quedó acreditada: detectó actividad del usuario y rechazó el envío. El recorrido automatizado valida el camino de interacción y sus filtros, pero no sustituye un playtest completo con teclado y ratón de una persona.

## Problemas encontrados y corregidos

1. Rótulos visibles a través de los muros: shader local con prueba de profundidad.
2. Control asociado a una hoja móvil: pulsadores fijos e independientes en ambos lados del acceso.
3. Caja sin desplazamiento con fuerza inicial: diagnóstico físico confirmó fricción suficiente para retenerla; ajuste local de aceleración a 24 m/s², conservando el límite de velocidad y comprobando frenado.
4. Prefabs con referencias a destinos de la escena: plantillas con indicadores propios y eventos libres para conectar en cada uso.
5. Preparación del test EditMode de obstrucción: sincronización coherente del Transform y Rigidbody.
6. QA a través de EnterPlayMode: retirada de captura de variable del callback, esperas mediante tiempo del juego y reporte XML que se registra después de la recarga de dominio. Se conservan los resultados de intentos fallidos; no se atribuyen al prototipo pruebas que no completaron.
7. Unity regeneró tablas de fuentes al clonar el jugador; la restauración y comparación con los archivos iniciales forma parte del cierre de integridad.

## Archivos creados

Todos los recursos jugables nuevos están bajo `Assets/Experiments/CodexInteractivePlayground/`:

- `InteractivePlayground.unity`.
- Runtime: `PlaygroundInteractable.cs`, `MechanismHandle.cs`, `SlidingMechanism.cs`, `MechanicalButton.cs`, `PushableBody.cs`, `PlaygroundPlayerBridge.cs`, `DiegeticSignal.cs`, `PlaygroundGoal.cs` y `PlaygroundAcceptanceDriver.cs`.
- Editor: `PlaygroundSceneBuilder.cs`, `PlaygroundScenePolish.cs`, `PlaygroundMechanicsTests.cs` y `PlaygroundRuntimeTests.cs`.
- Prefabs: `MechanicalButton.prefab`, `SlidingGate.prefab`, `PushableCrate.prefab`.
- Materials: `Brick.mat`, `Concrete.mat`, `Steel.mat`, `SafetyOchre.mat`, `Ink.mat`, `Paper.mat`, `StateTeal.mat`, `CrateContact.physicMaterial`, `WorldStencil.mat` y `WorldStencil.shader`.
- Sus archivos `.meta` y metadatos de las nuevas carpetas, generados por Unity.

`PlaygroundAcceptanceDriver` está protegido por UNITY_EDITOR y no está en la escena guardada. Sólo se añade temporalmente para QA. Las pruebas se encuentran en el ensamblado Editor existente para referenciar FPSController sin reorganizar los ensamblados originales. El builder rechaza sobrescribir una escena ya existente; la escena entregada ya está construida.

## Límites y conservación

No se generó un ejecutable, no se perfilaron CPU/GPU/memoria y no se afirma una mejora de rendimiento. La observación técnica y las capturas no prueban diversión ni sustituyen evaluación con jugadores nuevos. No se añadió audio específico ni animación de empuje al rig.

El arte es un patio de prototipo con geometría modular y materiales del proyecto, no una producción final de entorno. No hay integración con graffiti, reputación o progresión. La recuperación por caída devuelve al inicio; la escena se reinicia mediante Play Mode.

Comparación final SHA-256 de **4.237 archivos previamente seguidos por Git: 0 diferencias respecto del inicio**, incluidos los dos scripts que ya tenían cambios del usuario. Las cuatro fuentes regeneradas se restauraron a sus bytes iniciales y se volvieron a comprobar. `final-integrity-differences.json` contiene `[]`. Tras actualizar la caché de estado de Git sin preparar contenido para commit, `git status` y `git diff --stat` conservan solamente los dos cambios de código preexistentes, además de los archivos nuevos y las carpetas/documentos sin seguimiento que ya existían.

No se cambiaron paquetes, Editor, Build Settings, ProjectSettings, Gameplay, prefabs compartidos, rig, Animator ni shaders/materiales originales. No se hicieron commits ni push. La escena quedó guardada sin scripts faltantes, sin Canvas propio y sin el driver de QA en su jerarquía guardada.
