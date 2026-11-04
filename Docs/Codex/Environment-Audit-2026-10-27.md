# Auditoría y configuración de Codex para FLOWSTATE

Fecha: 27 de octubre de 2026, America/Santiago.

## Resultado

El entorno conserva sus herramientas existentes, incorpora Superpowers 7.0.1 mediante su marketplace nativo y una única skill nueva para profiling con evidencia. FLOWSTATE tiene instrucciones de trabajo en `AGENTS.md`. Se verificaron documentación, descubrimiento de skills, CLI, comunicación MCP y acceso visual al Editor.

La comparación SHA-256 de **3.355 archivos** de `Assets`, `Packages` y `ProjectSettings` encontró **cero modificaciones, eliminaciones o archivos añadidos**. El Editor se abrió con 6000.0.41f1; no se entró en Play Mode ni se guardó o modificó la escena.

## Identidad y contexto local

- Usuario Windows: `alons`. Proyecto: `C:\Users\alons\FLOWSTATE`.
- `CODEX_HOME` no estaba definido como variable de entorno; se utiliza `C:\Users\alons\.codex`.
- `codex login status`: autenticado mediante ChatGPT. La lectura local soportada `account/read`, con `refreshToken=false`, confirmó cuenta `requiiem141@…` y plan **Plus**. No se leyeron archivos de autenticación ni se imprimieron tokens.
- Codex CLI: **0.147.0**, instalado en el directorio npm del usuario. Se conservó la selección existente de modelo y esfuerzo; no se cambiaron permisos globales, cuentas ni límites.
- No había `AGENTS.md` del proyecto ni instrucciones heredadas en los directorios ascendentes comprobados. El `AGENTS.md` global existente estaba vacío y se conservó.

## Instalaciones conservadas y verificadas

| Componente | Estado final | Comprobación y límites |
|---|---|---|
| Unity Official Plugin | 0.1.8-beta, habilitado | Skills legibles y descubiertas por Codex; marketplace oficial actualizado sin errores. CLI operativa y Editor real detectado. No se ejecutaron todas sus operaciones de edición. |
| Unity CLI | 1.0.0-beta.14 | `self-update --check` confirmó la versión más reciente del canal instalado; `status` detectó FLOWSTATE listo. |
| Unity MCP / CoplayDev | Integración existente conservada | Paquete del proyecto 10.2.0; endpoint único `http://127.0.0.1:8080/mcp`. Inicialización MCP, recursos y herramientas de lectura respondieron. Es una integración comunitaria distinta del plugin oficial. |
| Context7 | 1.0.2, habilitado | `resolve_library_id` y `query_docs` devolvieron documentación oficial de Unity 6000.0 sobre Test Framework. La etiqueta CLI «Not logged in» no impidió esta consulta pública. |
| Computer Use | Conservado y probado | `@oai/sky`: localización de ventana, activación y captura del Editor. Sin clics sobre controles del juego. |
| Unity Test Framework | 1.4.6, conservado | MCP enumeró **116 pruebas**; primera página de 50. No se ejecutó ninguna suite. |
| Performance Testing | 3.0.3, dependencia existente | Confirmado en `packages-lock.json`; no actualizado ni ejecutado. |
| Microsoft.Unity.Analyzers | Ya existente | Referencia en `Assembly-CSharp.csproj` y DLL de Visual Studio presentes. No se ejecutó una compilación separada ni se añadió otra copia. |
| Pipeline | 0.9.1-exp.1, existente | La CLI detectó el Editor por este enlace; no se instaló ni actualizó el paquete. |

Se conservaron los plugins de documentos, PDF, hojas, presentaciones, plantillas, visualización y navegador. También las configuraciones de Blender, Playwright y los runtimes del escritorio. No se prueban sus operaciones ajenas a Unity en esta auditoría ni se presentan como herramientas específicas de Unity.

## Recuperación de la instalación anterior

Se inspeccionaron únicamente inventarios y archivos de configuración permitidos en `.claude`, las skills y directorios de respaldo conocidos del mismo usuario Windows. Se encontró Superpowers 6.2.0, C# LSP, code-review, Context7 y otros plugins. No se encontró otro `CODEX_HOME` o respaldo de configuración de Codex en los directorios examinados. Esto no demuestra que no exista una copia en otra ubicación.

