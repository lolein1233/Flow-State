# FLOWSTATE — métricas espaciales y alternativas del blockout

Revisión: 28 de octubre de 2026, America/Santiago. **Análisis y propuestas, sin implementación ni aprobación de métricas o niveles.** Se utilizaron Creative Direction y Game Design, conservando su [biblia](../FLOWSTATE-Game-Art-Direction-Bible.md) y sus ámbitos. Spots de autor y los otros bucles previos siguen siendo propuestas no aprobadas; estas alternativas no los adoptan.

## 1. Evidencia y límites

- Fuentes: Gameplay.unity, FPSController.cs, ClimbableSurface.cs, CharacterController, GraffitiRaycastUI/Pintando/GraffitiPainter, TagManager y manifest. Unity 6000.0.41f1, URP 17.0.4, ProBuilder 6.1.2, Input System 1.13.1 y Cinemachine 3.1.7.
- Unity MCP confirmó la instancia FLOWSTATE y Gameplay fuera de Play Mode, con escena limpia. CLI corroboró estado listo. Se hicieron consultas de lectura con C# compilado en memoria; no se recompilaron scripts del juego, crearon objetos o invocaron movimientos.
- La [captura JSON](Editor-Metrics-2026-10-28.json) conserva valores vivos de edición, AABBs mundiales y tres raycasts más un spherecast hipotéticos. Estos casts no movieron al personaje ni ejecutaron acciones.
- Categorías: **D** default de código; **S** serializado en Gameplay; **E** observado en Editor; **C** cálculo/interpretación; **R** probado jugando. En esta entrega **no hay métricas R nuevas** ni métricas adoptadas como estándar aprobado.
- Se usan unidades de mundo (u), no una equivalencia humana/metrológica aprobada. Los AABBs están redondeados a 0.001 u y pueden englobar espacios vacíos; no prueban pasos, recorrido, visibilidad o seguridad.

## 2. Métricas reales encontradas

