# FLOWSTATE — VFX y feedback de gameplay

Fecha: 2026-10-28. Auditoría de archivos y consultas explícitas de Editor en memoria, sin Play Mode, reproducción/simulación de efectos ni cambios del juego. Unity 6000.0.41f1, URP 17.0.4, Shader Graph y VFX Graph 17.0.4 confirmados en Packages. El [snapshot](Editor-VFX-Snapshot-2026-10-28.json) separa lectura de edición y constantes del código; no contiene mediciones de rendimiento ni observación del aerosol runtime.

## Identidad y autoridad

FSSRS documenta impresión industrial incompleta, papel/tinta, placas emocionales cian/magenta/amarillo/coral y graffiti como capa autoral. Industrial Dusk mantiene la base atmosférica. Conservar GraffitiSprayVFX, control de pintura y jerarquía de obra/ruta/HUD. El efecto debe ayudar a entender una acción; más partículas, flashes o shake no equivalen a más satisfacción.

IMPLEMENTADO describe código/recurso; LECTURA EN EDICIÓN, estado vivo sin inicialización jugable; OBSERVACIÓN HISTÓRICA, imagen guardada; HIPÓTESIS, interpretación pendiente; PROPUESTA, cambio sin aprobación. Creative Direction decide identidad; Game Design define éxito/recompensa; Animation contactos/timing; Art & Lighting integración; UI/UX información/accesibilidad. Este informe no ratifica bucles o efectos propuestos anteriormente.

## Auditoría comprobada

### Aerosol y pintura son sistemas diferentes

[GraffitiSprayVFX.cs](../../../Assets/01_Scripts/GraffitiSprayVFX.cs) presenta la nube visual; [GraffitiPainter.cs](../../../Assets/01_Scripts/GraffitiPainter.cs), [Pintando.cs](../../../Assets/01_Scripts/Pintando.cs) y las piezas rápidas gobiernan depósito/progreso. No se atribuye tinta real a colisión de partículas.

`EnsureRuntimeSystems` crea recursos/HUD/VFX si faltan. `Bind` reutiliza la ParticleSystem primaria o crea una, separa el root del padre, neutraliza su escala, añade tres capas y configura cuatro materiales runtime. Las capas son **ParticleSystem con renderer Billboard**, no cuatro meshes reconstruidos por fotograma.

| Capa | maxParticles de código | Emisión base/segundo | Función |
|---|---:|---:|---|
| Pigment | 220 | 420 | Núcleo de tinta |
| Air Cloud | 110 | 115 | Nube de aire suave |
| Ink Microdroplets | 60 | 36 | Microgotas/ruptura |
| Valve Comic Pop | 12 | 0 | Burst y pulsos de válvula |

Capacidad total configurada **402**, no ocupación real ni presupuesto aprobado. La tasa dinámica depende de presión/boquilla; estos valores no prueban bajo coste. Burst inicial: pigmento 20, aire 8, microgotas 4 —7 para Splatter— y válvula 2. Los pulsos de válvula usan intervalos .095–.068 s según presión. Se preserva `pendingStartBurst` para esperar un perfil válido.

Profila por boquilla: Needle/FatCap/Chisel/Splatter varían apertura, tamaño, densidad y aspecto. UpdatePresentation usa rayo/orientación de boquilla, distancia, cono, color y presión; los cambios de perfil tienen umbrales para evitar reconfigurar por diferencias mínimas. El modo rápido usa su gesto/rayo, cono .18 y presentación SoftRound; no inferir que toda boquilla del modo libre se usa allí.

**LECTURA EN EDICIÓN:** Gameplay sin dirty, fuera de Play Mode. Tres ParticleSystems explícitas: dos Dust de Industrial Dusk activas como GameObjects, pero no reproduciéndose; capacidad 80 cada una, rate 3, lifetime 18 s. La primaria heredada `Particle System` está inactiva, escala .22218, capacidad 400 y lifetime 64.8 s; GraffitiPainter la referencia. No existen aún componentes GraffitiSprayVFX/SprayResourceSystem en la escena cargada. `Bind` reconfigura la primaria al iniciar: sus valores heredados no prueban cómo se ve el aerosol final.

