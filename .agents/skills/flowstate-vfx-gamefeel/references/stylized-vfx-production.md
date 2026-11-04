# Producción de VFX estilizados

## Lenguaje y rendering

Preserva papel/tinta y acentos FSSRS; el graffiti conserva autoría y protagonismo cromático. El composite está antes de transparencias: un aerosol transparente puede recibir tratamiento propio y post final, sin recibir automáticamente todos sus outlines. Revalida cámara/renderer/orden con Art & Lighting.

GraffitiCartoonAerosol.shader usa URP SRPDefaultUnlit, Transparent+25, alpha premultiplicada, ZWrite Off, ZTest LEqual, Cull Off y fog; incorpora paleta/patrones FSSRS y animación escalonada. No mezcles salida premultiplicada con Blend SrcAlpha sin revisar el contrato. Globales FSSRS y color de tinta participan: una emisión visual distinta del RGB elegido no prueba corrupción de pintura.

Flecks/halftone globales desactivados en la dirección actual no son funciones ausentes. El shader de aerosol tiene sus propios controles locales; no los confundas con el Volume ni los conviertas en autorización para ruido global. Distorsión, dissolve, trails, tinta comic o halftone son posibilidades para propuestas justificadas, no efectos obligatorios.

## Sistemas y construcción

Usa ParticleSystem existente para efectos acotados si cumple; VFX Graph 17.0.4 está declarado en el snapshot, pero no se encontraron .vfx/VisualEffectAsset en Assets ni VisualEffect en Gameplay. Revalida. Graph solo se justifica por necesidad demostrada, plataforma y coste/autoría; no presupongas ventaja GPU o instales paquetes para sustituir Shuriken.

El aerosol usa pigmento, aire, microgotas y valve pop, maxParticles totales de código 402. Bind reutiliza la primaria y crea capas/materiales una vez; no reconstruyas por clic. Emisión, ruido, velocidad/lifetime, forma/aspecto y color cambian por perfil. El nombre Valve Comic Pop no significa que sea URP Bloom.

Separa nacimiento, sustain y cola; compara boquillas a igual distancia/presión. Revisa dirección del rayo y nozzleUp, aspect ratio del chisel, near plane, escala, World/Local simulation, límites/bounds, culling y profundidad. No cambies la pintura real para que coincida con una nube decorativa incorrecta.

La vida útil calculada desde distancia y WallClearance es una aproximación, no una colisión garantizada: mínimos de lifetime, ruido, velocidad y gravedad pueden cambiar alcance. Comprueba paredes cercanas, ángulos, oclusión y mano/lata; no habilites collision/soft particles o más raycasts sin medir causa y coste.

En cambios autorizados, reutiliza prefabs/recursos o crea variantes acotadas, con campos configurables y referencias válidas. Shader Graph/HLSL se elige según propietario existente; no migres shaders para un ajuste local. Verifica keywords/variantes, textura/depth requirements, colas, fog, instancing y plataformas. Shader.Find soportado en Editor no acredita inclusión y soporte en Player.

## Game feel y accesibilidad

Define ataque/intensidad por función: confirmar entrada, comunicar flujo/recurso o marcar contacto/resultado. Gradúa énfasis por importancia evitando competencia con ruta, obra y HUD. Para velocidad/parkour, revisa FOV y animación ya presentes antes de añadir trails/shake. Una nube más densa no demuestra aerosol más satisfactorio.

No añadas otro escritor de cámara. Impulsos/FOV pasan por su propietario y deben conservar apuntado/colisión/control; con Animation/UI/UX acuerda variantes reducidas. Prueba tamaño de área, frecuencia de pulsos, contraste, movimiento escalonado y simultaneidad, conservando señales estáticas/sonoras pertinentes. No declares seguridad fotosensible ni comodidad por un valor de intensidad o un PNG.

FS.Menu.Motion no es contrato global de VFX; GraffitiSprayVFX y su shader no consumen esa preferencia en la lectura. Reducir cámara/HUD no reduce necesariamente partículas, pulsos o ruido temporal. Cualquier política nueva debe acordarse; la auditoría no instala un sistema de accesibilidad.