| Elemento | Default / fuente | Gameplay y lectura E | Significado y límite |
| --- | --- | --- | --- |
| Cápsula | CharacterController local: altura 2, radio 0.5, centro 0 | Escala uniforme 1.51304018; altura mundial 3.02608037, radio 0.7565201 y diámetro 1.5130402 | Dimensión física de la cápsula; no tamaño visual ni ancho cómodo de corredor |
| Posición inicial | Transform Player | (114.37,1.62304,-129.57); bounds Y 0.11–3.13608 | Lectura de edición; no prueba de apoyo durante movimiento |
| Caminar / sprint | D 1.8 / 5.5 u/s | S/E 3 / 6 u/s | Velocidad objetivo, no media medida en ruta |
| Aceleración / freno | D 14 / 20 | S/E 14 / 20 u/s² | Giro, aire y colisión alteran resultado |
| Reposicionamiento graffiti | D 2.25 | S/E 9.16 u/s | StartPainting bloquea canMove; no afirmar desplazamiento a esa velocidad durante trazo |
| Salto / gravedad | D altura 1.35, gravedad -18 | S/E altura 2.02, gravedad -18 | Parámetros del impulso; alcance real pendiente |
| Coyote / buffer | D 0.12 / 0.12 | S/E 0.12 / 0.12 s | Tolerancias de input, no aumento geométrico garantizado |
| Detección frontal parkour | D 1.45, altura 0.52, radio 0.22 | S/E mismos valores | Consultas en mundo desde el cuerpo; no alcance total de manos |
| Vault máximo / top-out contextual | D 1.15 / 2.6 | S/E 1.15 / 2.6 u | Se compara máximo del bounds menos pies. 2.6 no es límite de escalada continua |
| Umbrales low / medium | D 0.65 / 0.95 | S/E iguales | Altura relativa desde pies, no altura de mesh aislada |
| Duración low / medium / high | D 0.5 / 0.42 / 0.84 | S/E 0.5 / 0.62 / 0.84 s | Trayectorias interpoladas; no duración observada de una prueba |
| Arco low / medium / high | D 0.32 / 0.55 / 0.78 | S/E iguales | Desplazamiento vertical añadido a interpolación |
| Destino vault | D mínimo frontal 1.35 | S/E 1.35 u | Código amplía con profundidad proyectada + radio + clearance; no salto fijo de 1.35 |
| Top-out | D 1.55 s, avance 0.85 | S/E iguales | Necesita detección de suelo y espacio corporal |
| Escalada vertical / lateral | D 2.15 / 1.1 | S/E 2.15 / 1.65 u/s | Se multiplica por speedMultiplier de superficie; marcadores medidos usan 1 |
| Detección / gracia escalada | D 1.15 u / 0.12 s | S/E 1.15 u / 0.28 s | Contacto y continuidad, no garantía de cruce de juntas |
| Separación de escalada | D campo 0.45 | Campo S/E 0.45; efectivo C max(0.45,radio+skin)=0.7865201 u | Offset desde pared al root; no sumar escala a todos los campos |
| Wall jump | D alcance cast 4.25, duración 0.58 | S/E iguales; radio cast 0.2, arco 0.72, subida destino 0.2 | Transferencia entre superficies válidas; no un salto balístico libre |
| Steering wall jump | D 0.55 | S/E 0.55 | El algoritmo puede intentar normal de pared como alternativa |
| Suelo | CharacterController slope 45°, skin 0.03, step 0.3 | S/E mismos valores en edición; FPS solicita step 0.52 en suelo / 0 en aire | Asignación runtime pendiente; no certificar escalón de 0.52 por propiedad |
| Snap / clearance | D 0.52 u, 13 u/s; clearance 0.06 | S/E iguales | Recuperación de contacto y recepción; no un checkpoint |
| Ground normal | D mínimo 0.55 | S/E 0.55; código usa max con cos(45°)≈0.7071 | Pendiente geométrica efectiva requiere comprobar colisión/contacto |
| Cámara FOV normal/sprint/pintura | D 62/68/60° | S/E 62/65/60° | La configuración real difiere del default de sprint |
| Cámara boom | D offset Z -4.25 | S Z -4.55, hombro X 0.46 | Espacio para cámara y oclusión deben medirse por recorrido |
| Cámara primera persona | D/S offset local (0,1.55,0.1) | C TransformPoint: 2.3452 u sobre root; objetivo Y≈3.9683 en pose actual | Objetivo calculado, no altura de ojos aprobada ni vista jugada. Los casts hipotéticos desde Y=1.8 no representan automáticamente esta cámara o la boquilla |
| Cámara colisión | D radio 0.22 / padding 0.15 | S/E 0.28 / 0.18 u; retorno 0.18 s | Un paso corporal viable puede comprimir u ocultar el encuadre |
| Detección / pintura | D GraffitiRaycastUI 9 / 3 u; Pintando 3 | S/E detección 9, ray paint 6, free paint 6 u; wallLayer=8 | Distancias de consulta/deposición; comodidad y soporte de la obra aún pendientes |
| Máscaras | TagManager | Parkour 264 = Wall(3)+Parkour(8); movimiento 265 añade Default(0); pintura 8 = Wall(3) | No confundir nombre de layer con su índice o máscara |

### Cálculos analíticos, no alcances probados

Con altura 2.02 y gravedad 18, el impulso ideal es 8.5276 u/s, ápice a 0.4738 s y vuelo a misma altura de 0.9475 s. A velocidad horizontal constante, sin colisión/aceleración, daría 2.8425 u caminando o 5.6851 u en sprint. **No son huecos recomendados ni máximos fiables:** integración por frame, aproximación, aire, cápsula, recepción y geometría cambian el resultado.

FPSController deshabilita colliders durante ciertas trayectorias de vault/top-out/wall jump y valida destino; revisar el espacio recorrido y obstáculos intermedios sigue siendo necesario. TeleportTo restaura colliders si interrumpe parkour y reinicia estados. Eso acredita una API de recuperación, no un sistema de checkpoints aprobado; la búsqueda de scripts no encontró uno dedicado.

## 3. Espacio real de Parkour_Test_Blockout

Raíz activa en (84.4,0,-113.7), 48 hijos directos; 44 con collider sólido activo en las consultas. La línea central está aproximadamente en X=114.4 y progresa hacia Z creciente. La numeración no acredita una secuencia transitable.

