# Especificar mecánicas y bucles

Usa los campos que cambien una decisión real; no rellenes casillas irrelevantes. Para contexto e identidad remite a Creative Direction y su biblia.

## Ficha de mecánica

- **Experiencia e hipótesis:** quién juega, cuándo aparece la necesidad, qué siente/intenta y qué resultado permitiría aceptar o descartar la idea.
- **Decisión del jugador:** alternativas disponibles, información previa, coste, consecuencia inmediata/diferida y recuperación. Evita llamar elección a una única acción obligatoria sin compromiso.
- **Regla:** precondición, input o evento autoritativo, estado que cambia, salida/feedback y excepción. Define qué significa completar, repetir, abandonar y volver.
- **Propietarios:** código/asset existente, lectura de estado necesaria, coordinación nueva, datos de autor y datos persistentes. Un símbolo público facilita acceso; no demuestra que exista un evento de finalización o guardado.
- **Tuning:** unidad, valor de referencia actual, candidato, rango de experimento y motivo. Declara si la fuente es default, override de escena/prefab o valor observado al ejecutar.
- **Aceptación:** invariantes funcionales, comportamiento esperado del jugador, evidencia, límites y aprobación necesaria.

Para cada transición relevante usa una tabla pequeña:

| Origen | Evento y condición | Destino | Efecto y feedback | Interrupción/recuperación |
| --- | --- | --- | --- | --- |
| Preparado | Acción válida realmente completada | Activo o siguiente estado | Cambio visible y dato propietario | Pausa, cancelación, fallo o reentrada |

Sustituye el ejemplo por estados específicos. Identifica transiciones imposibles, prioridades de inputs compartidos, reloj de juego o tiempo real, procesamiento al reanudar y estado de la obra. Si hay premio, define clave de elegibilidad, momento de entrega, repetición y guardado; no supongas idempotencia porque una animación se reproduce una vez.

## Bucles y profundidad

Describe el ciclo como **observar → decidir → actuar → recibir consecuencia → actualizar intención**. Indica qué devuelve al jugador al inicio y qué cambia entre repeticiones. Un desplazamiento más largo o más puntos no crean profundidad por sí solos.

El bucle principal debe representar la actividad recurrente que se pretende disfrutar. Los secundarios deben alimentarlo o darle una variación: práctica de una ruta, preparación de la lata, descubrimiento de un muro, elección de una composición. Señala si ya existe esa conexión o requiere un objetivo, persistencia o coordinación nuevos.

Para riesgo/recompensa separa coste temporal, dificultad de ejecución, incertidumbre y pérdida de progreso. En FLOWSTATE no inventes muerte, stamina, persecución, escasez de latas o un inventario funcional por su mera utilidad conceptual. Todo sistema nuevo se etiqueta como propuesta con su complejidad.

Comprueba estrategia dominante, premio por minuto, rendimiento decreciente y repetición de la misma ubicación. Compara también motivación intrínseca: ¿el jugador elegiría esta ruta o pintura sin el premio? No obligues a hacer parkour de precisión para obtener una herramienta básica de expresión sin explicitar ese compromiso.

## Aprendizaje, dificultad y emergencia

Introduce una regla legible, permite práctica segura, varía una exigencia y combina reglas cuando se comprendan. Define qué cambia: lectura, precisión, secuencia, orientación, recurso o presión temporal; no aumentes todas a la vez sin una razón de diseño. Añade opción de recuperación y señales antes del fallo.

Evalúa la dificultad respecto de dimensiones reales del personaje, cámara y obstáculos. En la revisión inicial, Gameplay usa valores distintos de defaults/docs: no derives la dificultad de un controlador recién instanciado o del nombre del obstáculo.

Busca composiciones entre sistemas: ruta que altera el punto de vista de la obra, tipo de cap que cambia preparación y ritmo, orden de paredes que modifica recorrido. Describe causalidad y forma de observarlo. Distingue emergencia expresiva de repetición de premios, acceso no previsto por colisiones o dobles acciones provocadas por input compartido.

Para balance y frustración pregunta qué puede anticipar el jugador, atribuir a su acción y corregir en el siguiente intento. Una mecánica puede necesitar más claridad o menor coste de reintento antes que un cambio de dificultad. Conserva las alternativas de práctica y autoría libre al proponer retos opcionales.
