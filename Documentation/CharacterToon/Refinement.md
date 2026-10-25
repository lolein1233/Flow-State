# FLOW STATE — refinamiento del Cartoon original

24 de octubre de 2026. Unity 6000.0.41f1 / URP 17.0.4 / GTX 1650.

La versión final parte del shader original restaurado. Conserva el contorno fragmentado, las cuatro placas de papel, emoción, tinta y eco cromático, sus grosores base, desplazamientos y colores. La actualización de ondas descrita a continuación intensifica la amplitud animada. La malla visible, el componente PlayerComicBorder y la escena son los originales.

## Actualización — respiración más notoria y ondas

Ante la petición de hacer la animación más visible, se amplió únicamente `PlayerComicPlate.shader`:

- Una envolvente compartida expande y contrae las cuatro placas. Su amplitud base es del 22 %, modulada por la emoción; un segundo armónico añade una inhalación/exhalación menos mecánica.
- Dos ondas de distintas longitudes recorren la silueta en sentidos opuestos. Utilizan coordenadas relativas en unidades del mundo para que su escala no dependa de las unidades del modelo importado.
- El eco cromático y el color reciben más onda que el papel y la tinta. Se conserva la estructura fragmentada y se limita la expansión total entre 0,4 y 1,75 veces el grosor base.
- Los pigmentos forman cintas curvas en movimiento, conservando las celdas de breakup y la paleta existente.
- Un desplazamiento secundario pequeño acompaña las ondas. No se añaden geometría, render passes, texturas ni componentes.
- El shader incorpora `Breathing Strength`, `Traveling Waves` y `Wave Tempo`, todos con valor inicial 1.

Se grabó una secuencia en Play Mode con avance real de frames. El vídeo conserva los tiempos de captura; su frecuencia de muestreo no es una medida de los FPS del juego. Vista previa: `Waves/index.html`.

También se preparó una vista con pose fija y 180 muestras temporales del shader a 30 Hz. Usa una copia temporal en memoria que permite controlar únicamente el tiempo del efecto; no modifica el shader guardado ni interpola imágenes. Esta vista permite observar las ondas sin confundirlas con la animación idle. La copia y sus materiales de revisión se liberan al terminar. Los cuatro tests existentes volvieron a pasar y las variantes normal e instanciada del contorno compilaron sin mensajes de error.

El respaldo inmediatamente anterior a esta actualización está en `Backup-BeforeWaveEnhancement/PlayerComicPlate.shader`. Permite revertir solo las nuevas ondas conservando los refinamientos previos.

## Mejoras aplicadas

- La cadencia dibujada del contorno conserva sus poses y frecuencia; ahora interpola su pequeño cambio de ruido con una curva suave entre pulsos.
- La respiración conserva un 75 % de la fase propia de cada placa y comparte un 25 % con las demás. Se mantienen las ondas, paneles, registro y amplitudes existentes.
- El ciclo de pigmentos enlaza acento → tono medio → luz → acento con transiciones suaves, usando los colores emocionales existentes.
- Las fases temporales usan float para evitar la pérdida de precisión de half en sesiones prolongadas.
- El registro y la trama del contorno utilizan las dimensiones de renderizado escaladas de URP.
- El cel shading conserva sus bandas y su curva de valores. Se filtran sus fronteras subpíxel y los bordes del rayado.
- Las luces HDR conservan ligeramente mejor su cromatismo: la nueva corrección se mezcla al 18 %, sin alterar la paleta base ni el contraste original.
- El rim original conserva intensidad y potencia; recibe una respiración máxima adicional de ±2,5 %, modulada por la energía emocional.

## Alcance exacto

Se modificaron únicamente `FlowStylizedLit.shader`, `PlayerComicPlate.shader` y los nueve materiales que utiliza MAIKOL. En cada material solo se añadió `_PrintRefinement: 1`; todos los parámetros anteriores conservan su valor original. El control vale cero por defecto en materiales ajenos al personaje.

No se añadieron shaders, mallas, componentes, variantes de keywords ni render passes. Las referencias GUID existentes se conservan. El componente, las pruebas y sus assemblies se restauraron sin diferencias; `Gameplay.unity` coincide con el respaldo tomado al comenzar este trabajo. Los cambios que ya existían en el proyecto antes del trabajo se conservaron.

## Verificación

- Unity CLI confirmó `compilationFailed: false` y ambos shaders soportados en la GTX 1650.
- Unity MCP: ningún mensaje de error de compilación en los dos shaders y ninguna referencia de material/shader ausente.
- Los cuatro tests EditMode de `FSSRS.EditorTests` pasaron; cero fallos.
- Play Mode avanzó del frame 1790 al 2565, con cambios de pose del esqueleto. Se inspeccionaron capturas del personaje animado desde frente, tres cuartos y espalda.
- Se inspeccionaron capturas con iluminación neutra, cian, magenta, violeta, cálida intensa, tenue, y a mayor distancia. Se registraron los cinco estados emocionales existentes.
- La escena terminó fuera de Play Mode y sin cambios pendientes.

Las imágenes `before-*` utilizan los shaders originales compilados temporalmente en memoria y restaurados después de capturar. Las imágenes `refined-*` muestran la versión final. Las imágenes `live-*` pertenecen a la comprobación en Play Mode. Los instantes de las animaciones no son idénticos entre fotografías; no constituyen una comparación temporal sincronizada.

## Límites de validación

Se comprobó la animación idle activa, no el conjunto completo de clips de parkour y combate. No se generó un ejecutable ni se midió el tiempo GPU en una build: no se afirma una mejora de FPS ni un presupuesto GPU certificado. Se conserva el sistema original de pases auxiliares y renderizado; esta revisión no rediseña esa arquitectura.

## Respaldo

`Backup-20261024/` contiene los archivos originales recuperables y el estado del árbol de trabajo inicial. Para revertir esta revisión bastan los dos shaders y los nueve materiales originales de esa carpeta. No hace falta restaurar escenas o configuraciones de proyecto.

Abrir `Comparison.html` para comparar las capturas originales y finales.
