# FLOW STATE — Fase 3A: fundaciones técnicas

Implementado y validado en Unity 6000.0.41f1 / URP 17.0.4, escena `MainMenu_FlowState`.
Esta entrega termina en 3A. No contiene intro visual, fragmentos, construcción del logo,
Timeline cinematográfico, cámaras de contenido, diorama ni cambios de shaders/paquetes.

## 1. Archivos creados

- `Assets/09_MainMenu/Runtime/FlowStateIntroDirector.cs` y `.meta`.
- `Assets/09_MainMenu/Runtime/FlowStateMenuCameraController.cs` y `.meta`.
- `Assets/09_MainMenu/Tests/PlayMode/MenuFoundationsPlayTests.cs` y `.meta`.
- Este informe.
- Evidencia local en `Captures/Phase3A`: `disabled-menu.png`, `PlayModeResults.xml`, `EditModeResults.xml`.

## 2. Archivos modificados

- `Assets/09_MainMenu/Runtime/MainMenuController.cs`: legend opcional, Prepare, LockMenu, EnterMenu idempotente y MenuEntered.
- `Assets/09_MainMenu/Runtime/MenuInput.cs`: acciones privadas para ambiente y Skip; navegación separada.
- `Assets/09_MainMenu/Runtime/MenuCameraFeedback.cs`: proveedor de impulsos, sin escritura de transforms/FOV.
- `Assets/09_MainMenu/Runtime/CanMotion.cs`: únicamente dos eventos alrededor de la animación existente.
- `Assets/09_MainMenu/Tests/PlayMode/MainMenuPlayTests.cs`: únicamente el destino esperado Game → Gameplay.
- `Assets/02_Escenas/MainMenu_FlowState.unity`: rig, anchor, director y referencias; reconexión de Archive instructions a MenuSubmenu.body.

Los cambios previos de Gameplay y Leander SDF se preservan. Los cambios automáticos de
OWNED y EditorSettings causados por las pruebas se restauraron. No se modificó el asset
compartido de Input Actions ni se eliminaron escenas.

Respaldo previo de los archivos afectados:
`C:/Users/alons/.codex/backups/FLOWSTATE-phase3a-20260921/`.
No aplicar un rollback completo si después se realizan nuevas ediciones: comparar antes.

## 3. Jerarquía

```text
FLOW STATE · Living Graffiti Menu
├── FlowStateIntroDirector (componente)
├── MenuHeroAnchor                 posición (0, 0, -10), rotación original
└── MenuCameraRig                  FlowStateMenuCameraController
    └── ResponseRig
        └── Menu Camera            cámara existente y MenuCameraFeedback
```

La cámara mantiene el FOV base de 38° y su compensación para proporciones estrechas.
El controlador nuevo es el único escritor de pose del rig, offset de respuesta y FOV.

## 4. IntroDirector

Modo guardado: **Disabled**.
Awake bloquea el menú; Start prepara y completa usando `CompleteIntroSafely()`.
Full se detiene en Painting y Short en LogoReveal: son estados de infraestructura,
no secuencias visuales. `AdvanceTo` acepta hitos posteriores sin abrir directamente el menú.

Prueba manual de infraestructura: elegir Full o Short antes de Play y usar Escape,
Enter, Space o gamepad South. Alternativamente, usar el menú contextual del componente
`DEBUG / Complete Intro` o activar `debugCompleteOnStart` antes de Play.

Disabled, Skip y Complete comparten limpieza de spray, colocación en hero, EnterMenu,
activación reactiva y un único evento MenuReady. No hay esperas arbitrarias ni carga de escena.

## 5. EnterMenu

Prepare valida las cuatro opciones, guarda/restablece estado de cursor y prepara presentación.
EnterMenu activa navegación, aplica el primer estado visual y pinta una vez. Las llamadas
repetidas no duplican pintura, audio ni MenuEntered. Start conserva autoentrada para escenas
antiguas sin un director activo. LockMenu bloquea métodos de interacción y vacía la cola.

## 6. Input

- Izquierdo/D-Pad/WASD/flechas: navegación; se usa el eje dominante.
- Derecho: ambiente; sus bindings de Navigate quedan desactivados solo en la copia privada.
- Mouse: posición normalizada respecto a pixelRect; retorno tras 0,8 s sin movimiento.
- Rueda, click, Submit y Cancel existentes se mantienen.
- Skip se procesa mientras el menú está bloqueado. Se consume la pulsación y se espera
  liberación antes de aceptar Submit/navegación, evitando que saltar active JUGAR.
