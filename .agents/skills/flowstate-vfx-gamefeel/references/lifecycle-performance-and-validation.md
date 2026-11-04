# Lifecycle, herramientas, rendimiento y validación

## Reutilización y recursos

Inventaría ownership, creación, warm-up, reuso, cola, stop/clear y destrucción. GraffitiSprayVFX reutiliza cuatro sistemas y materiales HideAndDontSave; no es lo mismo que pooling de muchos impactos. No añadas pool si no hay churn relevante. Para one-shots autorizados, define límite, reset de tiempo/seed/transform/emisión/renderer/callbacks y retorno al terminar, incluidas cancelaciones/escena.

Respeta lifetime de pintura depositada: devolver un VFX al pool no debe borrar una obra. No compartas una instancia ambiental de Dust para disparar aterrizajes ni edites su material común desde una prueba. Distingue material compartido y runtime; lee sharedMaterial sin instanciar durante auditorías.

Al pausar comprueba reloj del productor, ParticleSystem.main.useUnscaledTime, shader _Time, UI y audio. EmitStartBurst/valve pulse y módulos tienen relojes distintos; que el motor se detenga no prueba que todo efecto se congele. Al ocultar, cambiar modo, destruir owner o recargar, verifica colas, referencias y materiales vivos.

## Herramientas

Prioriza MCP existente: instancias/proyecto/estado y custom tools antes de acciones. manage_vfx puede consultar/controlar/configurar sistemas según su versión; inspecciona esquema/implementación para distinguir lectura de play/emit/simulate/write. execute_code puede leer propiedades explícitas en memoria, sin crear scripts, pero no es una garantía de solo lectura: revisa el código. No invoques Bind/Play/Emit/Simulate/Evaluate para una auditoría documental.

CLI oficial complementa comandos repetibles con ayuda y project-path; no actualices CLI/Pipeline para una comprobación. Computer Use verifica imagen/input locales cuando corresponda y esté autorizado. Consulta skills oficiales pertinentes, código de paquetes instalados o documentación/Context7 con consultas genéricas para APIs de versión real. No actives dependencias globales, generación remota o paquetes por defecto.

## Medición

Aplica `unity-performance-evidence`. Acuerda plataforma/hardware, build, ruta/input, calidad/resolución, efectos simultáneos y criterio antes de medir. Preferir Player; Editor sirve para localizar candidatos, sin certificar coste final.

Revisa CPU de emisión/perfiles/noise, GC y actualizaciones; GPU por pass, fillrate/overdraw y transparencias; memoria de partículas, meshes, materiales/texturas/RT y crecimiento tras ciclos. Frame Debugger explica composición/llamadas, sin probar tiempo GPU. Contadores no disponibles se declaran desconocidos, nunca cero.

maxParticles=402 es capacidad configurada de código, no ocupación simultánea, presupuesto aprobado ni prueba de coste bajo. Menos partículas/draw calls o migración a Graph no garantizan mejora. Compara capturas repetidas y condiciones equivalentes; conserva legibilidad/calidad y documenta compromisos.

## Evidencia y aceptación

Registra revisión/escena, estado, boquilla/color/recursos, distancia/ángulo/superficie, input/seed si existe control, cámara/renderer/FSSRS, duración/resolución y modo runtime/edición. Repite A/B a velocidad normal y cuadro a cuadro. Una captura histórica con una lata no acredita el aerosol actual; una imagen sin nube tampoco prueba fallo de emisión.

Matriz útil: pulsación nueva/flujo/dropout/seco/agitar/cambio, rápido/libre, pared cercana/lejana/oblicua, fondos claros/oscuros, pausa/salida/reentrada, obra parcial/completa y reputación al tope. Define casos según alcance. Prueba solapamiento de efectos y lectura de recepción/graffiti/HUD, junto a accesibilidad.

Para cambios de comportamiento compila en Unity, lee consola sin borrar y ejecuta suites pertinentes de GraffitiInteractionTests, QuickGraffitiTests, MovementFeelTests/ParkourDetectionTests. Añade regresiones de fallos reales/eventos si corresponde, sin duplicar implementación. Tests de capacity/configuración no prueban calidad visual o FPS; playtests evalúan comprensión/satisfacción con tareas y registros de errores/ayuda.

Entrega observación/fuente → objetivo perceptual → propuesta → reutilización → riesgos → implementación futura → aceptación y evidencia. Explicita pruebas no ejecutadas y decisión pendiente. Prototipo, integración y aprobación artística son estados distintos.
