# Revisión de diseño e iteración

Usa solo los apartados pertinentes. La evaluación puede terminar en una recomendación documentada; las fases ejecutables dependen del encargo vigente.

## Evaluar una propuesta

Formula el problema en términos del jugador y sitúalo en un momento concreto: entrada al juego, recorrido, fallo, selección, pintura o recompensa. Explica qué cambia, por qué merece existir y qué parte del sistema actual se reutiliza.

Contrasta el alcance con la biblia y la evidencia primaria. Separa estas preguntas:

- ¿La acción ofrece control expresivo, una elección interesante o información útil?
- ¿Puede entenderse con los controles, cámara y feedback existentes? ¿Qué ocurre al fallar, cancelar o pausar?
- ¿El incentivo favorece explorar/crear o repetir la acción más barata? ¿Qué se conserva tras recargar?
- ¿La firma gráfica ayuda a leer al personaje, ruta y obra? ¿La atmósfera deja espacio para la saturación del graffiti?
- ¿Pose, contacto, sonido y efecto comunican el mismo evento? ¿Se puede reducir movimiento y mantener información por varios canales?
- ¿Qué coste y complejidad introduce? Trata como hipótesis el coste no medido; una escena más cargada no demuestra por sí sola una regresión.

Ofrece alternativas cuando exista un compromiso material. Describe la diferencia de experiencia, los sistemas afectados y la prueba que permitiría elegir. Evita una lista de ideas sin recomendación y sin criterio de descarte.

## Analizar referencias visuales

Inspecciona la referencia proporcionada o el archivo local antes de emitir juicio. Identifica procedencia, fecha y contexto: concept, captura histórica, escena en edición, frame de Play Mode o build. Describe elementos observables: jerarquía de valores, silueta, escala, material, composición, densidad y color.

Extrae principios transferibles al lenguaje propio: por ejemplo, separar figura/fondo mediante valores puede ser útil aunque la paleta de la referencia no corresponda a FLOWSTATE. Distingue observación de interpretación y no declares adoptada una estética externa. No generes assets ni subas referencias a servicios por el mero hecho de analizarlas.

Para comprobar el resultado en Unity, usa herramientas de lectura acordes con el alcance. Confirma instancia y escena; conserva cambios sin guardar. No cargues otra escena, actives objetos, muevas cámaras de escena, ejecutes builders o entres en Play Mode si el encargo limita la revisión a solo lectura. Si la vista está obstruida, registra esa limitación y prepara el siguiente punto de verificación. Las capturas de auditoría van fuera de Assets.

## Prototipo, revisión, playtest y refinamiento

Antes de un prototipo autorizado define una hipótesis, una porción mínima del sistema, las variables que cambian, lo que se conserva y la condición de descarte. Reutiliza escenas/objetos existentes cuando sea coherente; no impongas una reconstrucción ni un paquete nuevo.

La revisión visual verifica el resultado desde la cámara del jugador: contacto, oclusión, lectura a distancia y cercanía, contraste y continuidad. El playtest verifica comprensión y conducta: qué intenta el jugador, qué aprende, dónde falla y qué interpreta como recompensa. Define quién prueba y en qué condiciones; distingue un recorrido del agente de un playtest con una persona nueva.

Propón criterios antes de observar resultados. Ejemplos de criterios futuros, no objetivos aprobados:

- Alcanzar una pared y completar una marca sin recurrir a F7/F8 ni explicaciones externas.
- Identificar boquilla seleccionada y estado de la lata con sonido desactivado y efectos reducidos.
- Cambiar de recorrido a pintura, cancelar y pausar sin inputs contradictorios ni pérdida de obra.
- Asociar cada premio a una acción y comprobar que recargar o repetir la misma finalización no lo duplica.
- Mantener personaje y muro objetivo distinguibles en el tratamiento normal y en el emocional.

Registra resultado observado, contexto y aceptación/incumplimiento por criterio. Refina el supuesto responsable; no sumes efectos para ocultar una falta de claridad. Compilar y pasar regresiones comprueba corrección, no calidad creativa completa. Sigue AGENTS.md si se implementa código; usa profiling comparable si la decisión depende de coste.

## Conflictos y decisiones

Para un conflicto estética/lectura/rendimiento/gameplay indica: beneficio buscado, quién lo percibe, coste o riesgo, evidencia disponible, alternativa y prueba discriminante. Propón la mínima concesión que mantenga la intención: limitar intensidad/duración, reservar un efecto a un evento o redistribuir contraste, según lo que demuestre la revisión.

Registra una decisión aprobada solo con ID, fecha, formulación, alcance, procedencia verificable del desarrollador y condiciones de revisión. Mantén los registros de propuestas y aprobaciones separados. Cuando el usuario ya haya autorizado una decisión, no vuelvas a pedirla; una decisión nueva y material debe llegar a revisión como opción concreta.

## Encargos a otras especialidades

| Especialidad | Entrega útil |
| --- | --- |
| Game Design | Regla, estados, elección, incentivo, casos de fallo y aceptación |
| Level Design | Ruta y ubicación real, métricas por confirmar, dificultad y recuperación |
| UI/UX | Información por estado, dispositivo, jerarquía, accesibilidad y cancelación |
| Art Direction / Lighting | Referencias propias, valores y materiales que preservar, composición y límites del efecto |
| Animación / Cámara | Evento, contacto, timing, plano de jugador y transición problemática |
| Audio / VFX | Evento y estado propietario, sincronización, mezcla/intensidad y señales alternativas |

No inventes nombres de skills instaladas. Si una especialidad no tiene skill, entrega el brief directamente y explicita qué validación especializada queda pendiente. Usa subagentes solo con autorización y tareas independientes; evita que dos agentes editen el mismo archivo o el Editor.
