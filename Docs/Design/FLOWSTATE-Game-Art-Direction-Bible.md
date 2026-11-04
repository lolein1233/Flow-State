# FLOWSTATE — Game & Art Direction Bible inicial

Fecha de revisión: 28 de octubre de 2026, America/Santiago.

Estado: referencia descriptiva y de trabajo, **pendiente de aprobación como biblia creativa definitiva**. No introduce mecánicas, estética, valores de tuning o compromisos de producción nuevos. El desarrollador mantiene la autoridad de decisión.

## Cómo interpretar este documento

| Estado | Significado |
| --- | --- |
| IMPLEMENTADO | Existe evidencia de código, asset o serialización. No implica calidad jugable, integración runtime completa o aprobación artística. |
| INTENCIÓN DOCUMENTADA | Un documento del proyecto expresa una dirección o propósito. Puede describir una versión histórica. |
| INTERPRETACIÓN | Síntesis de la evidencia, formulada para orientar una conversación de diseño. |
| PROPUESTA | Oportunidad o solución aún no aceptada. |
| PENDIENTE DE VERIFICAR | Hace falta inspección o prueba concreta para establecer el comportamiento o calidad. |
| APROBADO | Hay una instrucción o registro verificable del desarrollador que cubre esa decisión. |

Las instrucciones actuales y AGENTS.md definen el alcance permitido. Para describir funcionamiento contrasta serialización, referencias de prefabs, código de inicialización y observación runtime. Para establecer intención o aprobación usa su procedencia explícita: el código por sí solo no las demuestra.

La auditoría creativa previa de esta conversación es el punto de partida. No se encontró un informe creativo independiente guardado en el repositorio. Esta versión recontrasta las fuentes necesarias sin ejecutar el juego; las observaciones MCP y capturas anteriores conservan carácter histórico. No se reejecutaron compilación, pruebas, audio ni profiling para redactar esta biblia.

## 1. Base real y pilares identificados

La formulación de los pilares siguientes es una **INTERPRETACIÓN** de sistemas existentes, no una nueva jerarquía creativa aprobada.

| Pilar identificado | Evidencia IMPLEMENTADA | Límite actual |
| --- | --- | --- |
| Recorrido corporal por el espacio urbano | Movimiento con aceleración/frenado, salto tolerante, vault, hang, escalada, top-out, wall jump y air roll en [FPSController](../../Assets/01_Scripts/FPSController.cs) y [ClimbableSurface](../../Assets/01_Scripts/ClimbableSurface.cs). Circuito Parkour_Test_Blockout en Gameplay. | Ritmo, dificultad, métricas y game feel requieren playtest en la escena real. |
| Autoría expresiva mediante pintura | [GraffitiPainter](../../Assets/01_Scripts/GraffitiPainter.cs), [Pintando](../../Assets/01_Scripts/Pintando.cs), [GraffitiGestureDynamics](../../Assets/01_Scripts/GraffitiGestureDynamics.cs), superficies de pintura y cursor de pared. Gesto, distancia, caps y mezcla alteran la huella. | No atribuir persistencia entre sesiones a la obra jugable sin verificarla. |
| Continuidad entre recorrer y dejar una marca | Cámara de tercera persona y modo de graffiti en primera persona; libreta y piezas rápidas en [GraffitiQuickPainter](../../Assets/01_Scripts/GraffitiQuickPainter.cs) y QuickGraffitiBook. | La conexión con objetivos y recompensas no constituye todavía un ciclo completo acreditado. |
| Mundo industrial impreso con autoría cromática | FSSRS, paletas, contorno de papel/tinta del personaje, materiales y atmósfera Industrial Dusk. | La consistencia de rutas, lectura y efectos debe comprobarse desde la cámara del jugador. |
| Reconocimiento social como soporte incipiente | [ReputationData](../../Assets/01_Scripts/Reputation/ReputationData.cs) y [ReputationSystem](../../Assets/01_Scripts/Reputation/ReputationSystem.cs): Recognition, CrewRespect y CityImpact persistidos. | Infraestructura existente; motivaciones, economía, encargos y desbloqueos completos no acreditados. |

## 2. Lo que ya existe

### Entorno y ruta de producto

- [ProjectVersion](../../ProjectSettings/ProjectVersion.txt): Unity 6000.0.41f1. [Manifest](../../Packages/manifest.json): URP 17.0.4, Input System 1.13.1, Cinemachine 3.1.7, uGUI 2.0.0, Timeline 1.8.7 y VFX Graph 17.0.4. Revalidar antes de usar APIs.
- [BuildSettings](../../ProjectSettings/EditorBuildSettings.asset) habilita MainMenu_FlowState y Gameplay. [00_JUGAR](../../Assets/09_MainMenu/Options/00_JUGAR.asset) apunta a Gameplay.
- Game, Level, intro, LookDev y trailer son escenas existentes con funciones particulares; no equivalen al flujo de build actual.

