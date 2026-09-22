# FLOW STATE — Main Menu Fase 3B

Fecha de validación: 2026-09-21  
Escena: `Assets/02_Escenas/MainMenu_FlowState.unity`  
Estado guardado: `IntroMode.Disabled`  
Duración del prototipo Full: 6.4 s

## Resultado visual

El prototipo implementa exactamente tres trazos. Empieza sobre negro, revela una única escena 3D animada dentro de tres recortes, prolonga los trazos mediante pasadas horizontales y diagonales, y deposita partes del RGB original de `YakuzaStudio_FLOWSTATE (1).png`. El logo queda deliberadamente incompleto.

La composición usa:

- A / Creative Flow: pasada izquierda continua y curva, seguida por la F y el subrayado izquierdo.
- B / Anger: pasada central más rápida, ancha, interrumpida y con más overspray; alcanza la zona de la W.
- C / Clarity–Neutral: pasada derecha más estrecha y desaturada; construye parte de T/A y el subrayado derecho.

Las capturas finales están en `Captures/Phase3B/`:

- `A-black-first-fragment.png` — negro y crecimiento inicial de A.
- `B-three-fragments.png` — tres ventanas verticales, separadas y con una única fuente 3D.
- `C-structure-break.png` — prolongaciones que conectan y rompen la lectura de paneles.
- `D-partial-flow-state.png` — F, W, T/A y subrayados parciales con el RGB oficial.

## Arquitectura

`FlowStateIntroDirector` sigue siendo el coordinador. Bloquea menú y cámara reactiva, inicia Timeline, pintura, contenido y audio de spray; en Skip ejecuta el mismo handoff seguro de Fase 3A. No escribe el transform de Main Camera. La cámara principal permanece frontal y estática.

`FlowStateIntroPaintController` posee las dos superficies ping-pong de máscara, evalúa las tres definiciones, entrega máscaras/crops al material, congela el último frame para revisión y libera todos los RenderTexture de intro. La máscara se reconstruye desde tiempo absoluto y distancia de arco; no integra deltas por frame.

`FlowStateIntroContentProvider` posee una sola cámara secundaria y una sola RenderTexture de contenido. Mueve lentamente la cámara y el pivote de la lata usando el tiempo absoluto de Timeline. El diorama temporal usa pared, zócalo, montantes, ventilación, banda pintada, una luz y la lata existente.

`IntroFragmentDefinition` es una clase serializable editable desde Inspector. Contiene `position`, `scale`, `orientation`, `path`, `extensionPath`, `depositionPath`, `width`, `duration`, `revealCurve`, `contentCrop`, `contentWindow`, `overspray`, `treatment`, `logoContribution`, `depositionWidth` y `seed`.

`IntroPaintTrack` y nueve clips serializados en `ThreeTraces.playable` controlan Reveal, Extension y Deposition para A/B/C. Los clips admiten solapamiento y búsqueda determinista. Timeline no tiene una pista de Main Camera.

`PaintRenderSurface` comparte únicamente creación, clear, snapshot y release de superficies. `GraffitiMenuPainter` conserva su responsabilidad y usa esa utilidad sin asumir lógica de intro.

## RenderTextures y memoria

La intro activa exactamente tres RenderTextures temporales:

| Uso | Cantidad | Resolución | Formato | Profundidad |
|---|---:|---:|---|---:|
| Máscaras ping-pong RGBA | 2 | 1024×576 | ARGB32 Linear | 0 |
| Mundo 3D compartido | 1 | 1024×576 | ARGB32 Linear | 24 |

Las tres ventanas leen la misma textura de mundo mediante `contentCrop` y `contentWindow`. A y B comparten una transformación UV compatible para continuidad parcial; C usa un recorte independiente y desaturación parcial.

El coste fijo de color es 6.75 MiB; con el buffer de profundidad de 24 bits del contenido, el presupuesto propio es aproximadamente 8.44 MiB. Al terminar, se reemplazan las superficies vivas por dos snapshots RGBA32 de revisión (máscara y contenido, 4.5 MiB en total) y se desactiva la cámara secundaria. Skip destruye también los snapshots.

Una lectura del editor durante Full mostró 95 draw calls, 95 batches, 18,389 triángulos, 14,859 vértices y 48 RenderTextures globales. Es una lectura del Editor/URP completo, no un perfil de build; el presupuesto propio anterior es el dato atribuible al prototipo.

## Máscara, WORLD → LOGO y debug

