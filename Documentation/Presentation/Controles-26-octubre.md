# Presentación del 26 de octubre de 2026

Escena: `Assets/02_Escenas/Gameplay.unity`.

- Al iniciar el juego, el player aparece en **LUGAR 1**.
- **F7**: volver a LUGAR 1.
- **F8**: ir a LUGAR 2.
- No hace falta mantener Ctrl. Se reemplazaron Ctrl+1/Ctrl+2 porque el editor los intercepta para cambiar de pestaña.
- En Unity, dar foco a la pestaña Game para recibir el teclado. Los atajos se ignoran durante la pausa.

El componente `PresentationLocationCommands` del Player tiene asignados ambos emptys. Se pueden mover y rotar para cambiar los destinos; la altura del personaje se calcula desde el suelo y el tamaño de su cápsula. Si no hay suelo o espacio suficiente, el viaje se cancela con una advertencia en la consola.

Se reutiliza `FPSController.TeleportTo` para reiniciar movimiento, parkour, escalada y cámara al viajar.

## Contacto con el suelo

Los MeshCollider de `Scenario_FS (1)/Cuadras.001` y `Cuadras.002` estaban en modo convexo. Sus envolventes cubrían parte de la calle a Y=19,664, aunque la calle visible está a Y=19,316. Se desactivó Convex en esos dos colliders estáticos para usar su geometría real.

`useGroundContactShadow` quedó apagado en el Player. Se conserva el sistema y su shader, por si se quiere volver a activar; ya no genera las manchas rojas de los pies.

## Verificación

- Unity compiló sin errores nuevos.
- 21 pruebas EditMode pasaron: teletransporte, parkour y cálculo del contacto.
- En Play Mode se comprobó el inicio en Lugar 1 y varios viajes de ida y vuelta mediante el mismo comando que llaman los atajos.
- Se inspeccionaron capturas de los pies en ambos lugares y se confirmó contacto físico con el suelo y ausencia de los objetos rojos.
- Los atajos físicos del teclado no se simularon; se comprobó su código y se ejecutó su comando en Play Mode.

Capturas: [Lugar 1](lugar1-suelo.png) y [Lugar 2](lugar2-suelo.png).
