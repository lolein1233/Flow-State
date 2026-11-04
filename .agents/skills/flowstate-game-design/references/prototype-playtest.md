# Prototipo reversible y evidencia de experiencia

La preparación de un experimento no autoriza ejecutarlo. Respeta las restricciones del encargo; para una demostración teórica entrega hipótesis y protocolo sin tocar el juego.

## Acotar el prototipo

Define una incertidumbre principal, una situación de prueba, una referencia actual y una variante. Reutiliza controlador, pintura y presentación existentes; identifica explícitamente la coordinación que falta. Estima complejidad por dependencias y casos de estado, sin prometer fechas con precisión falsa.

Antes de editar, cuando esté autorizado, conserva estado Git y cambios ajenos; elige aislamiento proporcionado y compatible con las restricciones. Un prototipo reversible especifica archivos/objetos tocados, activación limitada, datos que podría escribir, cómo salir y cómo restaurar solo su propio trabajo. Un toggle no revierte reputación, guardados o destrucción de obras; evita esos efectos o prepara una reversión explícita. No restaures un archivo entero si contiene trabajo ajeno.

Mantén fuera del primer prototipo sistemas que no resuelvan la incertidumbre. Una prueba de ruta puede usar moderación y registro manual; no requiere todavía misiones, persistencia, scoring o assets nuevos. Declara qué confirma esa simulación y qué no permite concluir.

## Protocolo antes de observar

| Campo | Especificación útil |
| --- | --- |
| Hipótesis | Relación causal entre cambio y experiencia; qué observación la refutaría |
| Situación | Escena, inicio, ruta/muro, estado de pintura, recursos y controles |
| Participantes | Experiencia previa y necesidades; distinguir diseñador familiarizado de persona nueva |
| Condiciones | Dispositivo, tasa de frames/resolución relevantes, instrucciones y ayudas permitidas |
| Registro | Intentos, éxito/fallo, causa, tiempo de recuperación, decisiones, ayuda y comentarios |
| Criterios | Aceptar, refinar o descartar; umbrales propuestos antes de ver resultados |
| Protección | Pausa/cancelación, obras y datos que no deben perderse; restauración |

Para aprender una mecánica, observa primero sin explicar la solución; rescata si es necesario y registra la ayuda. Para comparar alternativas, conserva condiciones y contempla orden/aprendizaje. Una muestra exploratoria pequeña descubre problemas; no justifica porcentajes generalizables ni pruebas estadísticas de diversión.

## Game feel y responsividad

Sigue la cadena input → detección/precondición → cambio físico → animación/cámara → señal percibida. Separa falta de respuesta, acción contextual inesperada, contacto impreciso, señal tardía y dificultad de ejecución. El mismo input puede funcionar correctamente y sentirse ambiguo.

Si se mide latencia, define evento inicial/final, reloj, dispositivo y captura; distingue frames de milisegundos y simulación de input frente a periférico real. No deduzcas latencia o satisfacción de un valor de smoothing. Usa casos de borde: entrar/salir de modo, giro, freno, borde de superficie, pausa, agotamiento y reanudación. Comprueba accesibilidad con información por varios canales.

## Verificación funcional y refinamiento

En implementación autorizada, compila en Unity y lee consola sin limpiarla. Ejecuta primero regresiones EditMode pertinentes. Añade casos útiles de reglas y fallos reales; no tests que solo repitan constantes o espejen el código. Usa PlayMode para secuencias de input, cancelación, reentrada, UI y persistencia cuando afecten a la mecánica. Las pruebas de componentes nuevos no cubren automáticamente tuning/referencias de Gameplay.

Registra por intento: condición, decisión/acción, resultado observado, ayuda, causa probable y siguiente ajuste. Revisa contacto y encuadre desde la cámara del jugador, y escucha feedback cuando corresponda. Identifica observación, interpretación y confianza; conserva evidencia que contradiga la propuesta.

Al refinar cambia el supuesto responsable y vuelve a la comprobación pertinente. Usa `unity-performance-evidence` solo si la decisión depende de rendimiento; frecuencia de fallos o tiempo de recorrido son métricas de diseño, no capturas de CPU/GPU.

## Puerta de incorporación definitiva

Entrega la mecánica concreta con reglas y tuning candidatos finales, evidencias, fallos previos/regresiones, compensaciones, reversión y dudas abiertas. Solicita aprobación del desarrollador antes de incorporarla definitivamente; una prueba satisfactoria no la concede. Cita cualquier aprobación explícita vigente que ya cubra esa versión en vez de repetir la pregunta. Mantén la propuesta como pendiente si no hay aceptación; en una tarea solo teórica no pidas permiso para ejecutar una fase que el usuario no solicitó.