El valor de edición useDecalMode=true tampoco demuestra modo final: Start puede ponerlo a false si tabCyclesNozzles está activo. No se llamó Start/Bind para «completar» la auditoría.

### Emisión, recursos y ciclo de vida

[SprayResourceSystem.cs](../../../Assets/01_Scripts/SprayResourceSystem.cs) controla pintura/mezcla, agitado, cambio y dropout por CanEmitAt. GraffitiPainter transmite el permiso mediante SetParticleEmission; Pintando exige CanEmitPaint para depositar muestras. Pulsación, isPainting, permiso de flujo y depósito efectivo son evidencias diferentes.

SetEmission reacciona a cambios false→true y dispara burst cuando el perfil está listo. Una recuperación de dropout puede atravesar ese mismo camino que una nueva pulsación. **Hecho de lógica**, sin observación de frecuencia o calidad perceptual. Desactivar emisión deja cola; SetVisible(false) la detiene y limpia. Los materiales se destruyen al destruir el componente; el root se destruye si es propio. Reutilización de cuatro sistemas no equivale a un pool general ni demuestra ausencia de fugas en todos los ciclos de escena/owner.

La lifetime se aproxima a distancia/velocidad con clearance .035 y clamp .045–.46 s. No es garantía de no atravesar un muro: límites mínimos, ruido, velocidad y gravedad alteran recorrido. No se observó penetración. La fuente no configura colisión como garantía de impacto visual.

Hay métodos de audio opcional, movimiento de lata y estados HUD para agitado, seco/cambio y mezcla; se coordinarán con sus propietarios. No se escuchó audio ni se observó sincronización temporal. No se encontró un consumo de FS.Menu.Motion en GraffitiSprayVFX o su shader.

### Shaders y orden de rendering

[GraffitiCartoonAerosol.shader](<../../../Assets/04_Materiales e Imagenes/GraffitiCartoonAerosol.shader>) declara `FLOWSTATE/Graffiti/Cartoon Aerosol Particle`, URP, SRPDefaultUnlit y Transparent+25. Usa Blend One OneMinusSrcAlpha, ZWrite Off, ZTest LEqual, Cull Off y fog; devuelve color premultiplicado. Combina tinta/papel/placas FSSRS, fragmentos/registro cromático y evolución escalonada con _Time. Su energía visual puede combinar presión y emoción: no es un medidor literal de recurso ni modifica por sí sola tinta depositada.

Shader.Find encontró el shader y isSupported=true en este Editor. No demuestra inclusión/compatibilidad en un Player. CreateMaterials registra error si falta; no se ha probado un build ni se alteró GraphicsSettings para retener variantes.

MainCamera usa renderer índice 1 en la lectura actual. FSSRS compone antes de transparencias; aerosol tiene acabado propio y recibe post final, sin asumir todos los outlines del composite. El [manual FSSRS](../../../Assets/07_StylizedRendering/FSSRS/Documentation/FLOW_STATE_STYLIZED_RENDERING.md) conserva referencias históricas de escenas/emociones; el estudio Art & Lighting documenta discrepancias. Flecks/halftone globales están desactivados deliberadamente; el shader de aerosol tiene controles locales distintos. No se «restauran» filtros globales por llamar comic a un efecto.

Otros recursos reales: GraffitiSprayStamp.shader para depósito por capas, QuickGraffitiReveal.shader/.mat para revelado por pasadas, shaders del HUD, Dust_Cold.mat para atmósfera y sistemas de spray propios del menú/trailer. No son intercambiables automáticamente.

VFX Graph **17.0.4 está instalado/declarado**, pero no se encontraron `.vfx` en Assets, VisualEffectAsset por AssetDatabase ni VisualEffect en Gameplay. No se propone migración de ParticleSystem por ese dato.

### Movimiento, impactos y finalización

