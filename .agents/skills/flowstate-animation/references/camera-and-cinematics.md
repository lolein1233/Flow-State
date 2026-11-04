# Cámaras y cinemáticas

## Propietario antes de mover cámara

Gameplay observado usa FPSController.UpdateCamera en LateUpdate y MainCamera; no se encontraron Brain/CinemachineCamera ni los componentes legacy CameraSwitcher/Transition/ThirdPersonCameraMouse en la escena cargada. Que existan scripts o Cinemachine 3.1.7 instalado no significa que sean el propietario activo.

Preserva órbita, colisión, damping, FOV y último world pose. El controlador evita arrastre horizontal extra y doble aplicación de rotación del padre; no añadas otro smoothing/Animator/Brain sobre la misma cámara sin contrato. Comprueba adquisición/retorno de control, pausa, teletransporte y cambio graffiti/tercera persona. CameraTransition puede delegar a FPSController en otros contextos: revalida componentes reales antes de diagnosticar conflicto.

Diseña encuadres por información necesaria: manos/contacto, borde y recepción, superficie a pintar y salida. Antes de añadir shake, roll o zoom plantea foco y silueta. Evalúa comodidad, movimiento reducido y apuntado con Game Design/UI/UX; una «toma cinematográfica» no debe secuestrar cámara durante una maniobra que exige leer destino.

## Timeline/Cinemachine existentes

Assets/10_Cinematics contiene escena, Timeline, cámara animada y presentación procedural de pintura. El Timeline del trailer dura 73 s, con AnimationTrack de cámara y AudioTrack; la escena serializa Brain y CM_Hero_Master. El movimiento del rig de cámara se liga a AnimationTrack, no a un CinemachineTrack identificado en ese asset. No regeneres desde el builder ni evalúes la escena de showcase durante una auditoría: ExecuteAlways/EvaluateAt pueden cambiar objetos y estilos.

Assets/09_MainMenu/IntroPrototype/ThreeTraces.playable usa IntroPaintTrack propio, duración 5.1 s. El director de intro tiene skip/lock/retorno propios; Timeline no debe sustituir el flujo del menú o gameplay. Respeta tracks, exposed references, bindings y ownership antes de editar.

Cuando corresponda usar Cinemachine Tracks, confirma API de Cinemachine 3 instalado y tipos de Timeline del paquete, sin copiar APIs de Cinemachine 2. No instales paquetes para una revisión. En cambios autorizados, una secuencia debe definir entrar, controlar input, clock, cámara/audio/actores, skip/interrupción y restaurar estado incluso al cancelar/cambiar escena.

## Revisión de imagen y temporalidad

Aplica el protocolo de capturas de Art & Lighting para condiciones reproducibles, añadiendo clip/fase, estado físico, input, velocidad del jugador, cámara y duración. Repite la misma maniobra A/B. Una vista de contacto lateral y la cámara de jugador responden preguntas distintas; no compares poses/cámaras diferentes como si fueran una mejora.

Un contact sheet histórico del trailer demuestra presentación de esa versión, no movimiento de parkour actual. Una captura de edición con UI o una pose estática no prueba fluidez. Para secuencias exportadas verifica también el archivo codificado, no solo preview del Editor, cuando renderizar esté autorizado. No cambies cámaras/configuraciones para obtener pruebas fuera del alcance.
