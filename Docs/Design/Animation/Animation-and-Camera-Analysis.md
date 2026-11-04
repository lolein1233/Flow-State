# FLOWSTATE — MAIKOL, animación y cámaras

Fecha: 2026-10-28. Alcance: archivos, lecturas explícitas de Unity MCP en memoria, observación de Editor en edición y captura histórica. Sin Play Mode, muestreo de clips, evaluación de Timeline ni cambios de juego. Unity 6000.0.41f1, URP 17.0.4, Cinemachine 3.1.7 y Timeline 1.8.7 confirmados en ProjectVersion/Packages.

El [snapshot de Editor](Editor-Animation-Snapshot-2026-10-28.json) conserva la evidencia de componentes, parámetros, estados/transiciones, clips, importers, valores de FPSController y Timeline. Es una lectura fechada de edición; no una grabación de movimiento ni un estado jugable inicializado.

## Autoridad y límites

Se conserva la [dirección creativa](../../../.agents/skills/flowstate-creative-direction/SKILL.md), la función jugable de movimiento/autoría y FSSRS/Industrial Dusk. Game Design decide reglas/recuperación; Level Design métricas/apoyos; Art & Lighting silueta/imagen; UI/UX interacción/accesibilidad. Animación coordina presentación temporal, rig, contactos y cámaras, sin reemplazar esos propietarios.

IMPLEMENTADO describe código/recurso; LECTURA EN EDICIÓN, valores vivos sin ejecución; OBSERVACIÓN HISTÓRICA, imagen guardada; HIPÓTESIS, explicación pendiente de observar; PROPUESTA, intervención no aprobada. No se usa APROBADO para un asset existente o el nombre de un test.

## Rig y recursos comprobados

| Elemento | Evidencia | Lo que no acredita |
|---|---|---|
| Jugador | Player/MAIKOL_Visual, Animator habilitado, [MAIKOL.controller](../../../Assets/06_Modelos/MAIKOL/MAIKOL.controller), Avatar null, applyRootMotion=false, Normal/AlwaysAnimate | Humanoid, retargeting o calidad de movimiento |
| Skin | PANTALON PRUEBA, 52 bones, root mixamorig:Hips; cuatro placas FSSRS adicionales usan el mismo número/root | El número de bones no mide coste de skinning ni contactos |
| FBX | 18 modelos bajo Assets/06_Modelos/MAIKOL: ModelImporter Generic, NoAvatar, importAnimation=true, optimizeGameObjects=false | Nombres Mixamo/manolo/X Bot no hacen los rigs Humanoid |
| Clips usados | 14 clips, legacy=false, humanMotion=false, 52 rutas transform por clip sin rutas faltantes bajo MAIKOL_Visual; 340 o 355 bindings | Matching de rutas no prueba poses, ejes, ausencia de clipping o retargeting entre jerarquías distintas |
| Otros actores | Dos manolo@Happy Idle con enemigos.controller, Avatar null y un skin de 52 bones cada uno | No se confunden con el controlador del jugador |

La configuración Generic/NoAvatar no se declara defectuosa: permite animar por rutas de transform compatibles. No existe fundamento para convertirla a Humanoid o activar Root Motion durante esta tarea. Se encontraron FBX y .anim de MAIKOL; no se encontró .blend/.avatar dedicado bajo su carpeta. Hay un .blend ambiental en otros recursos, que no se presenta como fuente editable del personaje.

### Animator y clips activos como recursos

Una capa Base Layer, sin mask ni IK pass, default Idle, 13 estados. Walk contiene un Blend Tree Simple1D de Speed con Walking a 1.8 y Running a 5.5; usa LocomotionRate. Todos los estados inspeccionados tienen Foot IK desactivado.

| Estado | Recurso | Duración clip / velocidad de estado | Loop / salida relevante |
|---|---|---|---|
| Idle | Happy Idle.anim | 2 s / .9 | Loop; salida por Speed, Climbing o Jump |
| Walk | Walking.anim + Running.anim | 1.033 s y .633 s; rate variable | Loop; blend por Speed |
| Jump | MAIKOL_Jump_Quick.anim | 1.06 s / 1.08 | Grounded y exitTime .72 para Idle; Climbing permite Hang |
| Hang | Hanging Idle.anim | 2.333 s / 1 | Loop; ClimbInput y Jump |
| ClimbUp / Down | Climbing Up/Down Wall.anim | 2 s / 1 | Loop; entradas/salidas por ClimbInput/Climbing |
| LedgeTopOut | MAIKOL_Ledge_TopOut.anim | 3.833 s / 2.35 | A Idle, exitTime .95, blend .08 s |
| VaultLow | ParkourAnimations/MAIKOL_Vault_Low.anim | 1.167 s / 2.33 | A Idle, exitTime .9, blend .07 s |
| VaultMedium | MAIKOL_Vault_Medium.anim | 1.4 s / 2.26 | A Idle, exitTime .9, blend .07 s |
| VaultHigh | MAIKOL_Vault_High.anim | 2.367 s / 2.82 | A Idle, exitTime .9, blend .07 s |
| Falling | MAIKOL_Falling_Loop.anim | .7 s / .72 | Loop; Grounded/Climbing |
| FallLanding | MAIKOL_Fall_Landing.anim | 1.067 s / 1.2 | A Idle, exitTime .92, blend .1 s; posible Hang |
| AirRoll | MAIKOL_Air_Roll.anim | 1.167 s / 1.25 | ExitTime .94 a Falling/Idle según Grounded; posible Hang |