### Movimiento, cámara y nivel

- FPSController es propietario de movimiento, parkour y cámara; el desplazamiento no depende de root motion. La cámara propia incluye colisión y transiciones de modo. Cinemachine instalado no demuestra que controle gameplay.
- [Gameplay](../../Assets/02_Escenas/Gameplay.unity) serializa caminar 3, sprint 6, graffitiMoveSpeed 9.16 y salto 2.02; coyote/buffer 0.12 s. Son valores observados, no recomendaciones ni tuning aprobado por este documento.
- El jugador tiene escala uniforme 1.51304 y cápsula de altura local 2. Las métricas de obstáculos deben partir de dimensiones reales, no solo de nombres históricos.
- Parkour_Test_Blockout y Scenario_FS (1) están activos en la escena guardada. Sewer_TP_Network está inactivo. Los marcadores Lugar1/Lugar2 y F7/F8 son herramientas de presentación, no un sistema de objetivos.
- CombatLockOnController y resaltados existen. No se acreditó un combate completo con ataque/daño/IA en los scripts revisados.

### Pintura y reputación

- Pintura libre y rápida conviven. Needle, Soft Round, Fat Cap, Chisel y Splatter tienen perfiles y lecturas propias. La precisión/cobertura responde al gesto; la boquilla, cursor, preview y deposición comparten referencias.
- [GraffitiSurfaceCanvas](../../Assets/01_Scripts/GraffitiSurfaceCanvas.cs) limita los recursos de pintura. Son decisiones de implementación que conviene preservar al ampliar autoría.
- GraffitiPainter crea sistemas de recurso/HUD/VFX al iniciar cuando faltan. Su lógica Start puede alterar el modo serializado: leer el Inspector aislado no prueba el modo final.
- Reputación pondera Recognition/CrewRespect/CityImpact en 35/30/35 %. Defaults de desarrollo: 35/20/45. Las recompensas encontradas en GraffitiPainter y GraffitiQuickPainter suman +5 a CityImpact; no se encontró premio equivalente para pintura libre ni un ciclo completo para los otros ejes.
- El prefab [StreetReputationMenu](../../Assets/Resources/UI/StreetReputationMenu.prefab) existe y Gameplay lo instancia con raíz inactiva. Su apertura, visibilidad y ciclo de vida quedan pendientes de prueba.
- La persistencia de reputación está implementada. La persistencia entre sesiones de obras libres/rápidas no se encontró en los propietarios inspeccionados. El guardado de pintura del menú es un sistema separado y no demuestra persistencia de las obras del mundo.

### FSSRS y dirección gráfica

- [PC_RPAsset](../../Assets/Settings/PC_RPAsset.asset) tiene dos renderers. Gameplay selecciona índice 1: [FSSRS_PC_Renderer](../../Assets/07_StylizedRendering/FSSRS/Renderer/FSSRS_PC_Renderer.asset), con SSAO, decals y FSSRS Print Composite.
- [FSSRSRendererFeature](../../Assets/07_StylizedRendering/FSSRS/RendererFeatures/FSSRSRendererFeature.cs) usa Render Graph y compone antes de transparencias. El shader Stylized Lit, paletas y PlayerComicBorder forman un sistema propio; no es solo un filtro de postprocesado.
- [FlowStatePaletteController](../../Assets/07_StylizedRendering/FSSRS/Runtime/FlowStatePaletteController.cs) define cuatro emociones: Comic, Neutral, Anger y CreativeFlow. La transición predeterminada es 0.65 s. Se encontraron llamadas desde demo y showcase; no se acreditó una conexión sistemática con parkour o reputación.
- El [documento FSSRS](../../Assets/07_StylizedRendering/FSSRS/Documentation/FLOW_STATE_STYLIZED_RENDERING.md) conserva reglas de papel, tinta y saturación del graffiti. Indica expresamente que las motas de tinta y puntos de semitono globales están desactivados en la dirección actual.

### Industrial Dusk e iluminación

- [Gameplay_IndustrialDusk](../../Assets/ArtDirection/IndustrialDusk/Gameplay_IndustrialDusk.asset) está enlazado al volumen Industrial Dusk de Gameplay con prioridad 200 y peso 1; coexiste con otros volúmenes. Parámetros relevantes: exposición 0.25, contraste 12, saturación 2, bloom 0.2, viñeta 0.17 y grano 0.055; blur y aberración con intensidad 0.
- Existen cielo, niebla azul grisácea, materiales de prácticas Amber/Cyan/ServiceGreen/Violet y mapas de reflexión en [IndustrialDusk](../../Assets/ArtDirection/IndustrialDusk).
- La lectura «atmósfera fría industrial con acentos cálidos y cromáticos» es una INTERPRETACIÓN de esos recursos, no una paleta nueva aprobada. Sus cantidades no demuestran coste GPU ni calidad visual.

