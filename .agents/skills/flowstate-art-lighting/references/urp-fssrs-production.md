# Producción URP y FSSRS

## Propietarios

Punto de partida fechado en el [estudio](../../../../Docs/Design/ArtLighting/Rendering-Architecture-and-Visual-Review.md). Confirma versiones antes de cambiar APIs.

- `Assets/Settings/PC_RPAsset.asset`: calidad y renderers. `FSSRS_PC_Renderer.asset`, bajo `Assets/07_StylizedRendering/FSSRS/Renderer/`, es seleccionado por MainCamera. No presupongas paridad móvil.
- `FSSRS/RendererFeatures/FSSRSRendererFeature.cs`: entradas Color/Depth/Normal, textura intermedia y blit Render Graph antes de transparencias. `Shaders/Hidden/FSSRSComposite.shader` combina bordes e impresión. Verifica orden efectivo con Frame Debugger en sesión autorizada.
- `Shaders/FlowStylizedLit.shader` e `Includes/FSSRSLighting.hlsl`: cuantización, GI, normal, fog, decals y bucles Forward+. No es automáticamente el modelo PBR de URP Lit; un Reflection Probe no garantiza reflejo visible en este shader.
- `FlowStatePaletteController`, paletas/presets y `FSSRSVolumeComponent`: emoción/globales y mezcla por volumen. Comprueba quién sobrescribe un ajuste al iniciar/cambiar estado.
- `PlayerComicBorder` y `PlayerComicPlate.shader`: placas de papel/emoción/tinta/eco. Conserva su firma; mide transparencia/overdraw antes de reducirlas.
- `Assets/ArtDirection/IndustrialDusk/Gameplay_IndustrialDusk.asset`: perfil activo observado. Ajustar perfiles inactivos no prueba un cambio en Gameplay.

El documento histórico FSSRS menciona Game.unity, aislamiento exclusivo de LookDev y cinco emociones. Gameplay observado usa renderer 1; el controlador actual tiene cuatro emociones. También documenta flecks/halftone globales desactivados deliberadamente. Conserva discrepancias como historial, sin convertirlas en instrucciones artísticas nuevas.

## Materiales, shaders y luz

Empieza por shader/passes/keywords/texturas del renderer afectado, escala/UV/normales, importación/color space y valores serializados. Distingue material compartido, instancia y generación runtime. Lee sharedMaterials sin instanciar materiales en auditorías.

En cambios autorizados duplica solo lo necesario para LookDev, conservando GUID/referencias de producción. Ajusta bandas, mezcla de luz, rim, breakup, emisión y roughness según el shader que los use. Shader Graph disponible no justifica migrar HLSL existente. Verifica tags URP, ForwardOnly/Deferred, DepthNormals/ShadowCaster, fog, instancing/SRP Batcher, DBuffer, transparencias y variantes/plataformas pertinentes. Compilación no prueba apariencia/coste.

Diseña principal, relleno y acentos por función. Revisa sombras desde cámara: cobertura, resolución, distancia, bias, contacto y cascadas. Considera Light Probes/APV o Reflection Probes tras comprobar soporte de shader, movimiento, espacio, bake y presupuesto; ausencia de bake no es un defecto por sí sola.

En bloom/exposición/grading/niebla traza datos HDR, umbral y orden. Saturación del Volume y del composite no son equivalentes. Examina máscara/prioridad/peso/override/estado activo y sharedProfile sin instanciar perfiles. Conserva decisiones sobre efectos de cámara y comodidad visual.

## Render Graph y herramientas

Usa código del paquete URP instalado como fuente primaria; Context7/documentación oficial con consultas genéricas si falta información. En Render Graph comprueba API, validez de recursos, lectura/escritura, backbuffer, formatos/MSAA, vida de handles y actualización del color de cámara. Un blit puede usar `_BlitTexture` del include URP; no inventes una dependencia `_MainTex`.

Prioriza MCP existente: instancia/proyecto/estado antes de acciones; consultas pequeñas y explícitas evitando getters con efectos. CLI oficial complementa comandos repetibles: descubre ayuda/status y fija project-path. No instales ni actualices Pipeline por desconexiones. Consulta las skills oficiales pertinentes para postprocesado/Render Graph; no todo el catálogo. Computer Use permite inspección local del Editor; comprueba visualmente capturas MCP.

Solo con autorización usa escrituras MCP/CLI/Editor para materiales, luces, perfiles o shaders. Respeta reversión y archivos permitidos; verifica compilación/consola sin borrar, referencias y resultado. Una herramienta conectada no acredita todas sus operaciones de escritura.
