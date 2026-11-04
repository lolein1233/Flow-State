# LookDev, iteración y rendimiento

## Prototipo reversible

Acota pregunta, escena/vistas, parámetros candidatos, recursos que cambiarían, aceptación y reversión. Reutiliza LookDev existente tras verificarlo o propone duplicado autorizado. No abras/reemplaces escenas, ejecutes setup de arte, hagas bake o actives Play Mode en una revisión de solo lectura.

Antes de editar registra git status, referencias compartidas, escena dirty y baseline visual. Una escena duplicada puede compartir materiales/perfiles: comprueba referencias antes de editar. Evita efectos temporales persistentes o getters que creen recursos durante inspección.

El fixture debe conservar los shaders y materiales originales de cada objeto, incluso cuando convivan URP Lit y FSSRS. No conviertas los obstáculos a FSSRS para una comparación de luz: introducirías una segunda causa. Una comparación de shaders requiere su propia variante y criterio.

Captura A dentro del propio fixture antes de B. Si simplificas fondo, geometría o contexto, limita las conclusiones a ese fixture: no atribuyas toda diferencia con una captura de producción al único parámetro de B.

Orden orientativo según causa: composición/valores → dirección/contacto de luz → respuesta de material → composite/post → detalle/VFX. La evidencia puede exigir otro orden. Mantén Industrial Dusk; no homogeneices zonas ni quites contraste por defecto.

Aplica propuesta → prototipo autorizado → captura A/B → recorrido/playtest autorizado → refinamiento → revisión de integración. Revierte solo recursos propios dentro del alcance; no uses reset/clean global. Presenta decisiones artísticas pendientes cuando excedan el encargo.

## Medición

Para profiling aplica `unity-performance-evidence`, sin duplicar su protocolo ni afirmar que ya se ejecutó. Acuerda hardware, build/calidad/resolución y presupuesto. Captura condiciones comparables, preferentemente Player, misma ruta/cámara/estado de pintura y calentamiento.

Investiga CPU main/render thread, GPU por pass, draw calls/batches/SetPass, overdraw/transparencias, sombras, memoria de texturas/RT/VRAM y GC según la pregunta. Frame Debugger atribuye trabajo, sin medir por sí solo tiempo GPU. Usa Profiler/Rendering Debugger y herramientas GPU disponibles; declara métricas no disponibles.

En FSSRS considera entradas Depth/Normals, SSAO/DBuffer, textura intermedia/composite, luces/sombras Forward+, placas del personaje, spray y reconstrucción dinámica de graffiti. Son candidatos, no cuellos de botella demostrados. 56 luces habilitadas no equivalen a 56 visibles ni shadow maps simultáneos.

Compara baselines repetidos y variación. Menos draw calls o FPS aislados no prueban una mejora. Una optimización conserva criterios visuales/legibilidad o documenta el compromiso. No presupongas paridad PC/móvil ni estabilidad temporal por un PNG.

## Entrega

Resume intervención/estado, recursos/dependencias, imágenes A/B y condiciones, aceptación, medición y pruebas no ejecutadas. Para integrar incluye aprobación que cubra esa versión y alcance; recomendación anterior no equivale a aprobación.

Briefs a otras especialidades: vista/acción, estado, evidencia, propuesta, restricción y criterio. Level Design valida navegación; Game Design, feedback; UI/UX, jerarquía conjunta; Animation, contactos/cámara; VFX, orden/HDR/transparencia; Creative Direction, identidad. No crees sistemas para resolver una pregunta local sin causa comprobada.
