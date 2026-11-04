# Revisión visual, accesibilidad y validación

## Criterios de calidad

| Frente | Qué comprobar |
|---|---|
| Jerarquía | Tarea principal, foco actual, acción siguiente y salida identificables; decoración subordinada |
| Lenguaje | Papel/tinta/graffiti y fuentes existentes; lectura de instrucciones distinta del gesto expresivo |
| Estados | Hover/foco/selección/confirmación/error diferenciados; no depender solo de color |
| Layout | Sin clipping/solapes/bloqueo de objetivos; texto y targets legibles a distancia real |
| Input | Ratón, teclado y mando cubiertos en el flujo probado; cancelación, repetición y cambio de dispositivo |
| Continuidad | Cerrar/reabrir, foco, cursor, pausa/audio y restauración sin fugas hacia gameplay |
| Accesibilidad | Contraste contextual, tamaño, señales redundantes, movimiento/destellos y alternativas de aprendizaje |
| Coste | Mediciones comparables cuando se afirma rendimiento; funcionalidad conservada |

Define criterios según pantalla, dispositivo y audiencia, sin inventar ratios/números universales. Un análisis de contraste digital no certifica toda accesibilidad; prueba lectura en fondos de juego claros/oscuros y por encima de VFX. Etiquetas largas/localización son casos de robustez, no autorización para instalar Localization.

## Resoluciones y capturas

Acuerda matriz de objetivos; como candidatos considera 1280×720, 1920×1080, 4:3 y ultrawide, además de UI scale/texto grande y safe areas si la plataforma las necesita. Son casos de prueba propuestos hasta confirmar soporte. No declares «responsive» por tener CanvasScaler.

Registra escena/revisión, modo edición/runtime, pantalla/estado, resolución/aspecto, escala, cámara/renderer si afecta, foco/dispositivo, valores de datos/preferencias y frame/tiempo de animación. Conserva condiciones entre A/B. Inspecciona capturas completas y detalles legibles; muestra las zonas de interacción importantes. Una imagen histórica o un viewport de Scene recortado no demuestra el layout actual en Game View.

## Movimiento reducido

Traza preferencia → consumidores → intensidad/timing → shaders/cámara. En FLOWSTATE `FS.Menu.Motion` selecciona una escala reducida .6 para consumidores concretos; no es modo inmóvil ni cobertura universal. SprayResourceHUD tiene animateHud/comicMotion propios. Revisa también HolographicTextEffect, logos, fondos y transiciones: el silencio de una animación no silencia todas las demás.

Proponer movimiento menor/cero exige definir semántica y alcance con el desarrollador. Al apagar transformaciones, conserva feedback de foco, datos/urgencia y tiempos funcionales. Comprueba pulsos, temblores, rotación, ecos y materiales: animateHud=false no demuestra reposo de shaders, porque pueden recibir offsets/movimiento de agitado y spray. Evita certificar comodidad o seguridad por una captura estática.

Comprueba también el cálculo dentro del shader: HolographicSprayCan usa `_Time.y * (0.38 + _Motion * 0.2)`, así que `_Motion=0` no congela barridos/foil. Un parámetro con nombre «Motion» no garantiza control total del movimiento; valida salida temporal, no solo valor del Inspector.

## Pruebas de interacción y aprendizaje

Separa tres evidencias: inspección de archivos/imagen; QA funcional (manual/automatizado); playtest con usuarios. Para probar comprensión, entrega una tarea sin enseñar su solución: entrar a pintar, identificar mezcla baja, recuperar, seleccionar obra, cancelar y retomar. Registra ayuda requerida, errores, tiempo, acciones accidentales y explicación del usuario; acuerda umbral antes de observar.

En pruebas autorizadas cubre foco inicial/perdido, cancelación en transiciones, tecla/clic sostenido, doble confirmación, pausa con panel abierto, cierre/reentrada y desconexión/cambio de dispositivo. No uses acciones inexistentes como criterio aprobado; la cobertura de mando de MenuInput no acredita pintura analógica.

La enseñanza contextual propone información ligada a estado real, recuperable y no intrusiva. Reutiliza ayuda existente sin convertirla en prueba de que ya se entiende. Los prompts deben corresponder a controles implementados/configurados; glifos y binding display necesitan un contrato que no cree soporte ficticio.

## Informe

Para cada propuesta entrega observación/fuente/contexto, hipótesis de UX, experiencia deseada, flujo/solución visual-funcional, recursos reutilizados, riesgos, aceptación y método visual/funcional/playtest. Marca lo no observado. No presentes prototipo, prueba automática o opinión del diseñador como aprobación artística o interfaz intuitiva.