- **Superpowers recuperado y actualizado:** se instaló desde `https://github.com/obra/superpowers.git`, marketplace `superpowers-dev`, versión **7.0.1**, revisión observada `2ce8aa15d05bd57213e6f746f953aafb7ded7a8b`. Codex descubrió sus **15 skills**, incluyendo debugging, TDD, planificación, revisión y verificación.
- **Skill Unity conservada:** la copia actual en `.codex/skills/unity-mcp-skill` ya contenía las instrucciones de la copia antigua más una mejora sobre herramientas personalizadas y grupos deshabilitados. No se reemplazó por la versión anterior.
- **C# LSP de Claude no migrado:** su integración pertenece al cliente de Claude; copiar su manifiesto no demuestra compatibilidad con Codex. Los analizadores Unity existentes cubren parte de la calidad estática, pero no equivalen a un LSP C# conectado a Codex.
- **code-review anterior no duplicado:** las capacidades locales de revisión de Codex y las skills de Superpowers permiten el flujo de revisión sin migrar comandos específicos de Claude ni conectar servicios externos.
- Las autorizaciones OAuth, suscripciones, conexiones de Figma/GitHub/otros servicios y permisos de otra cuenta no se transfirieron. No se inspeccionaron ni copiaron credenciales, conversaciones o sesiones anteriores.

## Instalaciones nuevas y cambios de configuración

1. Marketplace oficial de Superpowers y plugin nativo 7.0.1. No añade otro servidor MCP.
2. Skill `unity-performance-evidence`, creada con **skill-creator**, en `C:\Users\alons\.codex\skills\unity-performance-evidence`. Estructura validada con el validador oficial y descubrimiento real por `skills/list`. Se leyó y aplicó a la definición del procedimiento de captura; no hubo mediciones de rendimiento.
3. `C:\Users\alons\FLOWSTATE\AGENTS.md`: versiones, preservación del proyecto, comprensión → diseño → implementación → compilación → pruebas → revisión → Unity, criterios de game feel y roles proporcionales.
4. Refresh de los marketplaces de Unity y Context7. No se añadieron versiones redundantes de estos plugins.
5. Se deshabilitó `superpowers@openai-curated` para que solo esté activa la copia nativa. Su caché permanece conservada.

La comparación estructural de `config.toml` encontró únicamente cambios en las referencias/fechas de refresh de esos marketplaces, la nueva fuente/plugin Superpowers y la desactivación de la copia duplicada. No se modificaron servidores MCP, modelo, esfuerzo, confianza de proyectos o permisos.

## Problemas encontrados y resolución

| Hallazgo | Resolución |
|---|---|
| Unity MCP rechazaba la conexión y la CLI no veía instancias | El Editor estaba cerrado. Se abrió el ejecutable instalado **6000.0.41f1** para FLOWSTATE; el servidor existente arrancó y respondió. No se añadió otro MCP. |
| Superpowers del catálogo figuraba instalado, pero no aparecía en `skills/list` | Se comprobó su contenido 6.3.0 y se utilizó la distribución nativa oficial 7.0.1, que sí apareció. La copia del catálogo quedó deshabilitada. No se atribuye una causa interna no demostrada al fallo de descubrimiento. |
| `quick_validate.py` falló por falta de `yaml` en Python local y bundled | Se ejecutó mediante `uv run --no-project --with pyyaml python ...`; validación aprobada. La dependencia quedó en el entorno/caché de uv, sin modificar Python global ni paquetes del juego. |
| La primera captura visual falló porque Unity estaba minimizado | Se activó la ventana y se repitió la observación correctamente. |
| El cliente MCP de este chat retuvo el fallo inicial de arranque | La verificación se completó mediante el protocolo MCP contra el mismo endpoint local. Un chat nuevo o reinicio de Codex permite renegociar las herramientas del cliente; no se afirma que el enlace cacheado de este chat se haya recuperado. |

## Evidencia de Unity y skills

La lectura MCP confirmó `FLOWSTATE@3a6232501f358c97`, Unity **6000.0.41f1**, proyecto correcto y plataforma `StandaloneWindows64`. Estado: listo para herramientas, sin compilación o recarga pendiente, fuera de Play Mode. `manage_scene(get_active)` devolvió `Gameplay`, `isDirty=false`, 33 objetos raíz. `read_console(get)` devolvió cero entradas de error/advertencia en la consulta realizada; no se limpió la consola. Esto describe el estado observado, no una nueva compilación completa o un build aprobado.

`skills/list` del app-server local, con recarga forzada para FLOWSTATE, descubrió **64 skills** sin errores, incluidas las 15 de Superpowers, las de Unity, Context7, la skill Unity MCP y la nueva skill de profiling. Es el inventario del CLI local y no necesariamente coincide con todos los plugins remotos inyectados por el escritorio. Se cargaron instrucciones y se probaron operaciones de lectura representativas; no se ejecutaron operaciones mutantes de cada skill.

El endpoint MCP anunció 48 herramientas y 35 entradas personalizadas. Solo se usaron inicialización, listados, recursos y lecturas de escena/consola. Las versiones del paquete Unity y `serverInfo` son campos distintos; se registró `mcp-for-unity-server` 3.4.11 sin inferir incompatibilidad por esa diferencia.

