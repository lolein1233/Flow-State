# Producción técnica de UI

## Framework y herramientas

Sigue la arquitectura existente. Usa `unity:ui` para routing y `unity:ui-ugui` en Canvas/TMP; `unity:ui-uitk` para UXML/USS/UIDocument pertinentes, `unity:ui-imgui` para Editor legado. UI Toolkit es una opción contextual, no una migración automática ni solución al menú 3D actual. No instales paquetes/TMP/tweening para inspeccionar o resolver un ajuste local.

Prioriza MCP existente: lista instancias y comprueba proyecto/estado antes de actuar. Consultas explícitas pequeñas evitan serializar getters con efectos. CLI oficial complementa tareas repetibles con ayuda y project-path. Si Unity está cerrado o MCP desconectado, informa del límite y lee archivos; no abras el proyecto ni añadas servidor/paquetes para una auditoría que prohíbe cambios.

Computer Use permite inspección visual y entradas de prueba locales cuando estén autorizadas. Una captura de cámara puede excluir Screen Space Overlay; captura la composición Game View/pantalla pertinente para evaluar UI completa. Scene View y MainCamera pueden usar renderers distintos. No guardes evidencias en Assets ni envíes capturas a servicios externos sin autorización.

## uGUI/TMP y reutilización

Antes de modificar verifica Canvas/renderMode/sorting/cámara, CanvasScaler, RectTransform, layout owners, grupos/máscaras y referencias de prefab/escena. Componente presente no implica activo, visible, interactuable o conectado. Una raíz inactiva puede contener toda la pantalla; los PrefabInstance necesitan resolver overrides y ciclo de activación.

En cambios autorizados, ajusta anchors/pivot/offsets respetando Layout Groups/ContentSizeFitter y escala de referencia; no pases toda UI a otra política de scaling. Si trabajas con ScrollRect consulta su referencia oficial. Comprueba textos largos, acentos/glifos, tamaños mínimos legibles, fallback/atlas y clipping real; auto-size o overflow no demuestran adaptación correcta.

Usa nombres/tipos UI completos cuando haya ambigüedad, referencias serializadas claras, eventos y propietarios de datos existentes. Reutiliza prefabs/componentes sin destruir jerarquías para «arreglarlas». Valida onClick/listeners añadidos por código además de los persistentes: una lista vacía en YAML no prueba botón desconectado.

Revisa EventSystem y módulo activos por contexto, mapas de acciones, selección inicial y navegación hacia controles habilitados. RaycastTarget=false en decoración pertinente, GraphicRaycaster en Canvas interactivo y CanvasGroup.alpha/interactable/blocksRaycasts coherentes. Un grupo transparente puede bloquear y un hijo puede ignorar grupos; no diagnostiques solo por alpha.

## Animación y respuesta

Reutiliza PunkMenuMotion/Button, curvas/coroutines y estados existentes según la pantalla. Animator/Timeline sirven si su autoría/timing lo justifican; un tween externo no es requisito. Conserva cancelación/interrupción/reentrada y reloj no escalado en pausa. Evita dos sistemas escribiendo el mismo RectTransform o material.

Mantén estable el área de interacción cuando se anima texto. Define normal, foco, hover, pulsado, seleccionado y deshabilitado, con feedback de confirmación y cancelación. No uses movimiento como única señal de foco. Revisa sonido/mute y frecuencia de feedback; no añadas audio nuevo por defecto.

## Rendimiento y pruebas

Identifica posibles rebuilds de Canvas/layout, actualizaciones TMP, material instancing, batches, raycasts y overdraw; mide antes de reorganizar toda UI. No separes cada elemento en su propio Canvas ni agrupes todo sin evidencia. Evita actualizar strings/layout continuamente cuando el cambio lo permita, sin prometer ganancias no medidas.

Para cambios de comportamiento compila en Unity, lee consola sin borrarla y ejecuta suites relevantes: menús bajo Assets/09_MainMenu/Tests, QuickGraffitiTests/GraffitiInteractionTests cuando cubran el caso; añade regresión útil si procede. Respeta asmdefs Runtime/Editor. Tests existentes no ejecutados permanecen pendientes; un test con Gamepad virtual no demuestra mando físico, todos los glifos o cobertura integral de gameplay.