| Acción | Feedback implementado/encontrado | Límite |
|---|---|---|
| Salto/vault/escalada/wall jump | Estados/clips, trayectoria, cámara/FOV de FPSController | No se encontró emisor dedicado de impacto/velocidad ligado a esas acciones en los propietarios revisados |
| Aterrizaje | Falling/Land y recuperación; Land puede dispararse antes del contacto | Trigger Land no es evento físico de impacto |
| Spray/boquilla | Cuatro capas/perfiles, cursor de pared y preview de boquilla, depósito/chorreos | Apariencia, lectura y relación temporal no comprobadas ahora |
| Agitado/seco/cambio | Recurso, lata, avisos HUD y rutas de audio | Sin reproducción ni cobertura global de movimiento reducido demostrada |
| Obra rápida completa | Progress 1, revelado completo, StopStroke/StopPainting y CityImpact +5 | No se encontró un efecto de celebración específico en la ruta revisada |
| Ruta legacy GraffitiFade | AddPaint/IsComplete, +5 y StopPainting | No debe confundirse con pintura libre ni modo final inicializado |
| Pintura libre | Stamps, continuidad y drips | No se identificó semántica de «obra finalizada» para celebrar automáticamente |

[GraffitiQuickPainter.Advance](../../../Assets/01_Scripts/GraffitiQuickPainter.cs) devuelve true al completar y desactiva StrokeActive; [QuickGraffitiPiece](../../../Assets/01_Scripts/QuickGraffitiPiece.cs) conserva obra/progreso. Soltar pausa la pieza, sin certificarla como terminada.

[ReputationSystem.OnReputationChanged](../../../Assets/01_Scripts/Reputation/ReputationSystem.cs) también notifica Load/Reset y cambios de otros ejes. Al llegar al máximo, ChangeValues puede no notificar un delta nulo aunque se complete otra obra. No sirve como único evento de celebración ni debe otorgarse otra vez reputación desde VFX.

## Evidencia visual y límites

Se inspeccionaron las imágenes guardadas `Assets/Screenshots/spray_can_outline_painting.png` y `spray_hud_redesign_full.png`: muestran lata/HUD y una presentación anterior del entorno/personaje. El nombre «painting» no acredita nube visible o permiso de emisión en ese frame; no son prueba del aerosol actual ni comparación A/B. No se convierten sus colores, aberración o intensidad histórica en dirección vigente.

Las lecturas vivas son de edición; no se reprodujo spray, caída o finalización. No se afirma «mejor», «más satisfactorio», «barato» o «accesible» a partir de estas imágenes/datos. Faltan captura temporal runtime, input/datos controlados, pruebas de comprensión y profiling en hardware objetivo.

## Tres propuestas — no implementadas ni aprobadas

### 1. Diferenciar ataque nuevo y recuperación de dropout en el aerosol

**Estado comprobado:** cuatro capas/perfiles y burst al reactivar SetEmission; dropout de recurso puede apagar/encender flujo. **Objetivo perceptual:** respuesta inmediata al pulsar y continuidad comprensible cuando la mezcla falla, conservando precisión del trazo.

**Propuesta visual:** comparar el ataque completo actual con una recuperación contenida del flujo tras dropout, reservando el burst fuerte para inicio real de válvula. Conservar tinta/aire/microgotas, presión y boquillas; no aumentar humo ni modificar el depósito. **Hipótesis:** evitar repetir ataque completo en toda recuperación podría reducir ruido de lectura; necesita observarse y podría no aportar mejora.

**Reutilización:** GraffitiSprayVFX, permiso de SprayResourceSystem, datos de ray/pressure de GraffitiPainter y shader actual. **Implementación futura:** una distinción semántica mínima entre pulsación y cambio de flujo, con tests del contrato, sin otro sistema de aerosol ni Graph.

**Riesgos:** suprimir un feedback útil, perder respuesta tras agitado/cambio, duplicar estado, pulso demasiado frecuente, overdraw de capas o desacoplar nube de tinta real. **Aceptación/validación:** A/B con mismas boquilla/distancia/color/recursos, nueva pulsación, continuo, mezcla baja/dropout y seco; registrar bursts/entrada/depósito, cola y lectura del cursor/obra. Inicio sigue respondiendo; no hay nube que prometa tinta prohibida; pausa/salida/reentrada limpian según contrato. Vídeo normal/cuadro a cuadro, variantes reducidas y profiling comparable; sin promesa de ahorro por cambiar un burst.

### 2. Acento local de recepción basado en contacto físico

