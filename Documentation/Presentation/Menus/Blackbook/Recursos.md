# Libreta de graffiti: pausa

La pausa usa una libreta abierta con recortes independientes en la escena Gameplay. Se puede editar fuera de Play en MENÚS - PAUSA Y CONTROLES / PauseMenu / NightShade / GraffitiPaper. Los recortes están en BlackbookCollage - Recortes editables: cada RawImage permite ajustar posición, escala, rotación y UV Rect sin alterar la imagen original.

Se apagaron la foto negra grande y los textos «un momento, vuelvo...» e «ideas / pruebas / manchas». La página ahora incluye piezas naranjas, amarillas, lilas y verdes repetidas y superpuestas. Los objetos retirados siguen inactivos para permitir recuperarlos. La ayuda conserva su diseño anterior.

## Recursos guardados

En Assets/04_Materiales e Imagenes/GraffitiBlackbook/:
- Blackbook_OpenPaper.png: papel y encuadernación con transparencia.
- Blackbook_StickersAtlas.png: tag FLOWSTATE, pegatinas holográficas y tiras de papel.
- Reference_HandstylePages.png: referencia adjunta por el usuario, recortada en la UI mediante UV Rect.
- Reference_NeonMarker.png: referencia conservada, desactivada en la composición final.

Las piezas de colores proceden del asset existente Assets/04_Materiales e Imagenes/imagenes/graffitis.png. Los recortes y repeticiones se realizan con RawImage; las texturas fuente permanecen intactas.

## Prompts de los recursos generados

Modo: herramienta integrada image_gen, con fondo transparente. Especificaciones de los dos prompts:

1. Libreta abierta realista vista desde arriba para el menú de pausa de un juego de graffiti. Dos páginas vacías de papel marfil con fibras, desgaste leve, lomo y pliegue central, cantos de hojas y cubierta oscura. Iluminación suave, sin mesa ni otros objetos, sin texto ni dibujos; exterior transparente. Reservar las páginas para recortes y botones editables.
2. Atlas de elementos independientes sobre transparencia: pegatina de pulgar holográfico y pequeña libreta con ojos inspiradas en las referencias del usuario, tag original rojo y negro con texto FLOWSTATE sobre papel rasgado y tira de papel marfil rasgado con cinta adhesiva para botones. Mantener separados los elementos, bordes físicos y textura de papel; estética de blackbook de graffiti.

## Verificación

Captura integrada en Play: pausa-final.png. Se verificó que la imagen negra y ambos textos están inactivos, que hay 14 recortes visibles y que el botón Menú de botones abre la ayuda. No aparecieron errores de Unity. Escena guardada con la pausa visible para edición.

Corrección de trim: UV Rect del papel ajustado a (0.55, 0.02, 0.435, 0.325) para excluir la punta del tag contiguo. Verificado visualmente en Play en los cuatro botones.