| Recurso existente | Centro mundial aproximado | Tamaño AABB X×Y×Z | Lectura espacial |
| --- | --- | --- | --- |
| 00_Test_Floor | (113.099,-0.2,-65.244) | 68.07×0.5×155.741 | Suelo superior Y≈0.05, cubre Z≈-143.115 a 12.627; no checkpoint |
| 00_Vault_Low_065 | (114.4,0.47,-117.44) | 6×0.65×0.65 | Parte superior Y≈0.795; desnivel respecto al suelo ≈0.745, no 0.65 |
| 01_Vault_Medium_085 | (114.4,0.425,-110.79) | 6×0.85×0.7 | Segundo obstáculo candidato; perfiles dependen de pies actuales |
| 02_Vault_High_110 | (114.4,0.55,-103.48) | 6×1.1×0.8 | Recepción/entrada por comprobar; puede rodearse espacialmente por suelo |
| 04_Mantle_High_250 | (114.4,2.25,-94.7) | 6×4.5×3.2 | Supera umbral contextual 2.6; no presentarlo como mantle directo de 2.5 |
| 05_Climb_Wall_600 / (1) | (114.4,6.602,-84.7) / (114.4,6.71,-89.88) | ≈10×13.2×0.8 cada una | Separación entre caras Z≈4.38; requieren escalada/lectura más extensa de lo que sugiere el nombre |
| 05_TopOut_Deck | (114.4,12.97,-79.32) | 12×0.5×10.4 | Superficie superior Y≈13.22; acceso y salida no certificados |
| 06_Descent_Step_400 / 200 | Z=-70.75 / -66.75 | 7×8.484×3 / 7×2×3 | La primera no mide 4 de alto; pendiente de recuperación controlada |
| 07_SeamWall_Left/Center/Right | X=109.9/114.4/118.9; Z=-58.7 | 4.5×9.764×0.8 cada una | Frente conjunto de 13.5 de ancho; marcadores permiten desplazamiento lateral |
| 07_Lateral_TopOut_Deck | (114.4,9.275,-54.5) | 15×0.976×7.6 | Y superior≈9.763; descanso candidato, no zona segura acreditada |
| 09_Control_NoSide | (114.4,4.934,-41.7) | 5×9.867×0.8 | allowSideways=false; no utilizar como tramo lateral obligatorio |
| 11_WallJump_Left/Right | X=111.9/116.9; Y=8.871; Z=-22.7 | 0.5×17.741×8 | Hueco entre caras X=4.5; suelo adicional bajo el sector |
| 12_WallJump_ExitDeck | (114.4,17.57,-15.2) | 8×0.5×7 | Y superior≈17.82 |
| 13_Fall_Landing_550 | (114.094,2.13,-5.7) | 13.388×4.323×12 | Y superior≈4.292; caída desde deck≈13.529, no 5.5 |
| Cube (20)…(39) laterales | X≈81–82 y 145.8–146.9 | ≈1.276×10.254×11.629 | Límites/masas laterales escalables; huecos, accesos y cámaras no probados |
| concrete_wall_toppe (1) | Collider centro (114.611,5.404,-141.012) | 30.116×12.372×1.878 | Pared posterior candidata para pintura; su pivot está lejos del collider |

**Hallazgo de criterio:** el Low_065 elevado puede resolver un perfil distinto al esperado por su nombre; Mantle_250 y Fall_550 tampoco describen las dimensiones actuales. Cambiar nombres o geometría no está autorizado aquí. Registrar discrepancias evita diseñar progresión sobre categorías falsas.

### Comprobaciones estáticas y zonas existentes

- Un spherecast hipotético desde el lado izquierdo del sector wall jump, usando offset efectivo 0.78652, altura Y=5, radio 0.2 y alcance 4.25, detectó `11_WallJump_Right` a ≈3.46348 u. El hueco de 4.5 no puede compararse directamente con la longitud del cast. Trayectoria completa, clearance y recepción siguen pendientes.
- Ray de pintura hipotético desde (114.4,1.8,-138), hacia -Z: hit en collider hijo `01` de la pared posterior, distancia ≈2.07247, normal +Z. Desde (114.4,1.8,-97.5) hacia +Z: hit en Mantle_High_250 a ≈1.2. Desde (114.4,1.8,-108) hacia +Z: sin hit a esa altura. **No son pruebas de posición del cuerpo, composición o soporte de una pieza grande.**
- Scenario_FS (1) está activo y reúne 409 colliders sólidos activos; su AABB agregado ≈710.074×82.939×518.762 no equivale a área jugable conectada. Envioriment, CAMPO DE GRAFFITI, Blocking_HQ y Sewer_TP_Network están inactivos en la lectura. Los grupos GRAFFITIS activos no aportaron colliders sólidos en esta consulta.
- Cámara: Gameplay conserva solución propia en FPSController. FSSRS/Industrial Dusk son la referencia visual existente; no se hizo un recorrido visual jugable, escucha, medición de rendimiento o prueba con participantes en esta fase.

