# FLOWSTATE — decisiones jugables y tres bucles teóricos

Revisión: 28 de octubre de 2026, America/Santiago. Estado: análisis de archivos y propuestas; sin implementación, ejecución del juego ni decisiones aprobadas. Usa la [biblia existente](FLOWSTATE-Game-Art-Direction-Bible.md) como fuente de identidad y autoridad; este documento no la sustituye.

## 1. Base y decisiones del jugador

Unity 6000.0.41f1, URP 17.0.4, Input System 1.13.1 y Cinemachine 3.1.7 confirmados en ProjectVersion/manifest. Flujo de build: MainMenu_FlowState → Gameplay. La implementación específica acredita posibilidades y consecuencias del código, no diversión ni calidad perceptual.

| Mecánica IMPLEMENTADA | Decisión disponible | Consecuencia comprobada | Hipótesis o límite |
| --- | --- | --- | --- |
| Desplazamiento y cámara | Caminar/correr, orientar y frenar; elegir aproximación | [FPSController.StepMovement](../../Assets/01_Scripts/FPSController.cs) usa input y heading, aceleración/freno y control aéreo. Gameplay serializa 3/6 de velocidad, salto 2.02 y coyote/buffer 0.12 s | Respuesta y dominio percibidos pendientes de playtest; docs/defaults tienen velocidades distintas |
| Salto/parkour contextual | Aproximarse y pulsar salto; decidir dirección y momento | TryStartParkour detecta geometría, deriva vault/top-out/escalada y valida recepción. ClimbableSurface controla permiso/movimiento lateral; Gameplay exige marcador | No hay elección explícita de cada variante de vault: contexto decide. No se acreditó que el jugador prediga siempre el resultado |
| Escalada y wall jump | Moverse lateralmente, subir/salir o saltar entre paredes | Código de FPSController consulta superficie y dirección; Gameplay habilita wall jump | Libertad de rutas, legibilidad de superficies y recuperación deben observarse; no se infiere dificultad por el nombre del obstáculo |
| Entrada a graffiti | Elegir pared, acercarse, entrar y salir del modo | [GraffitiRaycastUI](../../Assets/01_Scripts/GraffitiRaycastUI.cs) distingue detección y distancia pintable; usa layers y raycast. Cambio de cámara y estado en FPSController | Un prompt de detección no demuestra alcance para depositar; affordance y controles contextuales por verificar |
| Pintura libre | Color/cap, precisión o cobertura, gesto y distancia | [GraffitiPainter](../../Assets/01_Scripts/GraffitiPainter.cs), [GraffitiGestureDynamics](../../Assets/01_Scripts/GraffitiGestureDynamics.cs), [Pintando](../../Assets/01_Scripts/Pintando.cs) y canvas cambian huella/deposición; Start puede cambiar el modo serializado | Autoría y satisfacción son hipótesis; no se encontró premio directo de reputación por obra libre |
| Reposicionarse frente al muro | Moverse entre trazos | Gameplay serializa graffitiMoveSpeed 9.16, pero GraffitiPainter.StartPainting establece canMove=false y StopPainting lo restaura | El valor alto afecta al reposicionamiento; no demuestra que se camine a esa velocidad durante un trazo. Comprobar transiciones reales |
| Pintura rápida | Elegir pieza/tamaño/ubicación, mantener clic, soltar y reanudar | [GraffitiQuickPainter](../../Assets/01_Scripts/GraffitiQuickPainter.cs) comprueba soporte en esquinas; limita tamaño a 0.4–2.5 por default y piezas a 128. Reanuda con pieza/escala/superficie y proximidad compatibles; finaliza con +5 CityImpact | No cualquier nuevo clic reanuda la misma obra; no se encontró exclusión de premios por ubicación repetida |
| Mezcla y lata | Seguir, agitar o cambiar al vaciarse; elegir consumo por cap | [SprayResourceSystem](../../Assets/01_Scripts/SprayResourceSystem.cs): agitar restaura mezcla y detiene emisión; cambiar exige lata vacía y disponibilidad. Caps tienen consumos distintos | Defaults infiniteCanSupply=true y muteSprayAudio=true; no asumir economía escasa ni feedback audible. Añadido al iniciar, sujeto a estado real |
| Pausa y menús | Consultar/seleccionar, cancelar y retomar | [PauseMenuController](../../Assets/01_Scripts/PauseMenuController.cs) congela tiempo, suspende input, cancela trazo y restaura estado. Menú/cambio de cap interrumpe pintura | Claridad de Q/E/Tab/R/F/Esc y cobertura de mando requieren QA; el menú principal usa Input Actions y gameplay principalmente legacy |
| Reputación | Elegir acciones premiadas frente a pintar libremente | [ReputationData](../../Assets/01_Scripts/Reputation/ReputationData.cs): tres ejes 0–100, pesos 35/30/35. ReputationSystem persiste valores. Premios encontrados: +5 CityImpact por pieza rápida/decal | No se acreditaron encargos, habilidades o desbloqueos completos. Arte del mundo no tiene persistencia encontrada en sus propietarios revisados |

