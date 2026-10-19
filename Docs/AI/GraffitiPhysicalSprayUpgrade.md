# FLOW STATE — Physical Graffiti Spray Upgrade

## Resultado jugable

El modo graffiti usa la lata y su boquilla animada como origen real del spray. La cámara permanece estable mientras el ratón desplaza y rota la mano; la rueda acerca o aleja la lata. El raycast, las partículas, el depósito de pintura, el cursor proyectado sobre la pared y la previsualización del menú comparten esa misma referencia.

La distancia controla un cono continuo de pintura:

- Con la rueda en posición cercana, la huella usa un multiplicador de radio de `0.55`: fina, concentrada y con núcleo firme.
- Con la rueda en posición lejana, usa `2.05`: la huella es aproximadamente `3.73×` más ancha aunque el raycast alcance la misma pared.
- Cada sello entre dos muestras interpola su apertura de cono. Un gesto que aleja la lata dentro de un único frame crece de fino a ancho sin saltos.
- El espaciado se calcula con la huella menor del tramo, evitando huecos al pasar rápidamente de cerca a lejos.

Con mezcla saludable, cada sello mantiene aproximadamente `0.95` de alfa antes del perfil procedural, tanto al iniciar como al mantener pulsado. La presión no cae por velocidad, distancia, boquilla ni cantidad restante: solamente una mezcla degradada reduce densidad/opacidad y provoca fallos de aerosol. El primer clic conserva una descarga breve para garantizar respuesta inmediata, pero ya no es el único momento con pigmento fuerte.

## Controles

- `Q`: abrir/cerrar el menú dibujado de boquillas.
- Ratón: mover la lata sin girar la cámara en modo graffiti.
- Rueda: acercar/alejar la lata y modificar físicamente el ancho del trazo.
- Clic izquierdo: pintar; un toque corto deposita pintura marcada inmediatamente.
- Cursor de pared: indica el punto exacto y el diámetro efectivo que recibirá pintura; sustituye al antiguo punto fijo de pantalla.
- `R`: agitar la lata.
- `F`: cambiar la lata cuando queda vacía.
- `Esc`: cerrar primero el menú; fuera del menú, salir del modo graffiti.

## Boquillas y presentación

Las boquillas conservan aspecto, tamaño, densidad, dureza, jitter y consumo propios. El menú recupera su escala de autor y reserva su área derecha únicamente para una muestra animada del aerosol seleccionado, sin rótulos ni panel flotante externo. La muestra usa el mismo shader procedural que se deposita sobre el muro y cambia con el color y la boquilla actuales.

## Continuidad y rendimiento

- Muestreo espacial independiente del framerate, validado a 30, 60 y 144 FPS.
- Presupuesto de interpolación acotado y remanente espacial conservado entre frames.
- Máximo de `4096` sellos por malla, `8` lienzos por superficie y `64` lienzos activos globales.
- Los sellos existentes no se modifican al vaciar o cambiar la lata.
- Shader común compartido entre pared y preview de menú, sin cámara de preview, RenderTexture ni readback.

## Validación automatizada relevante

- Clic corto fuerte y presión continua estable con mezcla saludable.
- Ensanchamiento medido de `3.73×` con la rueda, manteniendo constante la distancia real a la pared.
- Ensanchamiento monótono cerca→lejos dentro de un único frame.
- Depósito estable a 30/60/144 FPS en reposo y a distintas velocidades.
- Ray de la boquilla correcto con la rotación importada del modelo.
- Menú seleccionable con puntero y cámara inmóvil.
- Persistencia de pintura al agotar y reemplazar la lata.
- Cursor de pared alineado con el impacto y punto fijo de pantalla desactivado.
- Preview de menú con el mismo shader y canales de vértice requeridos.
