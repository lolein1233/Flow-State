# FLOW STATE — Revisión conjunta Fase 3A + 3B

Fecha: 2026-09-21  
Escena: `Assets/02_Escenas/MainMenu_FlowState.unity`  
Estado guardado: `IntroMode.Disabled`  
Duración de `Full`: 5,10 s

## 1. Causa exacta del fallo de Reactive Camera

La causa final reproducida fue `FS.Menu.Motion = 0` en `PlayerPrefs`. `MenuSubmenu` mostraba ese
estado como **REDUCIDO**, pero escribía cero en `MenuCameraFeedback.motionScale`. El controlador
multiplicaba toda la cámara reactiva por ese valor: `AmbientInput` llegó a `(0,88, 0)`, pero
`Response`, posición y rotación del rig permanecieron exactamente en cero. Los tests anteriores
ocultaban el defecto porque forzaban `motionScale = 1` durante su preparación.

También se corrigieron dos problemas secundarios: el timer ya no elimina una posición absoluta
que permanece fuera del centro, y en Editor un mouse nativo usa la posición remapeada a Game View
para evitar coordenadas globales de escritorio en configuraciones multimonitor.

No se encontró un segundo escritor de pose: `MenuCameraFeedback` solo entrega `motionScale` e
impulsos. `FlowStateMenuCameraController` sigue siendo el único que escribe `ResponseRig`, cámara
y FOV.

## 2. Archivos modificados para la cámara

- `Runtime/MenuInput.cs`: posición absoluta, fuente activa, telemetría y arbitraje Mouse/Right Stick.
- `Runtime/MenuSubmenu.cs`: migra el antiguo cero a movimiento reducido real (`0,4`).
- `Runtime/MainMenuController.cs`: cursor `None` + visible al preparar el menú.
- `Runtime/FlowStateMenuCameraController.cs`: amplitud legible y canal de pose cinematográfica.
- `Assets/02_Escenas/MainMenu_FlowState.unity`: valores serializados y referencias.
- `Tests/PlayMode/MenuFoundationsPlayTests.cs`: persistencia fuera del centro, retorno, intensidad
  cero, Right Stick, navegación y push cinematográfico.

## 3. Valores de input antes y después

Antes, la preferencia persistida era `0`, así que la intensidad final era cero. Ahora ese valor se
migra a `0,6`; “REDUCIDO” conserva parallax y rotación. La respuesta usa una curva lineal y un
resorte más rápido para eliminar la sensación tenue del ajuste anterior.
El modo completo sigue usando escala `1`.

Valores artísticos guardados:

| Parámetro | Antes | Ahora |
|---|---:|---:|
| maxHorizontal | 0,065 | 0,23 |
| maxVertical | 0,035 | 0,13 |
| yaw | 0,65° | 2,8° |
| pitch | 0,35° | 1,5° |
| roll | 0,06° | 0,25° |
| springStrength | 65 | 145 |
| damping | 16 | 18 |

`CameraMotionIntensity` y `ParallaxIntensity` permanecen en 1; el deadzone permanece en 0,04.

## 4. Resultado manual con mouse real

Unity confirmó en Play Mode que el mouse nativo está habilitado, la aplicación tiene foco y el
cursor queda libre y visible. La reproducción capturó la cadena que fallaba: input no nulo con
`motionScale=0`; después del hotfix, la misma entrada desplaza y rota el rig incluso en REDUCIDO.
La escena queda guardada en Disabled para la comprobación física final
centro/derecha/izquierda/arriba/abajo/centro.

La ruta completa sí quedó instrumentada y aprobada con un `Mouse` del Input System: posición
bruta → pixelRect → normalización −1..1 → deadzone/curva → resorte → `ResponseRig`.

## 5. Resultado con Right Stick

El stick derecho mueve la cámara, no navega, supera `Response.x > 0,3`, desplaza el rig más de
0,015 unidades y retorna a menos de 0,001. Un gamepad en reposo no reemplaza la fuente Mouse.
Left Stick, D-Pad, WASD y flechas mantienen navegación.

## 6. Cursor

Al entrar al menú: `Cursor.lockState = None` y `Cursor.visible = true`. Se conservan los valores
previos y se restauran al deshabilitar el controlador. Esto evita heredar el lock de Gameplay.

## 7. Rediseño de los fragmentos

La composición final usa seis siluetas convexas diseñadas, separadas y verticales, con alturas,
anchuras y ángulos distintos. Se organizan como tres pares que comparten los tres canales de
máscara y una sola textura de diorama. El aerosol recorre cada panel de abajo hacia arriba; el
ruido solo erosiona borde y overspray. El negro ocupa la mayor parte del plano.