## 2. Ritmo, profundidad, dificultad, aprendizaje y feedback

**INTERPRETACIÓN — ritmo:** existen alternancia de recorrido y atención al muro, más pausas de selección, mezcla y reposición. Esa alternancia puede permitir tensión y descanso; no hay evidencia de su cadencia ideal. Un temporizador global que siga corriendo al crear podría castigar expresión y lectura de la libreta.

**INTERPRETACIÓN — profundidad:** ruta/entrada al muro, cap/gesto/color y libre/rápido son ejes de decisión existentes. Combinarlos puede aportar estrategias distintas. Falta acreditar que el mundo y los incentivos hagan relevantes esas diferencias sin una estrategia dominante.

**RIESGO — equilibrio:** con defaults de reputación 35/20/45 y solo +5 CityImpact encontrado, once piezas rápidas saturarían ese eje y el total pasaría de 34 a 53.25. Es cálculo condicionado, no resultado de una partida. Premiar más la repetición podría agravar el problema antes de dar significado a los otros ejes.

**HIPÓTESIS — dificultad y aprendizaje:** Space resuelve varias acciones contextuales y la pintura tiene múltiples teclas/modos. Introducir una acción con señal, práctica segura, variación y combinación puede facilitar aprendizaje; debe observarse si las decisiones resultantes son previsibles. No hay prueba aquí de frustración, aburrimiento o dificultad excesiva.

**IMPLEMENTADO / PENDIENTE — feedback:** hay preview y cursor de muro, información de recursos, animaciones, cámara y aerosol por capas. Su sincronización, claridad y satisfacción no quedan certificadas por el código. La preferencia de movimiento reducido no cubre todos los HUD revisados; sonido de spray por defecto silenciado.

**HIPÓTESIS — emergencia:** el orden de paredes puede cambiar la ruta y el punto de vista de una composición; cap/mezcla puede cambiar el ritmo de preparación. Aún no son bucles conectados. La colocación de pintura no crea automáticamente geometría escalable, señalización semántica ni un sistema de objetivos.

## 3. Tres integraciones alternativas — PROPUESTAS NO APROBADAS

Complejidad relativa a los sistemas actuales: contempla coordinación, datos, UI, validación y contenido; no es una estimación de calendario. Las recompensas y nuevas reglas descritas no están implementadas. Ninguna propuesta exige cambiar FSSRS, Industrial Dusk o la jerarquía cromática del graffiti.

### A. Spots de autor — explorar para elegir dónde dejar la marca

**Experiencia deseada:** curiosidad, autonomía y satisfacción de alcanzar un lugar elegido.

**Bucle:** ver un muro de interés → elegir aproximación segura o exigente → superar el recorrido → elegir obra libre/rápida y encuadre → terminar voluntariamente → descubrir otro punto desde esa posición. La vista obtenida y la composición son recompensas intrínsecas; un reconocimiento por ubicación única sería una extensión a diseñar.

**Reglas propuestas:** no hay reloj. Un spot contiene pared válida y espacio de recepción; su ruta alternativa no promete parkour nuevo. En obra libre, «terminar» es una confirmación del autor a diseñar, no puntuación de belleza ni detector actual. Fallar el recorrido conserva el spot disponible; cancelar pintura no entrega un premio de finalización. La futura elegibilidad por spot requiere ID y registro; no existe por deducción de la posición del trazo.

**Estados:** descubierto → en aproximación → frente al muro → creando → autor confirma/completa pieza → visitado. Retirada devuelve a exploración; recepción fallida permite reintentar sin inventar un sistema de muerte o checkpoint.

**Ventajas:** aprovecha más infraestructura, respeta autoría y permite ritmos personales. **Riesgos:** caminar entre muros puede carecer de decisión; las rutas deben ser distinguibles y la recompensa explicable. **Complejidad:** baja para evaluación moderada manual; media si incorpora objetivos, UI y persistencia de spots/obras.

**Prototipo futuro reversible:** un tramo existente de Parkour_Test_Blockout y una pared previamente verificada, con un diseñador indicando objetivo/registrando llegada y confirmación; sin modificar el juego ni dar premios durante esa simulación. Verificar distancia, vista y recepción antes de afirmar que la conexión espacial funciona. La simulación prueba interés del recorrido, no usabilidad de una interfaz aún ausente.

**Prueba discriminante:** ¿una persona nueva elige una ruta, entiende por qué quiere llegar y encuentra una composición propia sin atajos F7/F8 ni presión de puntos? Registrar ayudas, abandonos y elección de ruta; comparar con recorrer sin objetivo de mural.

### B. Línea y firma — reto opcional de dominio del recorrido

**Experiencia deseada:** fluidez, precisión y mejora personal.

**Bucle:** elegir reto → leer ruta corta → encadenar maniobras correctas → completar una pieza rápida en el destino → revisar resultado y reintentar una variante. Ruta/orden y limpieza de ejecución aportan dominio; el feedback final debe explicar cada condición.