Todos los clips inspeccionados tienen frameRate 30 y cero Animation Events. Eso no significa ausencia de feedback: código/Playables/VFX pueden dispararlo fuera del clip. Los clips no loop son vaults, salto, top-out, aterrizaje y rodada; subir/bajar/colgar/caminar/correr/idle/caída sí tienen loop. MAIKOL_Idle_Neutral existe en disco, pero Idle del controlador referencia Happy Idle; no se reemplaza por su nombre.

Any State contiene entradas a tres vaults, TopOut, Falling, Land y Roll. Varias transiciones locales usan interruptionSource None. La configuración existe; sus prioridades reales e interrupciones siguen pendientes de observar en ejecución.

## Motor y sincronización

[FPSController.cs](../../../Assets/01_Scripts/FPSController.cs) es el propietario principal del cuerpo y la cámara. SetFloat/Bool/Trigger presenta Speed, Grounded, Parkour, Climbing, ClimbInput y acciones; el código comprueba parámetros existentes. Un trigger no demuestra que la transición ya se haya consumido.

Lectura viva frente a defaults: caminar/correr **3/6** en Gameplay, referencia de animación **1.1/3.5**, Blend Tree **1.8/5.5**. GetLocomotionPlaybackRate limita la relación a **1.15**; MovementFeelTests incluye pruebas para conservar esa cadencia. La diferencia es una pista de calibración, no evidencia de foot sliding. No aumentar clips ni cambiar velocidad de cápsula indiscriminadamente.

Vault bajo/medio/alto duran **.5/.62/.84 s** en el motor; top-out **1.55 s**, wall jump **.58 s**, rodada **.8 s**. Dividir duración de clip por velocidad de estado aproxima .501/.619/.839 s para los vaults: hay correspondencia intencional aparente. Top-out llega a exitTime .95 alrededor de 1.55 s, cercano al final físico. Esto evita diagnosticar un desfase solo comparando duración bruta del clip; faltan blending e interrupciones runtime.

- ParkourMove deshabilita temporalmente CharacterController/cápsula, mueve transform mediante SmoothStep/arco senoidal, restaura colisión y pone velocidad planar a cero al terminar. TopOutMove separa ventanas de elevación (.36–.82) y avance (.68–.95), y asienta el controlador al final.
- Wall jump usa Jump, trayectoria/rotación por código y EnterClimb al llegar; no hay estado WallJump dedicado en el controlador inspeccionado. Esto no prueba que necesite uno.
- Escalada continua usa ClimbInput amortiguado. El input lateral puede alimentar su magnitud animada; no se observó un clip lateral dedicado en el grafo. ClimbableSurface aporta canClimb/allowSideways/speedMultiplier, no targets de manos.
- AlignVisualFeetToGround ajusta altura visual durante la alineación inicial; no equivale a IK de pies continuo. Contact shadow es presentación, no corrección de pose.
- Land puede dispararse antes del contacto si una estimación de impacto entra en landingAnimationLeadTime=.34 s. No usar ese trigger como prueba de que el pie ya tocó suelo ni duplicar audio de impacto sin revisar eventos.
- [GraffitiAnimation.cs](../../../Assets/01_Scripts/GraffitiAnimation.cs) aporta sway, bob, agitado/cambio y control procedural de lata. La presentación de primera persona no implica un rig de manos completo identificado.
- [RenderPoseController.cs](../../../Assets/01_Scripts/RenderPoseController.cs) mezcla pose sentada con AnimatorController mediante Playables y reserva F para graffiti; es otro escritor que debe respetarse al añadir capas/procedural.

