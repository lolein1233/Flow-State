# FLOW STATE — Physical Graffiti Spray Upgrade

## Resultado jugable

El modo graffiti usa la lata y su boquilla animada como origen real del spray. El ratón desplaza primero la mano dentro de una zona de precisión; cuando el gesto supera esa zona cómoda, el movimiento excedente se transfiere progresivamente al encuadre. No existe un borde horizontal artificial y la vista vertical conserva únicamente el límite de seguridad de `-85°..85°`. El raycast, las partículas, el depósito de pintura, el cursor proyectado sobre la pared y la previsualización del menú comparten esa misma referencia.

La distancia controla un cono continuo de pintura:

- Un gesto lento se interpreta como precisión y acerca la lata: huella fina, concentrada y con núcleo firme.
- Un barrido rápido se interpreta como cobertura y aleja la lata: la huella puede abrirse hasta el multiplicador de radio `2.05` sin usar la rueda.
- Un cambio brusco de dirección o una curva cerrada reduce temporalmente el cono para conservar esquinas y contornos limpios, incluso si el gesto venía rápido.
- La velocidad y el giro se normalizan por tiempo y después se filtran con respuestas distintas para apertura y precisión, evitando parpadeos de tamaño.
- La lata visible avanza o retrocede junto al cono calculado: animación, cursor y pintura comunican siempre el mismo ancho.
- Cada sello entre dos muestras interpola su apertura de cono sin saltos.
- El espaciado se calcula con la huella menor del tramo, evitando huecos al pasar rápidamente de cerca a lejos.
- La boquilla estándar baja a una huella aproximada de `3.2 cm` en precisión lenta y todavía abre más de `6×` para cobertura.
- El diámetro nominal permanece estable durante el trazo: la textura vive en el grano y el borde, no en pulsaciones del quad completo.
- Los sellos móviles se alargan sobre el segmento que deben cubrir y se centran hacia atrás, formando una columna continua en lugar de una cadena de círculos.

Con mezcla saludable, cada sello mantiene aproximadamente `0.95` de alfa antes del perfil procedural, tanto al iniciar como al mantener pulsado. La presión no cae por velocidad, distancia, boquilla ni cantidad restante: solamente una mezcla degradada reduce densidad/opacidad y provoca fallos de aerosol. El primer clic conserva una descarga breve para garantizar respuesta inmediata, pero ya no es el único momento con pigmento fuerte.

## Controles

- `Q`: abrir/cerrar el menú dibujado de boquillas.
- Ratón: mover la lata; al llegar al borde cómodo, continuar el mismo gesto desplaza el encuadre para alcanzar el resto de la pared.
- Velocidad del gesto: lento produce detalle fino; rápido produce cobertura amplia; las curvas cerradas vuelven automáticamente a precisión.
- La rueda ya no es necesaria para controlar el ancho.
- Clic izquierdo: pintar; un toque corto deposita pintura marcada inmediatamente.
- Cursor de pared: indica el punto exacto y el diámetro efectivo que recibirá pintura; sustituye al antiguo punto fijo de pantalla.
- `R`: agitar la lata.
- `F`: cambiar la lata cuando queda vacía.
- `Esc`: cerrar primero el menú; fuera del menú, salir del modo graffiti.

## Boquillas y presentación

Las boquillas conservan aspecto, tamaño, densidad, dureza, jitter y consumo propios. El menú recupera su escala de autor y reserva su área derecha únicamente para una muestra animada del aerosol seleccionado, sin rótulos ni panel flotante externo. La muestra ahora representa un pequeño trazo compuesto con once depósitos —incluidas dos gotas de detalle—, usa el mismo shader procedural de la pared y cambia con el color y la boquilla actuales. El eco cromático es estrecho y de baja opacidad, evitando el halo cian sobredimensionado anterior.

### Aerosol visible desde la boquilla

`GraffitiSprayVFX` reutiliza el sistema de partículas de la escena y lo convierte en una nube impresa de cuatro lecturas: micro‑pigmento denso, nube de aire amplia, microgotas y un pulso gráfico de válvula. Todas usan billboards compactos —nunca partículas estiradas—, terminan antes de atravesar la superficie detectada y reaccionan en tiempo real a color, cap, apertura adaptativa y estado de la mezcla. `Needle` forma un chorro fino, `Fat Cap` abre una nube densa, `Chisel` produce un abanico horizontal y `Splatter` aumenta la dispersión de gotas. El primer frame ya emite pigmento y pulso; al soltar conserva solamente una cola muy breve.

El origen ya no se calibra contra el borde superior de la lata. `Spray Test update.fbx` contiene la boquilla dentro de una única malla, así que se midió el centro real de la cara de salida (`0, -0.0034142, 0.0016845` en espacio local) y su dirección local `Vector3.down`. `TransformPoint`/`TransformDirection` mantienen el emisor unido a ese orificio bajo escala, sway, recoil, vibración y cambio de lata. El sistema legado también normaliza su escala `0.22218`, evitando que el aerosol quede cuatro veces más pequeño de lo diseñado.

El shader `FLOWSTATE/Graffiti/Cartoon Aerosol Particle` construye cada mota proceduralmente, sin texturas nuevas. Reproduce el lenguaje FSSRS dentro del propio transparente: silueta de tinta rota con animación escalonada, registro cromático cyan/magenta, trama sutil, paleta emocional y pulso gráfico. El pigmento negro permanece negro; el papel aparece únicamente como microdestello y nunca como un aro blanco dominante. La composición usa alpha premultiplicado para evitar halos grises. El presupuesto queda limitado a `402` partículas y los cuatro materiales se crean una sola vez.

## Continuidad y rendimiento

- Muestreo espacial independiente del framerate, validado a 30, 60 y 144 FPS.
- Presupuesto de interpolación acotado y remanente espacial conservado entre frames.
- Máximo de `4096` sellos por malla, `8` lienzos por superficie y `64` lienzos activos globales.
- Los sellos existentes no se modifican al vaciar o cambiar la lata.
- Shader común compartido entre pared y preview de menú, sin cámara de preview, RenderTexture ni readback.
- Ruptura orgánica del borde con ruido continuo de dos octavas y gotas con suavizado dependiente del píxel, evitando anillos facetados al acercar la cámara.

## Validación automatizada relevante

- Clic corto fuerte y presión continua estable con mezcla saludable.
- Transferencia progresiva mano→cámara en el borde y retorno inmediato hacia el centro.
- Giro horizontal validado más allá del antiguo límite (`120°` en la prueba de ejecución).
- Cono adaptativo validado desde `0.03` en precisión hasta `1.00` en cobertura, con reducción automática en curvas.
- Ensanchamiento superior a `3×` entre precisión y cobertura manteniendo constante la distancia real a la pared.
- Ensanchamiento monótono cerca→lejos dentro de un único frame.
- Depósito estable a 30/60/144 FPS en reposo y a distintas velocidades.
- Ray de la boquilla coincidente con el centro geométrico del orificio bajo rotación y escala jerárquica.
- Menú seleccionable con puntero y cámara inmóvil.
- Persistencia de pintura al agotar y reemplazar la lata.
- Cursor de pared alineado con el impacto y punto fijo de pantalla desactivado.
- Preview de menú con el mismo shader y canales de vértice requeridos.
- Huella lenta fina, diámetro estable, muestras elongadas sobre el recorrido y preview compuesto validados con pruebas dedicadas.
- Aerosol validado con cuatro capas billboard, escala legado neutralizada, presupuesto acotado, aspecto Chisel real y jerarquía visual Needle → Soft Round → Fat Cap.
