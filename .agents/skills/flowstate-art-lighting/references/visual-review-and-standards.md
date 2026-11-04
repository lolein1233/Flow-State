# Revisión visual y estándares

## Captura reproducible

Selecciona vistas desde el jugador: aproximación, despegue/recepción, pared pintable, encuadre de pintura y transición de cámara. Añade una vista ambiental solo si responde a una pregunta distinta. No confundas vista bonita con recorrido legible.

Registra escena/revisión/cambios, modo edición o juego, cámara/posición/rotación/FOV, resolución/aspecto, pipeline/calidad/renderer, HDR/AA, máscara y volúmenes efectivos, emoción/paleta, luz, exposición y frame/acción. En A/B conserva estas condiciones y cambia la intervención declarada. Repite el recorrido para efectos temporales: un PNG no revela flicker, popping, shimmering o adaptación.

En el estudio de Gameplay, MainCamera usa FSSRS renderer 1; Scene View usa el renderer por defecto de URP. Scene View no demuestra el composite final. Una captura directa de cámara puede excluir Screen Space Overlay UI y no reproduce la inicialización del juego. Declara estas diferencias y verifica la cámara efectiva runtime cuando esté autorizado.

Guarda originales fuera de Assets, con fecha y ficha de condiciones. No alteres la cámara del juego para conseguir una prueba sin autorización; usa una captura que restaure recursos temporales o una vista de prueba autorizada. Inspecciona la imagen, no solo el éxito de la herramienta. No uses servicios externos con material del proyecto sin permiso.

## Lectura de imagen

Lee masas y valores: foco, fondo, ruta, recepción y lugar de creación. Después temperatura/saturación, dirección de luz, sombras/contactos y frecuencia de textura/contorno. Distingue color base, respuesta de material, luz, palette mapping, composite y postprocesado antes de corregir.

Compara el mismo encuadre a tamaño de juego y reducido. Inspección en gris y simulación de deficiencia cromática orientan, pero no sustituyen pruebas con personas. Señales críticas necesitan forma, valor, posición o movimiento además de color. No impongas ratios de contraste de texto como regla universal para geometría 3D.

## Criterios a acordar por tarea

| Área | Evidencia/criterio observable | Motivo de revisión |
|---|---|---|
| Consistencia | Papel, tinta, capas y autoría se reconocen en vistas elegidas | El acabado borra la identidad |
| Jerarquía | Acción/recepción y foco se identifican antes del compromiso | Decoración, branding o bloom compiten con información necesaria |
| Silueta | Personaje/lata y bordes útiles sobreviven sobre fondos claros/oscuros | Outline fusiona masas o elimina detalle necesario |
| Saturación | Graffiti conserva énfasis; acentos ambientales tienen función | El aumento global uniformiza el énfasis |
| Sombras | Contacto/orientación claros; comprobar bias, acne, peter-panning y cascadas | Ocultan apoyo o saltan visiblemente |
| Continuidad | Capturas homologables entre zonas/escenas/emociones | Cambios de exposición/material sin razón identificada |
| Accesibilidad | Rutas/estados usan señales redundantes; revisar movimiento/destellos | Lectura depende solo de tono o ruido |
| Compatibilidad | Renderer, decals, pintura, UI y entradas depth/normals correctos | Arreglo de una capa rompe otra/plataforma |
| Coste | Presupuesto acordado y capturas CPU/GPU/memoria/GC comparables | Solo se cuentan luces o FPS aislados |

En navegación, pide identificar destino/recepción antes de actuar; registra tiempo de decisión, errores y causas. Acuerda umbrales según tarea/hardware. No declares accesibilidad o claridad aprobadas solo por juicio del autor.

## Crítica verificable

Conecta **observación + imagen/condiciones → causa candidata → propuesta → comprobación**. Expón qué hallazgo refutaría la hipótesis. Separa corrección técnica, preferencia artística y efecto jugable. Una captura permite observaciones locales, sin conclusiones sobre toda la experiencia final.