No se encontraron OnAnimatorIK/MatchTarget o RigBuilder de gameplay en los propietarios buscados. No se afirma ausencia de cualquier solver: la cinemática tiene su propio FlowStateCinematicPainter, que manipula huesos por nombre y resuelve brazo/piernas proceduralmente. No se conecta al personaje jugable por defecto porque también restaura poses.

## Cámara y cinemáticas

**Gameplay vivo en edición:** una Camera habilitada en Player/CameraHolder/MainCamera; ningún Brain/CinemachineCamera, PlayableDirector o componente de Animation Rigging en el inventario cargado. Los archivos CameraSwitcher, CameraTransition y ThirdPersonCameraMouse existen, pero no aparecieron como componentes en esa escena. cameraOverride=null es opcional: ResolveReferences tiene fallback, por lo que no se declara un error del juego.

FPSController.UpdateCamera usa LateUpdate, órbita, seguimiento de objetivo, look-ahead, SphereCast de colisión y FOV amortiguado. Valores vivos: modo blend 13.5, radio .28, padding .18, target smooth .075, posición .055, entrada/salida de colisión .025/.18; FOV tercera/sprint/graffiti **62/65/60**. El código conserva world pose para evitar rotación duplicada del padre y lleva desplazamiento horizontal inmediatamente. No añadir otro filtro para una hipótesis de jitter sin comprobarlo.

En graffiti cambia a primera persona y puede ocultar visualRoot. La vuelta, pausa, pose sentada, input y colisión son parte de la validación de cualquier presentación nueva; una cámara bonita en un único plano no prueba comodidad ni orientación.

**Cinemáticas existentes, sin abrir escenas:**

- [FlowState_Trailer_25s_98s.playable](../../../Assets/10_Cinematics/Timelines/FlowState_Trailer_25s_98s.playable): duración 73 s, un AnimationTrack de cámara y un AudioTrack (clipIn 25 s). No se encontró CinemachineTrack en ese asset. El builder liga la cámara animada al Animator de un rig que contiene CM_Hero_Master.
- La escena [FlowState_Trailer_Cinematic.unity](../../../Assets/10_Cinematics/Scenes/FlowState_Trailer_Cinematic.unity) serializa CM_Hero_Master, PlayableDirector y dos componentes CinemachineBrain. El inventario estático no establece cuál gobierna el render ni su activación efectiva; no se declara conflicto runtime.
- [ThreeTraces.playable](../../../Assets/09_MainMenu/IntroPrototype/ThreeTraces.playable): 5.1 s, IntroPaintTrack propio con tres trazos y un push de cámara. Su director contiene bloqueo/skip y retorno al menú propios.
- [RENDER_NOTES.md](../../../Assets/10_Cinematics/RENDER_NOTES.md) y [contact sheet v6](../../../Captures/FlowState_v6_FSSRS_contact.png) documentan una versión histórica. Se observan poses de pintura, lata, contorno impreso y distintos encuadres; no son parkour actual ni prueba temporal. El look Creative Flow máximo del trailer no reemplaza Industrial Dusk del gameplay.

Computer Use observó Game View en edición cubierto por la ayuda guardada; no se vio una maniobra de MAIKOL. Se restauró la ventana minimizada sin cambiar escenas ni cámaras. No se evaluó Timeline/ExecuteAlways del showcase ni se ejecutaron builders de presentación.

## Tres mejoras propuestas — sin implementación ni aprobación

### 1. Salida de vault/aterrizaje coherente con continuidad de movimiento

**Evidencia:** vaults y FallLanding terminan en Idle; Walk requiere otra transición por Speed. El motor pone velocidad planar a cero al terminar ciertas maniobras. **Causa candidata:** esa ruta puede introducir una pose intermedia o recuperación visual innecesaria al querer seguir avanzando; no se observó un tirón real y los timings actuales de vault ya están próximos al motor.

**Solución propuesta:** comparar el retorno actual con una salida a locomoción cuando exista una condición válida de continuidad. Primero registrar intención, velocidad física, Grounded y fase del grafo para definir esa condición; no basta añadir Speed>0 si en ese instante vale cero. Mantener cadencia, trayectoria y recuperación jugable; no restaurar inercia o suprimir anticipación sin Game Design.

**Riesgos:** condición inalcanzable, salto visual de pie de apoyo, interrupción que corta contacto, pose prematura de correr, cambios involuntarios de control. **Herramientas:** Inspector/Animator y lectura MCP de parámetros, variante de controller/clip en prueba autorizada; CLI para pruebas repetibles. **Validación:** mismo vault con input mantenido, soltado y dirección cambiada; registrar fin físico/estado/fase, primera respuesta y pose siguiente. Vídeo normal/cuadro a cuadro, cámara jugable y lateral; comparar tiempo de recuperación y continuidad sin nuevas colisiones. Criterios cuantitativos por acordar antes del prototipo; ningún resultado se da por mejor sin observarlo.

