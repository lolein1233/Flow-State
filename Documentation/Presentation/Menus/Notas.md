# Pausa y ayuda rápida

- **Esc** abre la pausa durante el juego normal. En graffiti o escalada conserva primero la función contextual de salir/cerrar/soltarse.
- **B** abre la ayuda desde el juego o desde la pausa. **B / Esc** cierran la ayuda y reanudan el juego.
- La ayuda también se abre con el botón «Menú de botones» de la pausa. Tiene dos páginas: movimiento/graffiti y música/presentación. Se cambian con sus botones o las flechas izquierda/derecha.

Se duplicó el menú de pausa original antes de mejorarlo, conservando su papel crema, franjas naranjas y tipografía para la ayuda. Ambos están guardados en Gameplay bajo **MENÚS - PAUSA Y CONTROLES**. Los hijos PauseMenu y ControlsMenu son editables fuera de Play; el Inspector del padre permite ver la pausa, cualquiera de las páginas de ayuda u ocultarlas. El juego utiliza estos mismos objetos y conserva los cambios de contenido y diseño.

La pausa añade el efecto HolographicTextEffect en PAUSA, ecos de tinta, entrada con rebote, respiración, respuesta al cursor y salida breve. Respeta la preferencia FS.Menu.Motion del menú principal y usa tiempo no escalado para animar durante la pausa.

El cartel «EL FLOW SIGUE» se quitó. El fondo holográfico usa un 22 % de opacidad sobre una sombra de 38 %, para conservar visible el escenario. Su material está en Resources/UI/PauseHolographicFoil.mat, con Opacity editable en el Inspector. Cada sesión anima una copia del material, para no modificar el asset.

Ambos menús suspenden las interacciones del juego y ocultan temporalmente el HUD. Al cerrar restauran el tiempo, el audio, el cursor, el HUD y los controles. Se verificaron estos estados y los botones en Play Mode, con un solo EventSystem, además de inspeccionar capturas de ambas páginas de ayuda y la pausa.

Las 34 pruebas existentes de interacción con graffiti pasaron. No se simuló el teclado físico; se revisaron las ramas B/Esc y se ejecutaron sus mismos métodos y los callbacks de los botones. Gameplay se guardó con los menús editables; no se modificaron paquetes ni ajustes del proyecto.