## 4. Tres diseños alternativos — PROPUESTAS

Son ejercicios sobre subconjuntos existentes. Toda geometría, checkpoint, señalización, regla de finalización o recompensa nueva se propone para una futura escena de prueba autorizada. No se reemplaza el circuito y no se adopta un bucle previo como decisión del proyecto.

### A. Patio de lectura y firma

- **Objetivo:** aprender a anticipar la acción contextual y elegir cómo atravesar un patio, con una pausa de expresión al regresar; sin reloj ni premio nuevo obligatorio.
- **Mecánicas:** caminar/sprint, salto/vault contextual, rodear obstáculos, alternar a pintura libre/rápida. Escalada de 4.5 queda como exploración posterior opcional.
- **Secuencia:** entrada próxima al Player → vista de los tres vaults → elección de línea central o suelo lateral → descanso antes de Mantle_High_250 → retorno por otro lado al muro posterior. Es un layout candidato; accesos y giros requieren prueba.
- **Dificultad:** empezar con un solo obstáculo correctamente clasificado, luego variar altura y aproximación. Low_065 no se usa como dificultad baja aprobada hasta comprobar su resolución real; no encadenar obligatoriamente el muro de 4.5 tras los vaults.
- **Rutas alternativas:** principal de vaults; secundaria por suelo con destino compartido; secreto candidato en la cara posterior del bloque alto, con indicio visible y retorno por suelo, sin habilidad nueva.
- **Interés y graffiti:** primer panorama del patio, elección de aproximación y muro posterior. Su ray de pintura tuvo hit estático; hay que verificar postura, encuadre, luz y soporte de esquinas antes de habilitar una obra.
- **Recuperación:** suelo lateral como candidato de reagrupación, sin prometer checkpoint. Reinicio manual moderado para el primer ensayo; un punto de retorno formal sería una propuesta separada.
- **Riesgos:** acción distinta a la sugerida por nombre, interferencia del boom al rodear y ambigüedad entre detectar y pintar. La orientación hacia un muro lejano puede no generar prompt inicial.
- **Playtest:** registrar acción que anticipa el jugador y la que ocurre, ruta elegida, ayudas, fallos, retorno y primera entrada a pintar. Aceptación candidata: identificar destino/retorno y completar una aproximación sin F7/F8; cero atrapamientos y obra conservada al cancelar. Criterios se ratifican antes del ensayo.

### B. Galería de costuras y miradores

- **Objetivo:** convertir desplazamiento lateral y cambio de perspectiva en decisiones espaciales, con altura opcional y un destino compartido a nivel de suelo.
- **Mecánicas:** escalada lateral en los tres SeamWall, top-out, caminar y pintura desde una plataforma estable. No incluye pintura mientras se cuelga.
- **Secuencia:** llegar a la banda Z≈-58.7 por suelo → leer tres paños y una vista lateral → practicar ascenso en un paño → cruzar una junta → elegir continuar o salir al deck → observar el conjunto → volver a zona de trabajo. La conectividad de junta/top-out está pendiente.
- **Dificultad:** primer contacto aislado y recuperación próxima; después junta simple; finalmente combinación de junta y salida. No extenderlo a NoSide como si permitiera lateralidad.
- **Rutas alternativas:** principal de ascenso y un cruce; secundaria por suelo para observar y crear sin altura; secreto candidato detrás de un extremo del paño, con pista de silueta y retorno legible. El acceso alto requiere prueba; no se inventa ascensor o capacidad de vuelo.
- **Interés y graffiti:** mirador del deck y lectura de los tres paños. Paneles de graffiti en caras/alturas alcanzables desde apoyos verificados; cualquier nueva plataforma de trabajo se construiría solo en prueba autorizada. Un mural conjunto y su persistencia son decisiones de Game Design aún abiertas.
- **Recuperación:** medir la caída de ≈9.7 al suelo antes de llamarla segura. Proponer repisas intermedias de recepción en el duplicado de prueba, con medidas por validar; checkpoint opcional solo tras definir restauración y conservación de obra.
- **Riesgos:** discontinuidad de colisión/normal en juntas, cámara contra pared, encuadre al salir, orientación desde altura y coste de volver para retocar. La recepción añade geometría, no se supone existente.
- **Playtest:** contar pérdidas de contacto y causas, falsas expectativas de lateralidad, elección voluntaria de altura, desorientación, compresión de cámara y coste de retorno. Comparar cruce con una vista equivalente desde suelo para saber si la verticalidad aporta algo.