### 2. Apoyo de mano acotado para vault bajo o top-out

**Evidencia:** trayectoria del cuerpo por código, clips Generic y ningún target de palma/IK de gameplay identificado; hay geometría y normal de superficie en detección. **Causa candidata:** una pose/contacto de clip fijo puede no coincidir con borde/altura/espesor real. No se ha medido penetración o deslizamiento de manos.

**Solución propuesta:** elegir un obstáculo representativo y trazar mano/borde durante la fase de apoyo. Ajustar primero fase/pose/offset de una variante de clip y el contrato de contacto, conservando root/cápsula. Si la evidencia exige corrección procedural, diseñar un solver compatible con Generic, con peso/ventana y pole vector; no activar Humanoid IK ni instalar Animation Rigging. El solver cinematográfico es referencia técnica, no módulo listo para acoplar.

**Riesgos:** doble escritura sobre huesos, torsión de muñeca/codo, alcance imposible, clip que mueve visualRoot, más clipping con lata/ropa/outline o cambio de rango físico. **Herramientas:** consultas de bindings y contactos, Animation Editor en variante autorizada, Blender solo con bridge/fuente verificadas; actualmente Blender no está conectado. **Validación:** vistas lateral/frontal y de jugador, misma aproximación, alturas/ángulos de muestra; medir distancia mano-borde y deriva durante apoyo, cuerpo/colisión y retorno. Incluir fallo/cancelación/pausa, sin cambiar métricas de nivel para ocultar el problema.

### 3. Encuadre de recepción durante top-out, dentro de la cámara existente

**Evidencia:** seguimiento genérico del objetivo con damping vertical y trayectoria por fases de top-out; no hay perfil específico de encuadre por fase identificado. **Causa candidata:** en algunos bordes el ascenso podría ocultar o desplazar la recepción; la captura actual no demuestra que ocurra.

**Solución propuesta:** prototipo local que ajuste el objetivo de encuadre según fase, reutilizando la cámara FPSController y su resolución de colisión. Priorizar que borde/suelo de recepción y dirección de salida permanezcan comprensibles. Comparar primero el comportamiento actual; no añadir Brain/Timeline, roll/shake o control automático del input al gameplay. Una toma de presentación separada puede planearse en la cinemática existente sin imponerla al jugador.

**Riesgos:** pérdida de orientación, tirón de retorno, colisión por suavizado, interferencia con apuntado/lock-on y preferencia de movimiento reducido. **Herramientas:** MCP/Computer Use para lectura/capturas y prueba autorizada del controlador actual; Cinemachine/Timeline reservados para su contexto real. **Validación:** mismos bordes y cámara/input, A/B del recorrido completo, registrar visibilidad de recepción, clipping, cambios de distancia/FOV y comodidad reportada; incluir vuelta a graffiti, pausa y control durante recuperación. Si la cámara actual ya cumple, descartar la intervención.

## Capacidades y límites de herramientas

| Capacidad | Estado en esta tarea |
|---|---|
| Unity MCP | Instancia FLOWSTATE verificada, Gameplay en edición, lecturas en memoria correctas tras corregir dos consultas; sin cambios de scripts |
| Animator/clips/importers/Timeline | Lecturas reales del Editor; no muestreo, edición ni reproducción |
| Unity CLI oficial | Complemento existente; uso de status documentado en validación, sin instalación |
| Computer Use | Ventana Unity observada en edición; ninguna maniobra probada |
| Cinemachine/Timeline | Instalados y recursos inspeccionados; no sustituyen cámara de gameplay |
| Animation Rigging | No declarado en manifest/lock; ningún componente encontrado en Gameplay cargada; no instalado |
| Blender | Health check connected=false; herramientas presentes, edición/exportación no verificadas |
| Pruebas/perfilado | Fuentes existentes identificadas; no ejecutadas ni presentadas como aprobadas |

Se inspeccionó el dispatcher local de manage_animation: ofrece consultas de Animator/controller/clip y operaciones reales de estados, transiciones, layers, Blend Trees, curvas y eventos. No se invocaron sus escrituras. La capacidad expuesta y su implementación inspeccionada no equivalen a integración o exportación verificadas.

La skill aporta procedimiento para poses, timing, rigs, blends/capas/aditivos, contactos, root authority, cámaras, DCC y evaluación. No añade paquetes, fuente editable, aprobación artística ni resultados perceptuales. Quedan pendientes observación temporal actual, pruebas de control/contacto/cámara, retargeting real y costes en hardware objetivo.
