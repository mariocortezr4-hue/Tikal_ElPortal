# Nivel 2 — Plaza Keynes

Fecha: 24 de octubre de 2026. Rama de trabajo: `feature/level2-plaza-keynes-environment`.

## Escena y alcance

Escena nueva: `Assets/_Project/Scenes/Levels/Level02_PlazaKeynes.unity`. Proyecto real: `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Tikal_ElPortal\My project`. Se construyó y probó primero en `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Tikal_ElPortal\_PlazaKeynes_20261024` y se copiaron únicamente los archivos nuevos de Level02, con los mismos GUID y contenido. Al preparar Git se retiraron únicamente espacios finales de líneas generados por Unity; la comparación de texto normalizado confirmó equivalencia, registrada en Formatting_Only_Verification.json. No se cambió geometría, transformaciones ni lógica tras las pruebas. No se modificaron escenas, scripts o prefabs preexistentes.

Escenario artístico de exploración libre. No contiene puzzles, pistas, objetivos, diálogos, coleccionables, condiciones de victoria/derrota ni nuevos controladores de juego. Reutiliza el prefab `Assets/_Project/Prefabs/Player/Explorer.prefab`, sin cambiarlo. No se duplicaron input, cámaras, AudioListener o EventSystem.

No había una escena Level02 ni spawn definido. `SceneCatalog` mantiene su entrada Level02 con ruta vacía; no se tocó el menú, Build Settings o transición de Level01. Para explorar esta entrega hay que abrir directamente la nueva escena en Editor. La conexión al flujo del juego queda expresamente fuera de este trabajo.

## Referencias y lectura artística

Referencias reales inspeccionadas visualmente antes de autoría: `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Tikal_ElPortal\Para_Nivel_2\Imagen_Reference1.png` y `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Tikal_ElPortal\Para_Nivel_2\Imagen_Reference2.png`; copias de consulta en References, hashes en Preparation.json. Se tomó su jerarquía: monumento central, plaza abierta, ruinas bajas/columnas cercanas, templos en el perímetro y selva/relieve por capas. No se promete reproducción fotorealista de las imágenes ni aprobación artística automática.

## Composición

- Plaza pavimentada de aproximadamente 150 × 150 m, con contorno irregular. Terreno de 720 × 720 m y colinas perimetrales; centro relativamente plano y desniveles de fondo.
- Estatua real de Keynes, procedente de `Assets_Especial/Estatuta_Keynes/01_UNITY_READY`; importada como copia en `Assets/_Project/Art/Environment/Level02/Keynes/Prefabs/PFB_EstatuaKeynes.prefab`. Altura del modelo completo aproximadamente 10 m por escala uniforme ×10, sobre tres terrazas bajas que suman 0.72 m. Frente orientado a la llegada. No se usó una estatua genérica ni placeholder.
- Seis ramales amplios parcialmente radiales, con orientaciones y desgaste variados. 48 tramos de piso existentes y diez patios/terrazas secundarios. Centro sin grandes árboles ni bloqueos permanentes del monumento.
- Cinco landmarks arquitectónicos de distintas escalas/distancias: dos cercanos con acceso a primera terraza y tres de fondo. Son ensamblajes de assets reales, no nuevos FBX de templos.
- 154 piezas de ruina, 66 columnas en grupos irregulares, 51 rocas y 164 plantas bajas, más 10 raíces decorativas. Las piezas están asentadas por sus bounds inferiores; rocas y algunos fondos se entierran ligeramente de forma intencional.
- Árboles grandes: cero dentro del centro, 60 intermedios y 64 de fondo con mallas reales; 56 arbustos de sotobosque lejano.
- Selva adicional: 620 grupos de siluetas cruzadas, distribuidos en ocho sectores con 2480 triángulos en total y atlas RGBA 1024². Horneados en Unity a partir de cuatro prefabs existentes confirmados; no son recursos externos ni imágenes generadas para simular capturas. Solo presentación de fondo, sin collider ni sombras.
- Una luz direccional cálida y fog sencillo; skybox procedural, sin volumétricos pesados. Materiales de escenario URP/Lit; nuevas variantes de pavimento/suelo evitan cambiar los materiales compartidos de otros niveles.

La jerarquía separa Terrain_Ground, Plaza_Paths, KeynesMonument, Ruins, CityCourts, Temples, Rocks, Vegetation_Low, Vegetation_Midground, Background_Jungle, Background_ImpostorForest y Lighting bajo ENV_Level02_PlazaKeynes. El Explorer queda fuera del root artístico.

## Colisiones y optimización

723 colocaciones de prefabs, más ocho sectores de impostores y el terreno. 723 LODGroups. 300 colliders activos, fundamentalmente cajas, escalones simples y CharacterController. Un solo MeshCollider habilitado: el terreno estático, no una malla de monumento/vegetación de alta complejidad repetida. Vegetación pequeña y fondo sin colliders activos; sombras deshabilitadas en el fondo. No se cambiaron los prefabs de origen.