### UI, animación, efectos y audio

- Menú principal y selector de graffiti utilizan aerosol, papel recortado, tinta, tipografía OWNED y acentos cromáticos. [MainMenu](../../Docs/AI/MainMenu.md) y [Neo Spray Lab](../../Docs/AI/GraffitiMenuNeoSprayLab.md) documentan sus refinamientos.
- [PauseMenuController](../../Assets/01_Scripts/PauseMenuController.cs) contiene pausa, ayuda por páginas y restauración de estado; GraffitiModeHudVisibility conserva visibilidad contextual. Configuración de menú: volumen maestro y movimiento reducido. SprayResourceHUD no consume esa preferencia en la revisión realizada.
- MenuInput utiliza Input Actions y contempla mando; gran parte de gameplay usa input legacy. Cobertura de mando y remapeo integral no acreditados.
- [MAIKOL.controller](../../Assets/06_Modelos/MAIKOL/MAIKOL.controller) incluye locomoción y estados de parkour; FPSController usa desplazamiento real para animar y limita cadencia. No se acredita ausencia de deslizamiento/contactos defectuosos sin observación temporal.
- [GraffitiSprayVFX](../../Assets/01_Scripts/GraffitiSprayVFX.cs) genera pigmento, nube, microgotas y pulso gráfico con ParticleSystem. VFX Graph instalado no equivale a assets .vfx implementados.
- [SprayResourceSystem](../../Assets/01_Scripts/SprayResourceSystem.cs) tiene por defecto muteSprayAudio=true e infiniteCanSupply=true. Son defaults de código, sujetos a la inicialización real. No se encontró un AudioMixer en el inventario revisado.
- Hay música y sonidos de menú. [DiegeticMusicPlayer](../../Assets/01_Scripts/DiegeticMusicPlayer.cs) se declara maqueta musical. El trailer tiene escena, Timeline y recursos dedicados en [10_Cinematics](../../Assets/10_Cinematics); sus notas de validación son históricas.

## 3. Lo documentado como intención

| Intención documentada | Fuente | Tratamiento |
| --- | --- | --- |
| Mundo como impresión industrial incompleta; papel/tinta organizan valores y graffiti conserva saturación/autoría principal | Documento FSSRS, Visual premise / Render order | Referencia de coherencia existente; no sustituirla silenciosamente. |
| Silueta gráfica del jugador separada del resaltado de objetivos; efectos intensos reservados a momentos acotados | Documento FSSRS, Core controls / Quality guidance / Extension points | Conservar la distinción al proponer feedback nuevo. |
| Gesto lento de precisión, barrido de cobertura y reducción del cono al girar | [Physical Spray Upgrade](../../Docs/AI/GraffitiPhysicalSprayUpgrade.md) y código asociado | Mantener expresividad y comprobar cualquier modificación del gesto en juego. |
| Selector de caps como instrumento físico editorial, sin obstruir superficies de interacción | Neo Spray Lab, Refinement pass / Interaction | Reutilizar gramática y separar presentación de parámetros de pintura. |
| Cámara inmediata en desplazamiento horizontal, cadencia natural y animación por movimiento real | [Movement](../../Docs/AI/Movement.md) y FPSController | Intención útil; valores de velocidad del documento no coinciden con Gameplay actual. |
| Menú pintado con lata oficial, branding, persistencia acotada y transición de aerosol | MainMenu y sus fases | Sistema propio; confirmar estado actual de cada detalle antes de extenderlo. |

## 4. Contradicciones y verificaciones abiertas

| Evidencia en conflicto o incompleta | Resolución descriptiva | Próxima comprobación pertinente |
| --- | --- | --- |
| Movement.md dice 1.8/5.5; Gameplay serializa 3/6 y graffiti 9.16 | Registrar ambas fuentes; no ajustar valores por documentación histórica | Recorrido con configuración real y evaluación de precisión/ritmo |
| Documento FSSRS dice cinco emociones y renderer exclusivo LookDev | Enum actual tiene cuatro; Gameplay selecciona renderer 1 | Comprobar transiciones desde cámara jugable cuando se autorice |
| UnityProjectContext describe Game como inicio y pruebas antiguas | BuildSettings actual usa MainMenu_FlowState → Gameplay | Mantener inventario y resultados separados por fecha |
| Reputación tiene tres ejes, pero premios encontrados solo aumentan uno | Progresión incipiente; evitar prometer desbloqueos o consecuencias inexistentes | Acceso UI, repetición de premios y persistencia de obra |
| Movimiento reducido existe en menú, HUD mantiene movimiento propio | Accesibilidad parcial | Verificar cámara/HUD/menús con preferencia reducida |
| Assets/código de audio y cámara existen; calidad temporal no probada aquí | Capacidad e implementación no certifican acabado | Escucha local y revisión de contactos/encuadre en ruta representativa |

