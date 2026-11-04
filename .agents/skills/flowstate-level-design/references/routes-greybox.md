# Rutas, mundo urbano y greybox

## Del objetivo al grafo espacial

Describe inicio, decisión, compromiso, recepción/descanso, objetivo y retorno. Asigna a cada conexión una mecánica existente, dificultad prevista y evidencia faltante. Un conjunto de obstáculos ordenados por nombre no prueba que las conexiones funcionen.

- **Principal:** señales continuas, destino y recuperación comprensibles, aprendizaje que no presuponga atajos de presentación.
- **Secundaria:** otra elección significativa —lectura, altura, vista, práctica— con coste y reencuentro claros; no necesariamente más larga.
- **Secreta:** descubrimiento opcional con indicios y retorno; no oculta instrucciones o acceso indispensables. Evita premiar exclusivamente el barrido exhaustivo de paredes sin señales.

No fuerces tres rutas si no aportan a una situación pequeña; explica su función o por qué no convienen. Diferencia ruta existente comprobada, conexión candidata y geometría nueva necesaria.

## Ritmo, composición y accesibilidad espacial

Alterna vistas y elecciones con ejecución, recuperación y observación/creación. Introduce una exigencia legible, permite práctica segura, varía y combina. Evita elevar simultáneamente altura, precisión, lectura y presión temporal sin una razón de diseño. Señala cuándo un obstáculo contextual puede resolver una acción distinta de la que espera el jugador.

Usa vistas previas y landmarks para que se entienda dónde ir y cómo regresar. Considera silueta, contraste de valores, orientación de masas y encuadre; la iluminación debe reforzar el grafo, conservar Industrial Dusk y reservar protagonismo al graffiti. Señales redundantes de forma/posición/texto o luz ayudan cuando el color no basta. No impongas una nueva paleta ni overlays por defecto.

La verticalidad necesita entrada, refugio, perspectiva útil y regreso. Un suelo inferior no acredita recuperación segura: mide caída, recepción, posibles atrapamientos y coste de volver. Sin sistema de daño/respawn comprobado no inventes sus reglas. Un checkpoint nuevo requiere propuesta de activación, orientación, soporte, restauración de movimiento/cámara, política de obra/recursos y persistencia; no es solo un Transform.

Para acceso alternativo conserva claridad de la experiencia sin exigir la maniobra más difícil a quien quiere crear. El reto opcional puede variar en ejecución mientras comparte destino o punto de interés. Mide espacio para parar, girar y recuperar orientación, además del volumen mínimo de paso.

## Graffiti como objetivo espacial

Define pared visible o revelada, aproximación, plataforma de trabajo, encuadre, luz, modo libre/rápido y salida. Si se propone una intervención en altura, verifica que hay dónde apoyarse; pintar mientras se cuelga no está acreditado en el sistema actual. No conviertas cualquier área de muro en un encargo aprobado o en superficie con persistencia garantizada.

Un muro puede servir como orientación, descanso, motivo de exploración o lugar de composición. Game Design decide reglas de finalización/recompensa; Level Design garantiza oportunidad espacial y comunica sus límites. Conserva el derecho del autor a elegir la obra sin puntuar belleza.

## Greybox reversible

Antes de una construcción autorizada entrega layout con coordenadas/medidas candidatas, grafo, cambios respecto del circuito, incógnitas y aceptación. Elige una sección pequeña. Usa escena de prueba o duplicado autorizado y preserva Gameplay/Blockout; no cambies el build ni guardes sobre el original por comodidad.

Con ProBuilder existente crea solo la geometría necesaria y comprueba collider, caras/normales, escala, layer y ClimbableSurface según intención. Identifica referencias compartidas: duplicar una escena no duplica automáticamente materiales, prefabs o scripts. Congelar transforms o convertir a ProBuilder es una mutación y puede alterar referencias/contactos.

La reversión identifica archivos/objetos propios, activación, datos que podría escribir el ensayo y restauración sin perder obra ni trabajo ajeno. Un toggle no revierte guardados o reputación. Una simulación moderada sin cambios puede explorar orientación/decisión, pero no validar geometría o UX que aún no existen.

Propuesta → greybox autorizado → pruebas de cuerpo/cámara → playtest/iteración → propuesta de arte/luz → nueva comprobación → aprobación/integración. No saltes la revisión funcional final porque el arte conserve el layout; oclusión, contraste, materiales y detalle pueden cambiar su lectura.
