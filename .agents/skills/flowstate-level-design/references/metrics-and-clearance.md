# Medir antes de diseñar

## Registro de métricas

Para cada dato registra objeto/asset, propiedad o consulta, unidad, espacio local/mundo, fecha, estado del Editor y categoría: **default**, **serializado**, **observado en edición**, **calculado**, **probado en runtime** o **aprobado para diseño**. Las categorías no son intercambiables. Un resultado funcional de un ensayo tampoco aprueba automáticamente su uso como estándar.

La [captura de métricas](../../../../Docs/Design/LevelDesign/Editor-Metrics-2026-10-28.json) y el [análisis](../../../../Docs/Design/LevelDesign/Blockout-Analysis-and-Alternatives.md) son referencias iniciales. No repitas todas las consultas si el encargo solo necesita una sección estable; revalida configuración y dependencias cuando afecten a la conclusión.

## Personaje y movimiento

- Distingue malla visual, cápsula de colisión, posición del root, centro y pies. Inspecciona CharacterController y colliders adicionales, escala heredada, enabled y layers. Usa unidades de mundo; no supongas equivalencia física o escala humana aprobada.
- Comprueba cómo el controlador convierte dimensiones. FPSController usa altura escalada en Y y radio con el mayor absoluto de escala X/Z; no multipliques del mismo modo todos los parámetros del script. TransformPoint/TransformVector y bounds tienen espacios distintos.
- Velocidad configurada no es velocidad medida: aceleración, giro, frenado, aire y colisión alteran el desplazamiento. El alcance de salto necesita altura relativa, velocidad inicial, margen y recepción; una fórmula balística ideal no es un salto fiable.
- Vault/top-out y wall jump usan trayectorias y detecciones propias. Separa altura de obstáculo desde pies, profundidad, proyección al avanzar, radios de cast, offsets de contacto, destino y clearance. `wallJumpDistance` es distancia de consulta desde un origen, no separación máxima entre caras.
- Comprueba permisos de ClimbableSurface y sus colliders compuestos, triggers, continuidad de normal, movimiento lateral y multiplicador. La escalada continua puede superar `climbMaxHeight`; ese umbral selecciona una resolución contextual, no limita por sí solo la altura total escalable.
- Registra contacto de suelo, slopeLimit, skinWidth, stepOffset serializado y asignaciones runtime. Un parámetro comentado como mundial debe contrastarse con su aplicación a la API; sin ensayo no garantiza un escalón franqueable.

## Geometría, cámara y pintura

Mide collider/mesh y rotación/escala; distingue AABB mundial, dimensiones orientadas y abertura real. Un AABB agrupado es inventario, no espacio transitable. Mide distancias entre superficies pertinentes, no solo pivotes. Excluye colliders inactivos/deshabilitados y trata sensores por separado. Un nombre como Low_065 o Mantle_250 puede ser histórico.

Para cada obstáculo: aproximación y visibilidad previa, acción prevista, alternativa, volumen barrido, salida, recepción y retorno. Anota reservas para cápsula, giro, cámara y pintura. La anchura de la cápsula no basta para decidir comodidad a sprint o con cámara al hombro; el margen debe justificarse y probarse.

Mide el boom/offset y límites de cámara, FOV por modo y radio/padding de colisión. Una abertura que deja pasar al cuerpo puede comprimir la cámara u ocultar la recepción. Revisa paredes delgadas, esquinas, techos y el cambio a primera persona.

Para graffiti verifica máscara/collider, normal, distancia desde la referencia real de boquilla, tamaño de pieza y soporte de esquinas. Detección lejana, distancia máxima pintable y distancia cómoda de trabajo no son el mismo dato. Una prueba estática de rayos no garantiza que el jugador pueda ponerse, orientarse o pintar allí.

## Escalera de evidencia

Una consulta solo lectura en Editor puede acreditar dimensiones, referencias o hits estáticos; no ejecuta la mecánica. Un cast hipotético permite estudiar una condición de detección, no afirmar que la trayectoria y recepción sean válidas. No muevas al jugador ni invoques métodos con efectos para completar una medición documental.

Para adoptar una métrica fiable, cuando se autorice, ensaya varios puntos de aproximación y velocidades, ambos sentidos, extremos del rango y fallos recuperables. Registra configuración real, controles, frame rate relevante y resultados. Define un margen candidato a partir de esa evidencia; no conviertas automáticamente el máximo observado en el valor de construcción.
