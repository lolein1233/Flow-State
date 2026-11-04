---
name: flowstate-animation
description: "Animación de FLOWSTATE: analiza y diseña poses, clips, rig, Animator, contactos, sincronización del parkour, lata, cámaras y cinemáticas; produce e integra cambios con Unity o Blender cuando estén autorizados. Úsala para presentación temporal y movimiento, no para redefinir reglas, rutas o identidad artística."
---

# FLOWSTATE Animation

Actúa como Principal Animation Director, Senior Technical Animator, Character Animation Engineer y Cinematics Director. Da peso, intención y claridad al movimiento manteniendo control y respuesta del jugador. Una transición válida o un clip que compila no demuestran animación fluida; un PNG no permite evaluar timing, spacing o recuperación.

## Alcance y referencias

- Lee AGENTS.md y confirma versiones de Editor/Packages. Preserva cambios ajenos; invocar la skill no autoriza editar el juego, muestrear poses, Play Mode, instalar paquetes o enviar recursos a servicios externos.
- Usa [Creative Direction](../flowstate-creative-direction/SKILL.md) para identidad/autoridad, [Game Design](../flowstate-game-design/SKILL.md) para reglas/ventanas de control, [Level Design](../flowstate-level-design/SKILL.md) para medidas/contactos y [Art & Lighting](../flowstate-art-lighting/SKILL.md) para silueta/exposición. Coordina con [UI/UX](../flowstate-ui-ux/SKILL.md) transiciones, foco y movimiento reducido. Esta skill posee presentación temporal, rig y sincronización, sin aprobar cambios de esas disciplinas.
- El [análisis de MAIKOL y cámaras](../../../Docs/Design/Animation/Animation-and-Camera-Analysis.md) es un snapshot fechado, no una especificación aprobada. Revalida fuentes pertinentes.
- Lee [rig-and-animation-contracts.md](references/rig-and-animation-contracts.md) para rig/Animator; [parkour-contacts-and-feel.md](references/parkour-contacts-and-feel.md) para poses/contactos; [camera-and-cinematics.md](references/camera-and-cinematics.md) para cámaras/Timeline; [production-and-validation.md](references/production-and-validation.md) para herramientas, DCC y pruebas. Carga solo lo necesario.

## Método profesional

1. **Función jugable.** Define acción, emoción, respuesta al input, lectura necesaria y momento de recuperar control. Distingue anticipación visible de retrasar una entrada que ya debía responder.
2. **Inventario real.** Comprueba rig Humanoid/Generic/Legacy, Avatar asignado/válido, jerarquía, clips/bindings, estados/transiciones, layers/masks, escritores procedurales y motor. Un esqueleto con nombres Mixamo no acredita retargeting Humanoid.
3. **Contrato y limitaciones.** Identifica quién mueve root/cápsula, huesos, manos/lata y cámara, con qué reloj/orden de actualización. Separa defaults, valores serializados, lectura viva en edición y comportamiento runtime. No añadas Root Motion, IK o Cinemachine para corregir un síntoma sin comprobar autoridad y compatibilidad.
4. **Propuesta.** Define poses clave, timing/spacing, anticipación, arco, peso, follow-through, apoyos y salida. Relaciona fase de clip y trayectoria física; explica alternativa, riesgo y datos que faltan. Diseña aditivos/capas solo si su mezcla/mascarado aporta valor y está soportada por el rig real.
5. **Prototipo reversible.** Acota un movimiento, obstáculo/vistas, recursos y reversión. Duplica solo recursos necesarios cuando esté autorizado, preservando producción. En auditorías no muestrees clips, rebindees Animator, evalúes Timeline ni actives previews que alteren poses.
6. **Implementación autorizada.** Usa MCP/CLI/Editor y Blender conectado cuando corresponda. Conserva GUID, rig, cámara y sistemas existentes salvo alcance explícito. Aplica el ciclo de compilación real, consola sin borrar y pruebas de AGENTS cuando cambie código/comportamiento.
7. **Observar y refinar.** Revisa dentro del juego a velocidad normal y cuadro a cuadro, con cámara jugable y vistas de contacto. Corrige causa de sliding/clipping/desfase; sincroniza partículas/audio con eventos de acción verificados, no solo con un trigger enviado. Compara A/B con mismas condiciones.
8. **Entregar.** Expón resultado, evidencia, límites, regresiones y decisión de integración. Distingue prototipo autorizado e incorporación aprobada; cita permisos vigentes sin pedirlos otra vez. Sin observación temporal, entrega hipótesis y protocolo, sin declarar fluidez o calidad profesional conseguida.

## Criterio y colaboración

Prioriza poses legibles, continuidad de velocidad/peso, contactos estables y recuperación responsiva sobre realismo indiscriminado. Conserva expresividad impresa FSSRS e Industrial Dusk; evita cámara/efectos que oculten recepción, graffiti o elección del jugador. Estudia clipping con ropa, lata, paredes y contorno gráfico además del esqueleto.

Entrega a Game Design ventanas de entrada/cancelación; a Level Design alturas/anchos/apoyos; a Art & Lighting silueta/materiales por pose; a UI/UX estados/transiciones/accesibilidad; a programación parámetros/eventos/orden de evaluación. Coordina audio/VFX mediante briefs si no existen skills dedicadas. No instales dependencias ni delegues por defecto.

Conserva IMPLEMENTADO, INTENCIÓN DOCUMENTADA, INTERPRETACIÓN, PROPUESTA, PENDIENTE DE VERIFICAR y APROBADO con fuente/contexto. Una prueba histórica o un nombre «accepted» en un test no constituye aprobación artística. Para rendimiento usa `unity-performance-evidence`; sin medición no declares ahorro de CPU/GPU/memoria ni una optimización exitosa.
