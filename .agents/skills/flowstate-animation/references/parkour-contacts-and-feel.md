# Parkour, contactos y game feel

## Diseñar la acción

Define pose de lectura → aproximación/compromiso → apoyo/impulso → vuelo/traspaso → recepción → recuperación. No todas requieren pausa o un estado nuevo. Asigna eventos a inicio real, adquisición/pérdida de contacto y recuperación; conserva los controles actuales salvo decisión de gameplay.

Timing y spacing deben expresar peso sin bloquear respuesta: anticipación corta o superpuesta cuando la acción debe responder inmediatamente; arco consistente con trayectoria física, contrapeso de torso, seguimiento de ropa/manos/lata y asentamiento que no borre la intención siguiente. Evalúa primero a velocidad normal; cámara lenta ayuda a explicar un fallo, sin certificar satisfacción.

## Contactos verificables

Registra superficie/normal/punto de apoyo, altura/espesor útil, aproximación, orientación y recepción desde métricas de Level Design. Una detección del collider no fija automáticamente posición correcta de palma/dedos o pies. Mide contacto visual y cuerpo físico por separado.

Para vault, sigue mano de apoyo respecto del borde durante su ventana; comprueba muñeca/codo/hombro, cadera, rodillas, lata y clearance de ropa/outline. En escalada, comprueba separación al muro, pivote de manos/pies, inversión, desplazamiento lateral, pérdida de superficie, top-out y wall jump. No añadas IK antes de identificar si la causa es pose, fase, escala, target, trayectoria o evaluación.

El código observado mueve el cuerpo con curvas temporales; TopOutMove separa subida vertical y avance, ParkourMove usa SmoothStep y arco senoidal, wall jump usa Jump y vuelve a escalada al final. Son contratos existentes, no prueba de contactos exactos. Side input puede reutilizar presentación de escalada; ausencia de clip lateral dedicado no demuestra por sí sola mala calidad.

Alineación de visual al suelo al inicio y sombra de contacto no son foot planting continuo. Sliding exige medir desplazamiento del pie durante una fase de apoyo frente a velocidad real del cuerpo. No lo diagnostiques solo por diferencia entre velocidades de catálogo.

## Transiciones y recuperación

Comprueba salida anticipada/tardía, pose de pie de apoyo, velocidad al terminar y entrada siguiente. Vaults/top-out/aterrizaje vuelven a Idle en el grafo actual; la hipótesis de una pausa visual necesita clip runtime. FPSController pone velocidad planar a cero al acabar ciertos movimientos: una condición Speed>0 propuesta para retorno directo puede no ser alcanzable en ese instante. Define el contrato de continuidad sin cambiar inercia o input inadvertidamente.

Para sincronización usa una ficha: acción/estado físico, clip/fase, transform que manda, eventos de contacto, cámara, sonido/VFX y criterio. No añadas eventos duplicados cuando código/Playable ya disparen el mismo efecto. Separa evento semántico —recepción confirmada— de disparar Land de forma predictiva; el código actual puede anticiparlo.

## Prototipo y aceptación

Acota una variante de obstáculo/movimiento y preserva el circuito. Propón vistas frontal/lateral y cámara jugable; ensaya alturas/ángulos representativos, ambos sentidos, fallo/cancelación, pausa y reentrada. Un ajuste no debe cambiar rango de parkour, atravesar colisión o imponer nueva penalización sin acuerdo.

Acuerda ventanas y tolerancias antes de probar: latencia entrada/acción, duración de contacto, error mano-borde/pie-suelo, penetración, tiempo de recuperación y control siguiente. Registra unidades, intentos y causa; no inventes tolerancias universales ni presentes el juicio del autor como playtest. Incluye comodidad de cámara y silueta bajo FSSRS/Industrial Dusk.