**Estado comprobado:** hay animación/cámara de caída/aterrizaje, pero no un emisor de impacto dedicado en las rutas revisadas; Land puede anticiparse. **Objetivo perceptual:** distinguir recepción con peso sin ocultar el próximo apoyo ni cambiar recuperación.

**Propuesta visual:** un acento breve y pequeño de polvo/tinta cerca del apoyo, con intensidad graduada por datos de caída verificados. Compararlo con la presentación actual antes de añadir shake/FOV; mantener cámara y física. **Reutilización:** ParticleSystem, vocabulario de polvo/papel y paleta del entorno, datos de contacto de FPSController y coordinación con Animation. No reutilizar la instancia ambiental Dust como one-shot ni alterar su material compartido.

**Implementación futura:** evento/punto de integración mínimo en transición física válida, excluyendo arranque, teletransporte, ajuste al suelo y repeticiones; pool acotado solo si hay reutilización real. No activar desde Land predictivo o Grounded cada frame. **Riesgos:** impactos falsos, bursts duplicados, saturar suelo/cámara, coste de transparencia, cambios de tono según superficie y movimiento innecesario.

**Aceptación/validación:** exactamente un acento por recepción elegible, ninguno en caminar/arranque/ajuste; contacto coincide con el efecto y no cambia respuesta/cámara. Pruebas de caída, salto pequeño, vault, top-out, pausa y cancelación; capturas/clips con misma ruta. Revisar reconocimiento y comodidad con jugadores, solapamiento/overdraw y costes CPU/GPU/GC. Umbrales/intensidades pendientes de acuerdo; no se deduce que todo salto necesite partículas.

### 3. Cierre visual localizado al completar una pieza rápida

**Estado comprobado:** revelado completo, corte de spray y premio +5 ya existen; no se identificó celebración específica. Pintura libre no tiene finalización definida. **Objetivo perceptual:** reconocer finalización y autoría sin tapar la obra ni convertirla en un juicio de belleza.

**Propuesta visual:** una breve firma de tinta o asentamiento de papel localizado en el borde de la pieza, acompañado de confirmación textual discreta solo si UI/UX la considera necesaria. Mantener la imagen terminada visible y su color; evitar flash fullscreen, nueva paleta o cambio de emoción global.

**Reutilización:** transición de Progress/IsComplete y retorno true de Advance, transform/superficie de la pieza, shader/material/vocabulario de impresión y UI existente si procede. **Implementación futura:** señal semántica de esa pieza con deduplicación, consumidor de VFX separado de quien otorga reputación. No disparar por OnReputationChanged, soltar clic o agotar lata; no volver a añadir +5. Para pintura libre haría falta una decisión de diseño adicional, fuera de esta propuesta acotada.

**Riesgos:** celebración doble, perderla con reputación al tope, más materiales/overdraw persistentes, parpadeo o feedback que parezca recompensa nueva. **Aceptación/validación:** una vez por pieza terminada, nunca al pausar una parcial; sigue funcionando con reputación máxima, Load/Reset no celebran y la obra permanece intacta. Repetición, cancelación, cambio de escena y presupuesto de one-shots; clips A/B para lectura de finalización y prueba con usuarios sin explicarles la señal. Coste medido y variante reducida antes de integrar.

## Herramientas y capacidades

MCP existente y execute_code leyeron datos explícitos sin construir/reproducir efectos. manage_vfx expone consultas y operaciones ParticleSystem/VFX Graph/Line/Trail en su implementación local; sus escrituras no se probaron. CLI/Computer Use son complementos para futuras pruebas autorizadas, no instalaciones nuevas. Profiler está expuesto entre custom tools, sin captura iniciada.

La skill añade producción de efectos compatibles, eventos/ownership, timing/intensidad, materiales, pooling/lifecycle, accesibilidad, A/B y planificación de medición. Se apoya en las seis disciplinas existentes sin redefinir reglas o rendering. Quedan pendientes Play Mode, variantes/Player, impresión temporal real, rendimiento y playtesting. Las suites GraffitiInteractionTests/QuickGraffitiTests contienen pruebas pertinentes, identificadas sin ejecutarlas ni ratificar resultados históricos.