**Reglas propuestas:** reto voluntario separado del paseo libre. La ventana temporal afecta al recorrido; se detiene al llegar al destino válido, para no convertir la selección/pintura en una carrera. Cuenta maniobra completada, no pulsaciones; requiere coordinación nueva. Al fallar se pierde el resultado del reto, no reputación ni obras existentes. Reinicio/teletransporte invalida el intento; pausa suspende el reloj. El replay conserva mejora personal; un premio económico repetido exige otra decisión y protección.

**Estados:** sin reto → listo → recorriendo → recorrido válido/fallido → firmando → resultado → nuevo intento. Soltar y reanudar una pieza no debe duplicar resultado. Una vuelta fuera de reto sigue disponible.

**Ventajas:** aceptación funcional clara y espacio de práctica/máster. **Riesgos:** favorece piezas rápidas frente a autoría libre; puede parecer un minijuego separado y generar repetición rentable. **Complejidad:** media-alta por eventos, reloj, reglas de invalidación, resultado, reintento y tuning; no requiere inventar otra locomoción.

**Prototipo futuro reversible:** moderador y cronómetro externo sobre una ruta validada, confirmando maniobras observadas y deteniendo medición antes de seleccionar la obra; sin puntuación persistida ni HUD nuevo. No confundir este ensayo con prueba del reloj/input implementados.

**Prueba discriminante:** ¿los jugadores entienden fallos y quieren repetir por dominio, sin sentir que deben apresurar su firma? Comparar intento inicial y posterior, causas de fallo y deseo expresado de repetir; no atribuir mejora solo a la recompensa.

### C. Mural por alturas — recorrer para componer una obra espacial

**Experiencia deseada:** planificación, apropiación del espacio y composición desde varios puntos de vista.

**Bucle:** observar una composición distribuida → elegir panel y secuencia de acceso → llegar mediante parkour → crear una parte → cambiar de altura para revisar el conjunto → ajustar o terminar → buscar otra composición. No presupone multijugador.

**Reglas propuestas:** conjunto de superficies físicas válidas, no un canvas único supuesto sobre geometría arbitraria. Cada sector identifica un lugar/estado de trabajo, no evalúa belleza. El autor decide finalización; se puede visitar otro sector sin perder trazos. Para un producto final se necesitan IDs, persistencia y UX de obra conjunta aún no acreditados. Moverse a otro sector no activa un premio; la aceptación única del conjunto requiere una regla nueva, separada de los +5 actuales.

**Estados:** composición explorada → sector elegido → acceso → intervención → revisión desde otro punto → continuar/finalizar. La dificultad procede de llegar y leer el espacio, con ruta alternativa; un fallo de recorrido no destruye partes creadas.

**Ventajas:** integra parkour y autoría de forma espacial, da razones para volver y mantiene ritmo contemplativo. **Riesgos:** carga de orientación, distancia entre sectores, frustración al retocar y pérdida de obra si no se resuelve persistencia. **Complejidad:** alta por contenido/métricas, agrupación de superficies, estado conjunto, datos, guardado y comunicación.

**Prototipo futuro reversible:** dos superficies existentes verificadas a alturas distintas y un punto de revisión; ensayo moderado con registro manual de intención del autor, sin agrupar canvases o guardar datos nuevos. Si el recorrido actual no las conecta, queda pendiente una preparación espacial autorizada.

**Prueba discriminante:** ¿cambiar de posición mejora la decisión artística o solo añade desplazamiento? Registrar visitas, retoques, desorientación y motivo de cada traslado; comparar con una superficie equivalente accesible desde un único punto.

## 4. Comparación y siguiente decisión

| Alternativa | Elección central | Riesgo/coste de experiencia | Complejidad de producto | Qué incertidumbre resuelve primero |
| --- | --- | --- | --- | --- |
| A. Spots de autor | Dónde ir y qué dejar allí | Trayectos sin propósito o recompensa repetida | Media; ensayo manual bajo | Si alcanzar un muro puede motivar exploración y creación |
| B. Línea y firma | Cómo dominar una secuencia | Presión, predominio del modo rápido y repetición | Media-alta | Si el reto voluntario añade dominio sin castigar autoría |
| C. Mural por alturas | Cómo recorrer para componer | Desorientación, coste de retoque y continuidad de obra | Alta | Si desplazarse es parte del acto creativo |

**Recomendación de diseño, pendiente:** empezar evaluando A por su menor dependencia de sistemas nuevos y compatibilidad con creación libre. B explora otra audiencia/motivación y necesita ser optativo. C merece estudio después de verificar continuidad de obras y conveniencia espacial. No es una priorización aprobada ni autoriza prototipos.

El siguiente paso, si se solicita, es elegir una hipótesis y preparar un ensayo acotado. Cada incorporación definitiva requerirá revisar evidencia y aprobación del desarrollador. No se fijan aquí premios, presupuestos temporales, desbloqueos o cambios visuales como decisiones finales.
