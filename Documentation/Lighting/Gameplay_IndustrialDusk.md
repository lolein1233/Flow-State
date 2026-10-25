# FLOWSTATE — Gameplay: Industrial Dusk

Escena: `Assets/02_Escenas/Gameplay.unity`.
Unity 6000.0.41f1 / URP 17.0.4. Cambios realizados principalmente mediante Unity MCP.

## Resultado

Atardecer temprano con ciudad y grafitis legibles, sol bajo cálido, relleno de cielo frío y acentos industriales cian, azul eléctrico, violeta, verde frío y ámbar. La iluminación física no utiliza rojo como base. Las paletas originales del sistema emocional se conservan; Ira mantiene su carmín intencional.

## Organización y luces

Nuevo grupo `FLOWSTATE | INDUSTRIAL DUSK`, dividido en claves arquitectónicas, acentos/ruta, fuentes emisivas, atmósfera y reflexiones.

- Luz direccional existente: sol a 18° de elevación, orientación Y=65°, intensidad 1.05, color cálido suave (1, 0.87, 0.69), sombras suaves.
- Nueva direccional de cielo: intensidad 0.65, azul grisáceo (0.64, 0.73, 0.90), sin sombras; recupera la lectura de la ciudad.
- 49 focos originales de farolas revisados: 24 activos y 25 desactivados para evitar parejas redundantes. Alcance 20, cono 76°, sin sombras; blancos fríos/cian con ámbar puntual. Se añadieron 24 superficies emisivas en sus fuentes.
- 20 focos nuevos: 10 claves arquitectónicas y 10 acentos. Cinco de ellos proyectan sombras.
- Ocho focos nuevos vinculados a proyectores de obra existentes, con superficies emisivas visibles: Projector.001, .002, .005, .010, .012, .014, .021 y .026. Alcance 32, cono 68°, sin sombras.
- Dos focos del bloque de pruebas: intensidad reducida de 10000 a 700, alcance 115 y color frío neutro.
- Grupo anterior `FLOWSTATE · Cinematic Blockout Lighting` desactivado, conservado en la escena.

### Nuevos focos de composición

Claves: Cyan facade rake; Violet corner separation; Boulevard blue edge; Central amber threshold; North cold entry; Construction cyan crosslight; Construction violet return; Scaffold warm upper landing; West mural cold rake; South industrial edge.

Acentos: Hero doorway cyan; Alley violet lure; Distant restrained magenta; Central service green; Construction pit cold green; Ground-floor industrial bay; Scaffold level 02; Scaffold level 03; North-east service amber; South alley violet.

Cinco elementos emisivos de fachada/servicio activos y uno conservado desactivado. El marco cian se dejó incompleto para una lectura más deteriorada y asimétrica.

## Atmósfera y reflexiones

- Ambiente plano controlado: (0.45, 0.49, 0.57), combinado con el sol y el relleno de cielo.
- Material de cielo propio; exposición 0.85 y tinte frío ligeramente desaturado. Cámara principal con Skybox.
- Niebla exponencial, densidad 0.0016, color (0.22, 0.265, 0.35), para separación de distancias.
- Dos sistemas de polvo muy discretos, máximo 80 partículas cada uno.
- Dos Reflection Probes con proyección de caja y cubemaps HDR precalculados de 128 px, asignados como Custom para persistir al entrar en Play y evitar actualizaciones en tiempo real.

## Postprocesado

Perfil propio: `Assets/ArtDirection/IndustrialDusk/Gameplay_IndustrialDusk.asset`. Prioridad 200. Los Volumes redundantes quedan desactivados; sus objetos y perfiles originales se conservan.

| Ajuste | Valor final |
| --- | --- |
| Tonemapping | ACES |
| Bloom | Intensidad 0.20, umbral 1.10, dispersión 0.62, resolución Quarter, 5 iteraciones |
| Color Adjustments | Exposición +0.25 EV, contraste +12, saturación +2, filtro blanco |
| White Balance | Temperatura -2, tinte 0 |
| Vignette | Intensidad 0.17, suavidad 0.55 |
| Split Toning | Sombras ligeramente frías, luces suavemente cálidas, balance -5 |
| Film Grain | Thin1, intensidad 0.055 |
| Depth of Field / Motion Blur / Chromatic Aberration | Desactivados |
| SSAO | Intensidad 0.65, radio 0.55, cuatro muestras, resolución reducida, desenfoque medio, distancia 65 |

La cámara conserva el renderer FSSRS (índice 1), HDR y postprocesado. Se activaron dithering y protección frente a NaN.

## Emociones FSSRS

Controlador conectado al Volume activo. Preset propio `Dusk_Emotion_Style.asset`, aplicado tanto al perfil persistente como a la instancia usada por el controlador. No se modificó el código de los shaders ni de los scripts de jugabilidad.

Se recuperaron los valores gráficos originales de tinta/contorno y se reforzaron las paletas: influencia 0.70, acento 0.52, saturación 0.40, paper lift 0.36, contorno 0.92, grosor 1.45, cinco niveles de posterización y hatching 0.18.

- Neutral: ciudad monocromática.
- Duda: violetas fríos y luces lavanda/cian.
- Ira: carmín y magenta de tensión.
- Flow creativo: cian con sombras púrpuras y acentos rosas.
- Claridad: teal más limpio y sombras azul verdosas.

Las cinco emociones se revisaron desde la misma cámara y encuadre. Los grafitis transparentes conservan sus colores: el compuesto FSSRS existente se ejecuta antes de las transparencias.

## Rendimiento y validación

- 56 luces activas en total; seis con sombras (sol y cinco spots). Las restantes son luces sin sombras, de alcance localizado salvo el relleno de cielo y los dos focos de prueba.
- Sombras URP: dos cascadas, distancia 55, mapa principal 2048 y atlas adicional 1024; spots nuevos con nivel bajo de sombras.
- SSAO reducido y bloom Quarter. Sin efectos volumétricos por ray marching ni reflexiones recalculadas por fotograma.
- Unity CLI `recompile`: actualizado, cero errores y cero advertencias de compilación.
- Validación CLI: perfil y cámara válidos, FSSRS activo, controlador enlazado, cubemaps persistentes, presupuesto de sombras correcto y sin errores en los shaders cargados revisados.
- Se comprobó la ejecución del juego con avance real de fotogramas. Validación de referencias de iluminación también ejecutada en Play.
- Unity MCP Scene Validate: cero incidencias, scripts faltantes o prefabs rotos.
- Comparación contra la copia inicial: los 532 componentes de colliders/CharacterController permanecen idénticos.

No se ejecutó un build completo ni un benchmark en una GTX 1650; los ajustes limitan el coste de iluminación, pero no constituyen una garantía de FPS para el conjunto de la escena.

## Límites visuales

La geometría activa es principalmente exterior. Lograr el encierro de los túneles de referencia requeriría cambios de arquitectura fuera del alcance de esta iluminación. Hay grafitis de gran escala, superficies de detalle limitado y transparencias que no reciben el compuesto emocional por el orden de renderizado existente. La niebla añade profundidad de distancia, pero no es volumetría física de haces de luz.

Los ajustes de sombras del PC_RPAsset y SSAO de PC_Renderer/FSSRS_PC_Renderer son assets compartidos: también afectan a otras escenas que los utilicen.

Capturas comparativas de emociones: carpeta `Previews/` junto a este informe.