### C. Bifurcación entre paredes

- **Objetivo:** ofrecer un reto vertical optativo de transferencia entre paredes, claramente separado de una conexión principal comprensible por suelo.
- **Mecánicas:** escalada, wall jump, steering y top-out; descenso/retorno deben resolverse en el greybox, no darse por seguros.
- **Secuencia:** llegada a SafetyFloor Z≈-22.7 → lectura de paredes y salida → elección entre paso por suelo o primer intercambio a baja altura → refugio candidato → cadena vertical opcional → ExitDeck → regreso en tramos. El cast estático demuestra detección posible, no la cadena.
- **Dificultad:** un intercambio bajo antes de subir; después variar aproximación y salida. El itinerario alto queda fuera de la ruta obligatoria hasta verificar cuerpo/cámara y recuperación.
- **Rutas alternativas:** principal por suelo y observación del reto; secundaria vertical de dominio; secreto candidato tras un retorno lateral del deck, siempre con recepción/retorno construidos y validados en prueba. Ninguna conexión al deck por suelo se presume existente.
- **Interés y graffiti:** vista del corredor desde altura, oportunidad de comparar rutas y panel al nivel de suelo para quienes eviten altura. Un panel en ExitDeck necesita soporte/encuadre nuevos o verificados; no se promete una pared pintable a altura de ojos solo porque exista el deck.
- **Recuperación:** conservar SafetyFloor, pero no llamarlo respawn. La caída actual de ExitDeck al bloque siguiente ≈13.53 no será salida obligatoria propuesta. Estudiar descenso fraccionado y punto de retorno en el duplicado autorizado, con política de obra/recursos definida.
- **Riesgos:** interpretar 4.25 como gap máximo, geometría atravesada durante interpolación, colliders restaurados tras interrupción, techo/cámara en salida y reinicio que pierda progreso. Elevar paredes no añade por sí mismo profundidad.
- **Playtest:** confirmar cada intercambio por ambos sentidos y aproximaciones; registrar fallos de detección frente a destino/clearance, tiempo de reintento, comprensión del bypass y voluntad de repetir. Cero reinicios que dejen colliders/input en estado incorrecto; ninguna caída sin recuperación comprobada en la ruta que se proponga como principal.

## 5. Criterio profesional y siguiente decisión

| Alternativa | Experiencia principal | Dependencias nuevas | Complejidad relativa |
| --- | --- | --- | --- |
| Patio | Lectura contextual, elección y transición a creación | Orientación y validación de zonas de trabajo; no exige nueva locomoción | Menor, conservando circuito como referencia |
| Galería | Exploración lateral y perspectiva | Recepciones/retorno, posible trabajo en altura y continuidad de obra | Media-alta |
| Bifurcación | Dominio vertical optativo | Refugios, descenso seguro, recuperación y QA de trayectorias | Alta |

**Recomendación pendiente:** comenzar por un ensayo de lectura del Patio, después de verificar el perfil real de cada vault. No compromete el bucle de producto, no exige nueva economía y evita asumir seguros los grandes desniveles. Galería y Bifurcación exploran otras experiencias y no son una progresión aprobada.

Antes de integrar cualquier variante: objetivo → capacidades → métricas fiables → layout → greybox autorizado → movimiento/cámara → dificultad/lectura → iteración con playtest → propuesta de arte/luz → validación y aprobación de esa versión. La existencia del circuito o de esta documentación no concede esa aprobación.
