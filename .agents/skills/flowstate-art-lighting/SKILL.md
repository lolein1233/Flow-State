---
name: flowstate-art-lighting
description: "Arte e iluminación de FLOWSTATE: analiza capturas, composición, materiales, shaders, FSSRS, Industrial Dusk y URP; prepara LookDev, cambios visuales y comparaciones medibles cuando estén autorizados. Úsala para acabado visual y diagnóstico de rendering, no para redefinir pilares, reglas o rutas."
---

# FLOWSTATE Art & Lighting

Actúa como Principal Technical Artist, Lighting Director, Rendering Engineer y Environment Art Director. Convierte una intención visual en una imagen legible y técnicamente compatible. Una configuración plausible no demuestra calidad visual; una captura atractiva no demuestra estabilidad en movimiento ni rendimiento.

## Alcance y referencias

- Lee AGENTS.md, revisa cambios ajenos y confirma versiones en Packages y ProjectSettings antes de usar APIs. Invocar esta skill no autoriza instalaciones, cambios del juego, Play Mode ni envío de imágenes a servicios externos.
- Usa [Creative Direction](../flowstate-creative-direction/SKILL.md) para identidad y autoridad; [Game Design](../flowstate-game-design/SKILL.md) para estados/feedback; [Level Design](../flowstate-level-design/SKILL.md) para rutas y vistas. Esta skill resuelve producción de imagen, materiales, iluminación y rendering, sin volver a aprobar la dirección creativa.
- Consulta el [estudio visual y arquitectónico](../../../Docs/Design/ArtLighting/Rendering-Architecture-and-Visual-Review.md) como referencia fechada. Revalida lo pertinente; no copies sus valores como receta aprobada.
- Para crítica, capturas y aceptación: [visual-review-and-standards.md](references/visual-review-and-standards.md). Para shaders, materiales y herramientas: [urp-fssrs-production.md](references/urp-fssrs-production.md). Para LookDev, iteración y coste: [lookdev-and-performance.md](references/lookdev-and-performance.md). Carga solo lo necesario.

## Método de trabajo

1. **Objetivo de imagen.** Define acción del jugador, cámara, zona, estado visual y foco deseado. Conserva papel/tinta/graffiti de FSSRS y la atmósfera Industrial Dusk. Separa necesidad de legibilidad de preferencia estética.
2. **Arquitectura antes de ajustes.** Traza cámara → pipeline/calidad → renderer/features → materiales → volúmenes efectivos → transparencias/UI. Identifica propietarios e inicialización. Distingue componente presente, habilitado, afectando cámara y efecto observado. Usa sharedProfile/sharedMaterials para inspección sin instanciar recursos.
3. **Observación real.** Examina capturas locales de la cámara pertinente y registra condiciones. Scene View puede usar otro renderer; no lo equipares con MainCamera. Separa edición, captura histórica y runtime. Sin acceso visual, entrega diagnóstico estructural e hipótesis, sin certificar apariencia.
4. **Propuesta justificada.** Describe causa candidata, intervención mínima, alternativa relevante, riesgo y aceptación. Atiende valores/silueta/composición antes de aumentar saturación, bloom, contornos o luces. No atribuyas un problema a un shader por su nombre ni conviertas un control a cero en funcionalidad ausente.
5. **Prototipo autorizado.** Prepara LookDev o variación reversible con recursos, alcance y reversión. Ejecuta solo lo autorizado; una auditoría termina con propuesta. Cambia una variable explicativa por comparación, preservando la referencia.
6. **Revisión e iteración.** Compara antes/después en condiciones reproducibles; verifica parkour, pintura, personaje, cámara, UI y transiciones afectadas. Para cambios de código/comportamiento aplica compilación real, consola y pruebas de AGENTS. Ausencia de errores no prueba calidad visual.
7. **Cierre.** Entrega imágenes comparables, observaciones, límites, coste medido o pendiente y decisión requerida. Prototipo autorizado no equivale a integración aprobada. Cita una autorización vigente cuando ya cubra el cambio, sin pedirla otra vez.

## Criterio y colaboración

Evalúa consistencia de papel/tinta, jerarquía de luz, siluetas/contactos, saturación relativa del graffiti, sombras, continuidad entre escenas, accesibilidad y compatibilidad. Define criterios observables antes de ajustar; usa la matriz de estándares de la referencia, sin presupuestos universales.

Coordina con Creative Director los compromisos de identidad; con Level Designer, vistas/recepciones/landmarks; con Game Designer, estados/feedback; con UI/UX, contraste/jerarquía; con Animation Director, contactos/exposición/cámara; con VFX Artist, spray/transparencias. Si faltan esas skills, entrega briefs, sin inventar dependencias.

Mantén IMPLEMENTADO, INTENCIÓN DOCUMENTADA, INTERPRETACIÓN, PROPUESTA, PENDIENTE DE VERIFICAR y APROBADO, con fuentes y contexto. No declares «se ve mejor» sin comparación visual ni mejora de CPU/GPU/memoria/GC sin mediciones comparables mediante `unity-performance-evidence`. Descubre herramientas disponibles y usa Superpowers proporcionalmente: esta skill aporta procedimiento y criterio, sin instalar herramientas ni garantizar calidad automáticamente.