## 8. Formas actuales

- Par A / Creative Flow: dos módulos inclinados y estrechos, uno alto y otro corto.
- Par B / Anger: dos trapecios centrales con diagonales, alturas opuestas y mayor erosión.
- Par C / Clarity–Neutral: dos stencils controlados a la derecha, parcialmente desaturados.

## 9. Cambios de shaders

`IntroSprayMask.shader` recibe hasta ocho puntos por silueta y calcula pertenencia al polígono,
distancia al borde, erosión y overspray localizado. Cada definición ejecuta dos revelados
escalonados, por lo que aparecen seis paneles sin añadir otra cámara ni otra RenderTexture.
`IntroWorldPigment.shader` conserva crops y debug, pero el Timeline no ejecuta ninguna deposición.

## 10. Regiones del logo

Ninguna. Los cuatro clips actuales son tres revelados dobles y el Camera Push. El canal alfa de
deposición permanece en cero durante toda la intro; el texto/logo “FLOW STATE” no aparece.

## 11. Pieza → graffiti

La revisión solicitada elimina las extensiones libres. La secuencia conserva únicamente el spray
que revela seis ventanas de diorama y una pausa de lectura antes del movimiento final.

## 12. Graffiti → logo

No existe transición a logo en esta revisión. El asset oficial continúa referenciado para una
fase futura, pero no se muestra ni se deposita.

## 13. Mini Camera Push

El cuarto clip, `CameraPush`, ocupa 4,20–4,85 s. Timeline entrega solo progreso; el director pide
al controlador central una pose interpolada hasta offset `(0,035, 0,012, 0,18)`, ángulo
`(-0,32°, 1,10°, 0,10°)` y FOV `−1,15°`. Timeline nunca escribe directamente Main Camera.
Reactive Camera y Camera Push usan canales separados y se limpian en el handoff.

## 14. Duración

5,10 s: negro → seis paneles escalonados → lectura conjunta del diorama → push breve → hold.

## 15. Resultado en Play Mode

Capturas A–E verifican el recorrido completo. Full congela el último frame y libera la cámara de
contenido y sus RenderTextures. Skip y Disabled ejecutan la misma finalización, pintan el menú una
sola vez y activan la cámara reactiva. La escena valida con 0 scripts faltantes, 0 referencias
faltantes y 0 prefabs rotos.

## 16. Tests

- Play Mode específico de intro: 4/4, 9,11 s. Incluye crecimiento por canales, negro inicial,
  espacio negativo, determinismo, contenido animado, RGB oficial, push y liberación.
- Play Mode completo: 19/19, 69,15 s. Job `064da74f95d9471c84934a91dd4a8f66`.
- Regresión específica de preferencia cero + mouse + Right Stick: 3/3, 10,99 s.
- Edit Mode completo: 7/7, 1,37 s. Job `a399073234654104b7fdf6217963f25d`.
- Cobertura final a 1024×576: A 39.720 px, B 46.716 px, C 41.197 px.
- Depósito alfa: 0 px durante toda la secuencia.

## 17. Errores y warnings

Compilación: 0 errores. Consola final: ningún warning del código del menú o shaders. Los únicos
mensajes restantes pertenecen al Unity Test Runner (`IPrebuildSetup`, guardado de resultados e
`IPostBuildCleanup`). Las pruebas no cambiaron Gameplay ni los assets TMP; `EditorSettings.asset`
fue restaurado byte a byte desde el respaldo de esta revisión.

## 18. Capturas A–E

- `Captures/Phase3ABReview/A_080_FirstStructuredReveal.png`
- `Captures/Phase3ABReview/B_265_ThreeDesignedModules.png`
- `Captures/Phase3ABReview/C_405_StructureBreak.png`
- `Captures/Phase3ABReview/D_595_LogoDeposition.png`
- `Captures/Phase3ABReview/E_674_FinalCameraPush.png`

Revisión posterior sin logo:

- `Captures/Phase3ABReview/IntroPanels_01_Early.png`
- `Captures/Phase3ABReview/IntroPanels_02_SixComplete.png`
- `Captures/Phase3ABReview/IntroPanels_03_FinalPush.png`

## Estado y límites

Se mantienen exactamente una Content Camera, una Content RenderTexture, dos máscaras ping-pong,
tres crops y siete modos debug. No se añadieron fragmentos, segunda cámara, World Reveal, diorama
final, partículas, audio final ni transición final automática al menú.