Los LOD de los assets se conservan. Las instancias de fondo mantienen solo el último nivel activo; intermedios cambian antes a niveles ligeros. El fondo de impostores no necesita LOD de malla adicional. Keynes conserva 30,699 / 15,348 / 6,139 triángulos. No se regeneraron modelos ambientales.

Autoría determinista, semillas 241026 y 241027 para geometría de fondo. Se aplicó dos veces y se compararon nombres, transformaciones, estados activos y conteos: sin duplicación. La comprobación es de contenido/estructura, no una promesa de fileID o bytes de escena idénticos después de regenerar todo el root. Herramienta Editor específica: menú `Tikal/Level02/Rebuild Plaza Keynes Environment`; usar solo con la nueva escena y respaldar antes de regenerar cambios manuales del root artístico.

## Validación realmente ejecutada

Editor: escena cargada, dependencias resueltas, cero Missing Scripts/materiales, un Explorer/MainCamera/AudioListener y cero EventSystem. Sin autoridades de puzzles/progresión añadidas. Reapertura de entrega sin guardar cambios y auditoría de contacto: 575 colocaciones evaluadas, 0 con separación de base superior a 25 cm. Se omiten de esta prueba de suelo las piezas elevadas intencionalmente sobre patios o terrazas. El contacto se mide con bounds inferiores frente a la función de terreno; no es un análisis de intersección de cada triángulo.

Play Mode real: comprobaciones de escena completadas, `sceneFunctionalPassed=true`, archivo de escena inalterado. Se recorrieron rutas con el PlayerController real mediante eventos de teclado de InputSystem: llegada en FPS/TPS, cuatro lados del monumento, acceso central y primera terraza del templo izquierdo. Sin caída a través del suelo ni grandes bloqueos en esas rutas. Cámara TPS mantenida por el sistema real y capturada con personaje existente. No se certifica una sesión humana ni cada rincón del terreno de 720 m; las posiciones de inicio de cada ruta se controlan en el fixture.

El resultado global `passed=false` del JSON se conserva por una excepción de **UnityEditor.Search.SearchDatabase** de indexación. No hubo fallo de aserción de la escena en la pasada final. No se oculta el aviso ni se atribuye al modelo. También se registran mensajes JobTempAlloc al cierre, de origen no determinado; no se realizó investigación causal fuera del alcance.

## Coste observado

Hardware real: Intel(R) UHD Graphics 630. Se midieron 119 fotogramas después del calentamiento: **32.09 ms de media**, equivalente aritmético a **31.2 FPS**, p95 **38.38 ms**. Son observaciones del Editor y esa vista de TPS, no una garantía de build ni de todo el nivel. La primera composición registró 44.32 ms y la segunda 36.51 ms; se conservaron sus evidencias.

Los contadores UnityStats de las capturas están en Play_Validation.json y son contadores globales del Editor, no draw calls GPU aisladas por cámara. No se presentan como perfil de una build. Queda recomendada una comprobación de build y calidad visual manual antes de cerrar rendimiento para gráficos integrados; no se efectuó optimización profunda.

## Capturas reales

Los nueve archivos solicitados están en Captures: Entrance, Center, StatueFront, StatueWide, LeftSector, RightSector, BackgroundTemples, Wide y TPSExploration, todos con prefijo PlazaKeynes_. Se inspeccionaron las vistas de entrada, general, estatua y TPS. No son renders generativos ni imágenes retocadas. La escala/pavimento/siluetas deben revisarse artísticamente con el usuario; los assets Tripo existentes conservan su faceteado y detalle originales.

## Seguridad y pendientes

La línea base de 2177 archivos authored preexistentes de Assets/Packages/ProjectSettings permanece idéntica por SHA-256. Ninguna modificación a MainMenu, Intro, Tutorial, Level01, puzzles, Journal, Map, checkpoints, respawn, input, PlayerController, CharacterVisualSpawner, personajes o sus prefabs/scripts. No se modificaron fuentes de la biblioteca ni la biblioteca de Axel. Tampoco se conectó la escena al menú o transición.

Pendientes: revisión visual humana, recorrido libre adicional, benchmark de build y eventual autorización independiente para conectar Level02 al flujo del juego. La escena es únicamente artística/explorable. No implementar mecánicas futuras desde esta entrega.

Commit y push se registran en `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Tikal_ElPortal\Para_Nivel_2\PlazaKeynes_20261024\GIT_DELIVERY.md` después de la revisión de alcance y `git diff --check`.

El `git diff --check` inicial detectó espacios finales de serialización Unity. Se normalizaron exclusivamente archivos nuevos; no se alteraron archivos preexistentes. Las evidencias Play/Editor corresponden a la escena anterior a ese ajuste de formato, con contenido funcional equivalente y los mismos GUID.