`IntroSprayMask.shader` calcula distancia al recorrido revelado, irregularidad multiescala, micrograno, overspray y pequeñas interrupciones para Anger. Los canales R/G/B almacenan A/B/C y A almacena únicamente la deposición del logo.

`IntroWorldPigment.shader` muestra la RenderTexture animada donde existe pintura. Cuando la pasada de deposición alcanza una coordenada, ese punto cambia a `logo.rgb * logo.a`; no existe `_LogoOpacity` ni un fade global. La máscara de pintura permanece, de modo que la transición se lee como repintado espacial.

El enum `IntroPaintDebug` permite ver Composite, Mask A, Mask B, Mask C, Combined Paint, Logo Deposition y Content desde Inspector.

## Audio

`MenuAudio.StartIntroSpray` reutiliza los clips placeholder existentes: clack de ataque y aerosol en loop. Las fronteras de los clips Timeline generan Start/Loop/Stop. El comportamiento visual es independiente de volumen o mute.

## Validación

- Play Mode: 18/18 aprobados en 65.88 s. Incluye los 14 tests anteriores y 4 tests de Fase 3B.
- Edit Mode: 3/3 aprobados en 0.27 s.
- Evidencia XML: `Captures/Phase3B/PlayModeResults.xml` y `Captures/Phase3B/EditModeResults.xml`.
- Determinismo: la máscara de t=3.8 fue idéntica tras evaluarla directamente y tras doce búsquedas intermedias.
- Crecimiento observado a 1024×576: A 6,739→16,358 px; B 15,127→21,524 px; C 6,755→11,710 px.
- Deposición: 0 px en t=3.9, 7,987 px en t=4.45 y 91,012 px en t=6.25.
- Una cámara secundaria y una única textura de contenido confirmadas en runtime.
- La fuente de contenido continúa cambiando mientras las máscaras verticales ya terminadas permanecen idénticas.
- Fin natural: cero RenderTextures de intro vivas, Content Camera desactivada y resultado congelado para revisión.
- Skip: libera snapshots y materiales, restaura pared/postproceso/audio, activa cámara reactiva y entra al menú una sola vez.
- `IntroMode.Disabled`: entrada directa al menú sin asignar recursos de intro.
- Validación de escena: 0 referencias faltantes, 0 scripts faltantes y 0 prefabs rotos.
- Consola final: 0 errores y 0 warnings nuevos.

El único warning observado durante la suite es el ya existente en `PlayUsesPaintTransitionAndLoadsGameplay`: Unity elimina de la camera stack una overlay camera que el flujo anterior destruye. No fue introducido por Fase 3B.

## Archivos

Creado en Fase 3B:

- `Runtime/FlowStateIntroPaintController.cs`
- `Runtime/FlowStateIntroContentProvider.cs`
- `Runtime/IntroFragmentDefinition.cs`
- `Runtime/IntroPaintTrack.cs`
- `Runtime/IntroPaintClip.cs`
- `Runtime/PaintRenderSurface.cs`
- `Shaders/IntroSprayMask.shader`
- `Shaders/IntroWorldPigment.shader`
- `Editor/IntroPrototypeBuilder.cs`
- `IntroPrototype/ThreeTraces.playable`
- `IntroPrototype/Concrete.mat`, `Iron.mat`, `Faded ochre.mat`
- `Tests/PlayMode/IntroPrototypePlayTests.cs`

Modificado en Fase 3B:

- `Assets/02_Escenas/MainMenu_FlowState.unity`
- `Runtime/FlowStateIntroDirector.cs`
- `Runtime/MenuAudio.cs`
- `Runtime/GraffitiMenuPainter.cs`
- `Runtime/FlowState.MainMenu.asmdef`
- `Editor/FlowState.MainMenu.Editor.asmdef`

No se modificaron el PNG oficial, Gameplay, las Input Actions ni los ajustes del render pipeline.

## Desviaciones deliberadas de Fase 2

El prototipo no avanza automáticamente al Main Menu al finalizar. Conserva la imagen parcial para evaluación y libera sus RenderTextures; Skip realiza el handoff. Esto responde al límite explícito de Fase 3B: detenerse antes de construir la transición completa.

La imagen congelada usa snapshots `Texture2D` temporales. Es la forma mínima de mantener el hito D visible con Content Camera apagada y sin retener RenderTextures. No se creó segunda cámara, VFX Graph, Camera Push, World Reveal ni diorama definitivo.