- La pérdida de foco neutraliza el input ambiental.

## 7. Feedback

MenuCameraFeedback mantiene Impulse y motionScale para compatibilidad con MainMenuController
y MenuSubmenu. El controlador central muestrea la amortiguación y aplica los offsets originales
de kick (traslación horizontal y roll). Ya no hay LateUpdate escribiendo la cámara desde feedback.

## 8. Eventos de CanMotion

- `RotationStarted(int direction, float duration)`.
- `RotationCompleted()`.

No se cambiaron curva, giro de 90°, anticipación, rebote, idle ni spray de la lata.
El controlador responde con acompañamiento pequeño y una cola amortiguada al terminar.
Puede desactivarse con canFollowEnabled.

## 9. Inspector de cámara

| Parámetro | Valor inicial |
|---|---:|
| maxHorizontal / maxVertical | 0,065 / 0,035 unidades |
| yaw / pitch / roll | 0,65° / 0,35° / 0,06° |
| springStrength / damping | 65 / 16 |
| returnSpeed | 2,5 |
| deadzone | 0,04 |
| CameraMotionIntensity / ParallaxIntensity | 1 / 1 |
| IdleMotionIntensity | 0 |
| canFollowEnabled / canFollowStrength | true / 0,2 |
| baseFieldOfView / referenceAspect | 38° / 16:9 |

responseCurve también es editable. El resorte usa pasos acotados, respuesta limitada y tiempo
no escalado. La preferencia existente FS.Menu.Motion multiplica toda la respuesta de cámara.
Intensidad cero reinicia offsets y velocidades. ParallaxIntensity controla la traslación;
la rotación conserva su propio control mediante CameraMotionIntensity.

## 10. Play Mode

Entrada explícita en Play Mode mediante MCP y captura de la cámara real:
MenuReady, IsReady=true, IsLocked=false, StrokeCount=1, ReactiveEnabled=true y pose hero.
legend permanece sin asignar y Paint no lanza excepción. Captura: `Captures/Phase3A/disabled-menu.png`.
No se añadió geometría temporal: el test de perspectiva usa puntos a profundidades 3 y 12
para comprobar que la traslación provoca desplazamientos diferentes.

## 11. Tests

**14/14 PlayMode aprobados**, 0 fallidos, ejecución final 57,53 s.
**3/3 EditMode aprobados**, 0 fallidos.

Cobertura: entrada única; Full/Short/Disabled/debug; Skip de teclado y South sin Submit;
navegación por teclado, stick izquierdo y D-Pad; derecho sin navegación; mouse; retorno;
intensidad cero; eventos/follow-through; galería/configuración; recarga; carga de Gameplay;
cola de 1.000 entradas y 300 pinturas con número fijo de RenderTextures/objetos;
parallax geométrico y preservación del JSON compartido de Input Actions.

Las pruebas nuevas suspenden temporalmente los dispositivos físicos de Unity y utilizan
dispositivos sintéticos para evitar interferencia del mouse del editor. Restauran dispositivos
y settings en teardown; no modifican bindings globales ni settings persistidos.
No se probó un mando físico ni se generó un build standalone.

## 12. Errores y avisos

Compilación y ejecución finales sin errores. Validación MCP: 0 scripts faltantes y 0 prefabs rotos.
La transición existente emite un aviso URP:
`MainCamera: 1 camera overlay no longer exists and will be removed from the camera stack.`
El test de transición pasa: Gameplay carga y el overlay se destruye. MenuTransition no fue
modificado en esta entrega. Los logs del runner sobre resultados/cleanup no son errores del menú.

## 13. Ajustes respecto a fase 2

- Se reconectó MenuSubmenu.body al texto Archive instructions ya existente: las pruebas
  descubrieron esa referencia nula adicional. No se reconstruyó UI.
- Los parámetros están directamente en el componente de cámara en 3A; perfiles por estado
  y recursos cinematográficos quedan para una fase posterior.
- El parallax se valida con profundidad real sin añadir un diorama.
- Full/Short son rutas de depuración manuales; solo Disabled tiene entrada automática normal.

Detener aquí. La fase 3B requiere confirmación del usuario.