Las suites existentes incluyen movimiento, parkour, graffiti, piezas rápidas, menús y FSSRS. La auditoría previa descubrió 121 pruebas, sin ejecutarlas; esa cifra es histórica y no constituye una suite aprobada ni cobertura completa del tuning serializado.

## 5. Oportunidades de mejora — propuestas pendientes

| Oportunidad | Valor esperado para el jugador | Validación futura |
| --- | --- | --- |
| Primera ruta completa: recorrer → alcanzar pared → crear → comprender consecuencia | Unir las capacidades actuales en una experiencia con propósito | Playtest sin atajos de presentación ni instrucciones externas |
| Funciones distintas para los tres ejes de reputación | Entender qué se gana y por qué volver | Mapa de acciones/recompensas, repetición y explicación del jugador |
| Conservar obras del mundo | Dar continuidad a la autoría | Guardar/cargar pintura libre y rápida sin pérdidas ni premios duplicados |
| Enseñanza contextual y preferencias compartidas | Reducir carga de controles manteniendo profundidad | Primer uso de pintar/caps/agitar/cambiar lata y señales accesibles |
| Contactos y feedback audiovisual coherentes | Comprender acciones y consecuencias con confianza | Revisión de pies/manos/cámara y eventos spray/impacto/recompensa |
| Gramática espacial de luz/material | Explorar con orientación y conservar atmósfera | Distinguir ruta, desvío y muro pintable desde cámara de jugador |
| Emoción visual vinculada a situaciones significativas | Expresar estado sin dominar permanentemente la imagen | Comparación de lectura, intensidad reducida y comprensión del cambio |

Estos ejemplos no fijan un diseño de misiones, reglas de combo, precios, desbloqueos o métrica de calidad definitiva.

## 6. Registro de decisiones y autoridad

### Restricciones confirmadas por el desarrollador

- **G-01 — Conservación.** Procedencia: AGENTS.md y solicitud de creación de esta skill. Preservar sistemas e identidad existentes; no reconstruirlos por defecto.
- **G-02 — Alcance de esta entrega.** Procedencia: solicitud actual. Crear skill y documentación; no modificar scripts, escenas, prefabs, materiales, shaders, animaciones, ajustes del juego ni instalar paquetes.
- **G-03 — Autoría.** Procedencia: solicitud actual. Las propuestas no se presentan como decisiones artísticas definitivas. No es necesario modificar AGENTS.md para instalar esta skill.

Estas son restricciones de trabajo, no una ratificación formal de todos los assets/tunings existentes. No se dispone de un registro independiente de aprobaciones artísticas definitivas; las intenciones conservadas arriba mantienen su procedencia documental.

### Decisiones creativas que requieren aprobación

| ID | Decisión abierta | Material necesario antes de decidir |
| --- | --- | --- |
| C-01 | Ratificar, ajustar o priorizar la formulación de pilares | Evidencia existente y una experiencia corta representativa |
| C-02 | Objetivos, encargos y función de cada eje de reputación | Propuesta de ciclo y consecuencias sin evaluar arbitrariamente el arte |
| C-03 | Alcance y límites del guardado de obras | Experiencia deseada, formato/volumen de datos y pruebas de carga |
| C-04 | Eventos, intensidad y significado de los cuatro estados emocionales | Propuesta de activación y revisión de legibilidad/accesibilidad |
| C-05 | Política de recursos de aerosol | Compromiso entre práctica expresiva, ritmo y escasez |
| C-06 | Dispositivos, accesibilidad y presupuesto técnico objetivo | Prioridades de producto y condiciones de prueba |

No hay aprobación implícita de estas decisiones por incluirlas en esta tabla. Una aprobación futura registrará fecha, autor/procedencia, alcance, formulación y condición de revisión, conservando la propuesta original cuando ayude a entender el compromiso.

## 7. Uso y mantenimiento

La skill [flowstate-creative-direction](../../.agents/skills/flowstate-creative-direction/SKILL.md) utiliza esta biblia de forma selectiva. Revalidar fuentes que cambien; no duplicar reglas en AGENTS.md ni tratar una captura histórica como aceptación actual.

Una iteración de diseño documenta propuesta, hipótesis de prototipo, revisión visual, playtest y refinamiento. Cada fase ejecutable requiere alcance autorizado. Registrar resultados reales y pruebas no realizadas; las mejoras de rendimiento necesitan evidencia comparable.