Dependencias comprobadas: Git **2.54.0.windows.1**, Node **24.18.0**, Python **3.12.10**, uv **0.11.24**, .NET SDK **10.0.400**, Unity CLI **1.0.0-beta.14**. No se considera el SDK .NET global equivalente al compilador de Unity.

## Profiling preparado para futuras tareas

La skill nueva define condiciones comparables y recoge CPU/GPU en ms, mediana, p95/p99, picos, GC por frame y memoria sostenida. Exige identificar hardware, revisión, build, resolución/calidad, VSync, ruta, calentamiento y overhead del Profiler; repetir muestras y comprobar regresiones.

Plan inicial para FLOWSTATE, sin ejecutarlo: acordar dispositivo y presupuesto de frame; capturar por separado una ruta repetible de parkour y una sesión de graffiti con la misma cámara/input; tres muestras comparables tras calentamiento; observar CPU principal/render/jobs, GPU por pases, asignaciones GC y memoria antes/después de ciclos de uso. Comparar el mismo escenario antes y después de una única hipótesis de optimización. Si GPU timing o snapshots no están disponibles, declararlos ausentes.

No se midió ni se afirma una mejora del rendimiento del videojuego, del consumo de tokens o de la velocidad del modelo. La mejora real de configuración es disponer de workflows verificados, evitar un Superpowers duplicado y reducir instrucciones/carga innecesaria mediante selección proporcional.

## Evaluados, no instalados y pendientes

- **Memory Profiler y Code Coverage:** no presentes en el lockfile. Son opciones oficiales útiles, pero instalarlas modificaría paquetes del juego y queda fuera de esta tarea. Tampoco se actualizó Performance Testing.
- **Otro Unity MCP / otra copia de Unity skills:** descartados por redundancia. Se conserva el enlace existente.
- **GitHub/Figma y otros servicios conectados:** el directorio mostró opciones disponibles, pero no se conectaron ni se transfirieron autorizaciones; la revisión local no las requiere. No se subió contenido del proyecto.
- **LSP C# dentro de Codex, depuración con breakpoints y build en dispositivo objetivo:** no verificados o configurados. El Editor y los analizadores están disponibles; esto no prueba dichas capacidades.
- **Tests y profiling del juego:** pendientes de una tarea de validación del comportamiento/rendimiento. Enumerar tests no significa aprobarlos.
- Para que este chat del escritorio incorpore las nuevas skills y renegocie el MCP inicialmente fallido, usar un chat nuevo en FLOWSTATE; reiniciar Codex si conserva el error de conexión.

## Respaldos y reversión

Respaldo local: `C:\Users\alons\.codex\backups\flowstate-environment-20261027`.

Contiene el `config.toml` original, `AGENTS.md` global original vacío, la skill Unity MCP previa, manifiesto SHA-256, inventario y hashes de los 3.355 archivos del juego, estado Git inicial y evidencia de verificación. `config.before-disable-duplicate.toml` conserva además el estado intermedio. No es una copia de seguridad de los assets del juego: los inventarios y hashes permiten comprobar integridad, no restaurar su contenido. Los archivos originales de credenciales y sesiones no se incluyeron.

Para revertir, comparar primero la configuración actual con el respaldo y conservar cualquier cambio posterior del usuario. Restaurar solo las secciones modificadas de marketplaces/plugins, o el archivo original completo si no hubo cambios posteriores. Las nuevas instrucciones y skill se pueden retirar tras comprobar que no fueron editadas posteriormente. Las cachés pueden conservarse: deshabilitar un plugin no exige borrarlas. No se ejecutó ningún rollback, borrado, commit, push o publicación.

## Fuentes primarias consultadas

- [Plugins y configuración local de Codex](https://developers.openai.com/plugins/build/plugins).
- [Unity Official Plugin](https://github.com/Unity-Technologies/unity-agent-plugin) y [documentación de Unity](https://docs.unity.com/en-us/ai/unity-plugin/about-unity-plugin).
- [Superpowers, repositorio oficial](https://github.com/obra/superpowers).
- [Context7, repositorio oficial](https://github.com/upstash/context7).
- [Unity MCP, CoplayDev](https://github.com/CoplayDev/unity-mcp).
- [Microsoft Unity Analyzers](https://github.com/microsoft/Microsoft.Unity.Analyzers).
- [Unity Profiler 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/Profiler.html), [Test Framework](https://docs.unity3d.com/6000.0/Documentation/Manual/DeveloperToolsFeature.html) y [Memory Profiler](https://docs.unity3d.com/Packages/com.unity.memoryprofiler@1.1/manual/index.html).
