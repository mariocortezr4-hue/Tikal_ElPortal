# Tutorial — Environment Art Pass

> Historial: las secciones hasta «Path Containment & Tutorial Guidance» describen la primera pasada aprobada (`eb33dc9`, `f1f51ac`). La sección final documenta el estado vigente y sus excepciones autorizadas: elevación del dintel, sustitución de colisiones placeholder por boundaries y safety local.

## Alcance y fuentes

Unity 6000.6.3f1, URP 17.6.0. Rama `feature/tutorial-environment-art-pass`, base `9d282fa` (merge del pulido de Intro `6e58da2`). Se viste únicamente `Assets/_Project/Scenes/Tutorial/TUT_ExplorerBasics.unity`, conservando Move → Look → Jump → Crouch → Sprint → Interact → Exit. No se modifica código runtime, InputActions, Player, UI, selección, persistencia, Intro, Level01, puzzles ni checkpoints. No se trabaja audio ni se instala software.

Se consultó el handoff `D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/Tikal_Assets_Optimized/_PROJECT_HANDOFF`: README, estado de biblioteca y sus enlaces a prefabs, requisitos/cobertura de Tutorial, pipeline, integración y revisiones de no-personajes 17/19/20/21. Sus autorizaciones históricas/documentales no sustituyen el encargo actual del usuario, que autoriza esta feature de Tutorial. R4 y el manifiesto del kit prevalecen sobre los informes anteriores B/C de puerta/observación. Esos recursos de Level01 no se importan.

Kit consultado: `D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/TIKAL_NON_CHARACTER_INTEGRATION_KIT`, README, guía Tutorial, mapa de dependencias, manifiesto maestro/individuales, checklist y evidencias de validación. `TIKAL_PUZZLES_INTEGRATION_KIT` queda reservado y no se abre/importa.

Las fuentes son de solo lectura. Para vegetación/rocas/ruinas se usan exclusivamente los prefabs de pruebas vigentes enlazados por el handoff, con su cierre de FBX finales, materiales, texturas y metas. No se utilizan `00_SOURCE`, ZIP originales ni modelos antiguos high-poly. Para los tres adaptadores y tierra se usa el UnityPayload R4. Se elige copia selectiva de dependencias con `.meta`; no se importa el unitypackage completo ni muestras diagnósticas. No se altera la política de firma de Unity.

## Selección y procedencia

| Recurso | Categoría | Triángulos LOD0 / LOD1 / LOD2 preparados |
|---|---|---|
| VEG_ArbolTropical_04 | Vegetation | 25718 / 11107 / 4879 |
| VEG_ArbolTropical_02 | Vegetation | 20000 / 9000 / 3500 |
| VEG_PalmeraTropical_02 | Vegetation | 11861 / 4999 / 1799 |
| VEG_ArbustoTropical_02 | Vegetation | 3208 / 2500 / 1762 |
| VEG_ArbustoTropical_03 | Vegetation | 4643 / 2793 / 2793 |
| VEG_HelechoTropical_02 | Vegetation | 3500 / 1800 / 1458 |
| VEG_PlantaTropical_02 | Vegetation | 4321 / 2199 / 899 |
| VEG_HojasSecasMonton_01 | Vegetation | 2759 / 1873 |
| ROC_RocaMusgo_02 | Rocks | 6000 / 2500 / 800 |
| ROC_RocaPlana_01 | Rocks | 5916 / 2400 / 700 |
| ROC_RocaTropical_01 | Rocks | 2442 / 1200 / 1200 |
| ARQ_MuroMaya_01 | Ruins | 5716 / 3000 / 3000 |
| ARQ_ColumnaMaya_01 | Ruins | 3999 / 1799 / 599 |

También se importan:

- `ADP_Tutorial_JumpObstacle`: tronco, 6.000 triángulos, dimensiones 3,5 × 0,6 × 0,658768 m, pivote inferior.
- `ADP_Tutorial_LowLintel_R2`: dintel, 4.796 triángulos, 4,2 × 0,45 × 1,2 m, pivote central.
- `ADP_Tutorial_LiftStone`: piedra móvil, 6.000 triángulos, 3,6 × 2,8 × 0,7 m, wrapper de ejes/profundidad del kit conservado.
- Material de suelo R4 y sus tres texturas. No se copia el modelo `VALIDATION_ONLY_Ground_Surfaces` ni su prefab diagnóstico.

`TutorialEnvironmentSources.json` registra 110 archivos seleccionados, rutas fuente/destino, GUID, SHA256 de binario y meta antes de importar. `TutorialEnvironmentSelection.json` registra las 13 familias ambientales. La operación comprueba colisiones de ruta/GUID antes de copiar; las únicas referencias fuera del cierre son shaders/clases oficiales de URP ya instalado. Los FBX y las texturas copiados coinciden binariamente con sus fuentes; los prefabs de destino reciben únicamente ajustes artísticos documentados abajo. No se renombran texturas ni se rompen referencias.

Los archivos van a `Assets/_Project/Art/Environment/Tutorial/{Vegetation,Rocks,Ruins,Adapters,Ground}` y los 16 prefabs a `Assets/_Project/Prefabs/Environment/Tutorial`. Se mantienen nombres canónicos PFB_/VEG_/ROC_/ARQ_/ADP_, sin inventar otra identidad de catálogo. No hay scripts del kit, escenas de muestra ni audio copiado. El mayor archivo fuente seleccionado tiene 10.479.851 bytes, por debajo del límite de 90 MB.

## Reservas para Level01

No se importan ceibas/hero trees, templos, grandes arcos/portales, plataformas, escalinatas monumentales, altares ceremoniales, estelas de pista, murales, guardianes, puerta principal, Eco, observación, llave ni sello de revelación. Los recursos de Level01 y Shared/Audio del NON_CHARACTER kit quedan fuera. El kit de puzzles permanece intacto. Las dos especies de árbol, palmera y piezas menores usadas aquí pueden reutilizarse; se reserva la escala monumental y la variedad restante para la revelación arqueológica de Level01.

## Contrato espacial y composición

El spawn sigue en (0; 0,05; 0). El suelo funcional conserva 28 × 0,6 × 60 m y el camino 4,6 × 0,08 × 54 m. Salto en Z=11, paso bajo en Z=19, piedra móvil en Z=36 y trigger final detrás de ella permanecen. Ningún collider/trigger se mueve o redimensiona para adaptar arte.

Se usa una **seed fija 210621**, System.Random, con separación lateral y variaciones moderadas de posición, yaw y escala. El authoring reutiliza nombres deterministas, no crea duplicados. La composición tiene:

- 14 árboles laterales, tres variantes, principalmente X ±9,5–12,5 m, con escalas 0,66–0,88.
- 6 árboles de fondo tras la salida, Z aproximadamente 53–58 m, formando profundidad y ocultando el horizonte; escalas 0,85–1,05.
- 24 arbustos de dos variantes, X aproximadamente ±3,8–6,2 m.
- 30 plantas/helechos de dos variantes, X aproximadamente ±3–5,6 m, más 12 montones pequeños de hojarasca en los márgenes.
- 18 rocas de tres variantes, bordeando el recorrido sin nuevos colliders.
- Tres fragmentos de muro laterales. Cuatro columnas adaptadas visualmente sustituyen apoyos del dintel y marcadores de salida; no forman un templo.

El sendero central conserva espacio para el controlador y el boom TPS. Los decorados no obstruyen físicamente CameraCollision porque no tienen colliders habilitados; los troncos grandes quedan apartados del eje. Se revisan capturas de juego FPS/TPS para detectar obstrucción visual, que no queda probada solo por ausencia de collider.

## Obstáculos e interactuable

Los renderers de los proxies se ocultan; sus GameObjects, Transform funcional, colliders, scripts, fileID/GUID y referencias permanecen.

- **Jump root**: hijo `Art_ADP_Tutorial_JumpObstacle`, bounds ajustados al volumen original (con profundidad real 0,658768 m frente al collider original 0,7 m). Collider original intacto; el tronco comunica salto.
- **Low ruin lintel**: hijo `Art_ADP_Tutorial_LowLintel_R2`, mismas dimensiones y altura del proxy; conserva clearance inferior 1,325 m. Apoyos laterales con columnas, sin cambiar sus colliders.
- **Lift stone**: hijo `Art_ADP_Tutorial_LiftStone` bajo el objeto original `_movingStone`, ajustado al volumen 3,6 × 2,8 × 0,7 m. El padre canónico sigue subiendo +Y2,2 m por TutorialExitGate. **No está marcado static**, ni lo están sus hijos.

El paso Interact real abre esa piedra elevable mediante raycast (`Abrir paso`); no recoge una vasija. Se conserva esa lógica en lugar de cambiar mecánicas para cumplir una preferencia de prop. La salida sigue siendo el mismo trigger, sin nueva puerta o puzzle. La baliza cálida existente se conserva y el camino continúa visualmente hacia selva, con vegetación y fragmentos discretos.

## Suelo y relieve

Los tres renderers de suelo/camino reciben materiales URP/Lit derivados del material de tierra R4. UV de cubo con repetición proporcional a dimensiones (aproximadamente un tile cada 3 m), normal y MetallicSmoothness con mismo tiling. Se aplica una variación de color suave para orientar, sin flechas. Los límites rectos de la superficie funcional se disimulan con rocas, plantas y hojarasca.

Un plano visual adicional de 80 × 110 m a Y=−0,04 m extiende el fondo fuera del suelo funcional, sin collider y sin modificar cotas jugables. No es un Terrain reconstruido ni una nueva superficie transitable. El relieve se sugiere mediante bases de árboles, rocas y bordes; no se introducen rampas o cambios de topología del CharacterController. No hay coincidencia coplanar entre la extensión y el suelo funcional.

## Materiales y texturas

Se conservan materiales URP/Lit validados y mapas reales. BaseColor sRGB; Normal con Texture Type Normal Map/lectura lineal; mipmaps habilitados. Sin mapas ficticios. Metallic=0 para orgánicos/piedra; smoothness constante cuando el recurso no posee Roughness, o alpha del mapa MetallicSmoothness preparado como 1−Roughness en el kit/pipeline. No se vuelve a invertir un mapa ya empaquetado.

22 materiales usados en el entorno vestido y 37 texturas por identidad de Unity. Algunas texturas de los adaptadores coinciden en contenido con recursos ambientales pero conservan GUID validados propios del kit; no se duplica el kit completo. 34 texturas son 2K, dos normales de árbol son 1K y la normal de `VEG_HelechoTropical_02` es **4K preparada en origen**. Se conserva esa excepción sin subir resolución. La lista exacta queda en el reporte de Editor. Los cuatro materiales de superficie derivados habilitan instancing; no se modifican materiales de otras escenas.

## LOD, sombras y colliders

Se conservan los LOD y umbrales de los prefabs aprobados; no se regeneran mallas. Hojarasca tiene dos niveles y los demás ambientales los niveles publicados, algunos con conteos repetidos que se mantienen por integridad. Los tres adaptadores pequeños tienen un único nivel, sin LOD artificial. 111 LODGroups en las 114 instancias: árboles 20, arbustos 24, plantas/hojarasca 42, rocas 18 y muros/columnas 7. Dintel/adaptadores completan el recuento.

Todos los colliders de las nuevas copias de prefabs se conservan **deshabilitados**, incluidos MeshColliders diagnósticos. Hay **0 colliders artísticos habilitados**. Los 34 colliders funcionales originales de escena permanecen; la prueba runtime crea además su query collider deshabilitado de cámara, como antes. La configuración de todos los colliders y scripts originales se contrasta contra `TutorialEnvironmentBaseline.json` por identificadores estables. Se excluyen únicamente caches transitorios de punteros nativos/TMP y el layout derivado de RectTransform; los bloques YAML originales de UI se auditan separadamente.

Sombras de hierbas, plantas, helechos, hojarasca y arbustos desactivadas en las nuevas copias de prefabs. Árboles/rocas/ruinas conservan sombras; no se desactivan globalmente. El recuento de 131 renderers con capacidad de sombra incluye todos los LOD preparados, no 131 casters simultáneos. Objetos decorativos usan flags Occluder/Occludee Static cuando procede; no se marca static Player o la piedra móvil. No se promete occlusion culling horneado ni GI baked: no se ejecutó bake.

## Jerarquía

```text
TUT_ExplorerBasics/Environment/
  [suelo, obstáculos, triggers y salida originales]
  ENV_TutorialArt/
    Terrain/ForestFloor_Extension
    Vegetation/
      Trees
      Bushes
      GroundCover
    Rocks
    Ruins
    Props
    Background
```

Los adaptadores funcionales son hijos visuales de cada proxy original, deliberadamente fuera del root decorativo, para seguir su movimiento/autoridad. `Props` se reserva como categoría ordenada; no contiene objetos nuevos sueltos. Los 20 cilindros proxy se conservan con Renderer oculto y colisión original, tal como exige el alcance.

## Iluminación y atmósfera

La luz direccional existente cambia de Euler (42;−28;0), intensidad 1,15 y color (1;0,88;0,67), a **(48;−34;0), intensidad 1,25, color (1;0,91;0,76)** y sombras suaves. Ambiente Trilight: cielo (0,46;0,57;0,57), ecuador (0,29;0,39;0,32), suelo (0,18;0,22;0,16). Fog lineal ligera, inicio 28 m, fin 72 m, color (0,29;0,38;0,34), con sistema integrado de Unity/URP. No se añade solución volumétrica cara ni luz duplicada.

Warm exit beacon existente no se mueve ni altera. Volumes y perfiles se conservan, sin nuevo Bloom ni paquetes. La legibilidad del personaje y la UI se revisa en capturas reales; la aceptación artística humana sigue pendiente.

## Authoring y validación

- `Tikal/Authoring/Apply Tutorial Environment Art Pass`: actualiza copias/prefabs, coloca por seed, monta hijos visuales, configura suelo/iluminación y valida. Repetirlo conserva escena y prefabs byte a byte, sin duplicados.
- `Tikal/Validation/Validate Tutorial Environment Art Pass`: escena actual, componentes/colliders/spawn/anchors originales, pasos, referencias, root único, colocaciones, URP/texturas/normales/mipmaps/LOD, ausencia de Missing Scripts y high-poly, AudioListener, configuración FPS/TPS y destinos GameFlow.
- `Tikal/Validation/Run Tutorial Environment Play Tests`: Editor normal, persistencia temporal aislada, gamepad/teclado virtuales, controlador de producción y física real.

El authoring histórico `Build Explorer Tutorial` reconstruye blockout y **no debe ejecutarse sobre esta escena vestida**. El nuevo comando es el de mantenimiento de esta feature; no se altera lógica ni el generador histórico.

Las pruebas no invocan Complete de pasos. Recorren Move, Look, Jump, Crouch, Sprint, Interact y Exit con input virtual; tras Look se restablece orientación hacia delante para el trayecto determinista. Se verifica raycast real y evento de interacción, elevación/clearance y transición natural a Level01, tanto en TPS como FPS. Intro→Tutorial se ejercita mediante skip real UI.Cancel; la reproducción natural completa de Intro ya tiene su prueba de la feature previa y no se repite ni se modifica aquí. Axel/Mario se comprueban con su Avatar/controlador compartido y visibilidad FPS/TPS.

Capturas y resultados se guardan en `Temp/TutorialEnvironmentArtPass`, con copia externa de evidencias finales. Las medidas de triángulos visibles usan frustum y LOD seleccionado por tamaño proyectado, sin occlusion culling ni pases de sombra; son estimaciones explícitas, no contadores GPU. Los tiempos de fotograma del Editor excluyen warm-up inicial y cinco frames tras capturas; no prueban el rendimiento standalone ni eliminan stutter por shader compilation/Editor.

## Límites y aceptación pendiente

Los recursos provienen de derivados optimizados con validación histórica, cuya geometría/estilo tiene límites existentes; no se regeneraron ni maquillaron con modelos source. La vegetación es estática, sin viento nuevo. El acabado de sendero usa el suelo funcional plano y aún puede leerse su modularidad; no se promete un terreno orgánico final. La normal 4K y transparencias/overdraw de vegetación requieren perfilado representativo. No hay benchmark anterior comparable para atribuir coste incremental o declarar cero stutters.

La UI y Movement Debug preexistente permanecen. Las capturas durante cargas iniciales pueden mostrar transitorios de shaders del Editor; no son prueba de cambio de materiales del personaje. No se cambian personajes para corregir un frame diagnóstico. El playtest humano A–H y la aceptación visual final no se sustituyen por checks automatizados. Revisar cámara lateral libre, assets flotantes/enterrados, límites, repetición, alpha/overdraw y sombras durante recorrido lento.

## Resultados de la ejecución final

**861 comprobaciones Editor y 45 comprobaciones Play Mode aprobadas**, cero errores capturados durante la ejecución final. Authoring repetido con escena y prefabs idénticos. Hubo intentos de desarrollo conservados en los logs: el verificador inicial asumía máximo2K para todas las texturas; la comparación de firmas debía excluir punteros nativos/TMP y layout derivado; un intento de skip agotó una espera fija de frames. Se corrigieron las sondas, sin cambiar lógica de juego ni ocultar intentos anteriores.

114 colocaciones: 20 árboles, 24 arbustos, 42 plantas/hojarasca, 18 rocas, 8 piezas de ruina (incluido dintel) y 2 adaptadores de salto/salida. 111 LODGroups, 22 materiales usados, 37 texturas. Geometría de todas las instancias sumando LOD0: **759.671 triángulos**; suma de todos los niveles almacenados: **1.261.627**. Estos totales no son geometría visible simultánea. Mayor mesh: VEG_ArbolTropical_04_Mesh000, **25.718 triángulos**; mayor textura por dimensiones: normal del helecho **4096×4096**. Cero nuevos colliders artísticos habilitados; 34 funcionales originales.

| Captura | Triángulos visibles estimados | Draw calls Editor en ese frame | Distancia cámara-target m |
|---|---:|---:|---:|
| TPS_01_Start | 379501 | 164 | 3.42 |
| TPS_02_Jump | 387655 | 176 | 3.42 |
| TPS_03_CrouchExit | 342957 | 160 | 3.46 |
| TPS_04_Sprint | 317013 | 144 | 3.42 |
| TPS_05_Interact | 272043 | 123 | 3.42 |
| TPS_06_Exit | 270243 | 120 | 3.42 |
| FPS_01_Start | 391462 | 166 | 0.22 |
| FPS_02_Jump | 378011 | 165 | 0.22 |
| FPS_03_CrouchExit | 358790 | 145 | 0.55 |
| FPS_04_Sprint | 276586 | 133 | 0.22 |
| FPS_05_Interact | 269964 | 117 | 0.22 |
| FPS_06_Exit | 273514 | 119 | 0.22 |

La estimación final de frustum+LOD oscila **269.964–391.462 triángulos**; algunos encuadres iniciales superan el objetivo conceptual350k en aproximadamente12%. El objetivo no era un límite rígido y se documenta la desviación. La vista inicial anterior a alejar árboles alcanzaba ~450k; el nuevo fondo concentra composición tras salida y reduce el peso cercano. No se fuerza una reducción geométrica de fuentes o de sus LOD para maquillar cifras.

Tiempos reales de frames de la segunda prueba: **media48,596ms, p9556,628ms**. No son GPU timings, no separan CPU/GPU, incluyen carga del Editor y el instrumento, aunque excluyen warm-up y cinco frames tras capturas. No equivalen a benchmark standalone ni a coste incremental atribuible a estos assets. Es un **riesgo pendiente de perfilado representativo**: no se certifican60FPS ni ausencia de stutter. Draw calls observadas en las12capturas:117–176; incluyen escena/player/UI y pases según Editor. La distancia TPS del boom se mantuvo en~3,42–3,46m en las capturas del trayecto, sin compresión sostenida introducida por decorados.

Se ejecutó Intro(skip UI.Cancel)→Tutorial vestido, recorrido completo Dwight TPS→Level01, recorrido completo Dwight FPS→Level01 y alternancia de Axel/Mario en ambos modos. Progreso y ajustes de prueba aislados, sin escritura sobre guardados del usuario. Se inspeccionaron capturas reales del inicio, obstáculos, interacción y salida; el material del personaje en capturas finales se ve correctamente. No se declara playtest humano ni aceptación artística final.

Auditoría de protección: **991 archivos rastreados preexistentes sin cambios**, único archivo preexistente modificado la escena Tutorial. Cero objetos originales de escena eliminados; scripts, colliders, prefab overrides y bloques RectTransform/UI originales conservados. Modelos/texturas importados y fuentes seleccionadas comprobados por SHA256; kits y biblioteca externa intactos. No se integró Level01 ni puzzles.

## Path Containment & Tutorial Guidance

Segunda pasada sobre `feature/tutorial-environment-art-pass`, desde `f1f51ac`. No rehace el Art Pass ni altera los seis pasos, Exit, Input, PlayerMovement, PlayerLook, dimensiones globales del CharacterController, selección, FPS/TPS, Intro, GameFlow o Level01. La única clase runtime nueva es `TutorialOutOfBoundsVolume`, local a Tutorial. No hay imports nuevos, nuevos materiales, cambios de prefabs compartidos ni biblioteca duplicada.

### Contención visual y física

Se recolocan los 24 arbustos y 18 rocas existentes hacia los bordes, con variación determinista de posición. Los arbustos existentes aumentan su escala base por 2,1 en la variante pequeña izquierda y 1,65 en la mediana derecha, conservando sus proporciones y variaciones originales: el borde gana continuidad sin añadir objetos ni mallas. Se añaden diez masas de `VEG_ArbustoTropical_02/03`, con tamaños aproximados 3,2 × 2,1 × 3,2 m y rotaciones variadas, intercaladas en ambos lados; árboles y plantas existentes completan primer plano, borde y fondo. La seed adicional es 210622. El camino de 4,6 m queda despejado; la cara interior de los límites laterales está en X=±6,5 m. No se añaden colliders a plantas. Las nuevas masas desactivan sombras mediante overrides de escena, sin editar sus prefabs.

`Environment/GameplayBoundaries` contiene **once BoxColliders sin Renderer**:

- Cuatro segmentos laterales de 1 × 4,2 × 29,5 m, centros X=±7, Z=8,5/37,5. Solapan 0,5 m y cubren Z=-6,25…52,25. Altura 4,2 m frente a salto configurado de 1,2 m; se comprueba también salto físico real.
- Un cierre trasero, centro (0; 2,1; -5,8), tamaño 15 × 4,2 × 1 m.
- Dos alas delanteras, centro (±5,25; 2,1; 51,5), tamaño 4,5 × 4,2 × 1 m. Conservan una abertura central de 6 m tras el Exit canónico; no se encierra completamente el recorrido.
- Dos alas de ruina junto a Crouch, centro (±4,4; 1,4; 19), tamaño 4,4 × 2,8 × 1,2 m, cerrando el bypass lateral. Se reutilizan dos muros visuales existentes, conservando sus nombres históricos `Wall_Jump_Left` y `Wall_Sprint_Right`.
- Dos colliders de pilares Interact, centro (±2,25; 2,45; 36), tamaño 0,7 × 4,9 × 0,7 m.

Se **deshabilitan únicamente los veinte CapsuleColliders de `Vegetation proxy 0…19` de esta escena**, cuyos renderers ya estaban ocultos. Sus volúmenes aislados quedaban por delante de los bordes reorganizados y comprimían la cámara TPS. La contención continua los sustituye; no se borran sus objetos ni se cambian sus demás propiedades. El validador exige exactamente esa excepción, manteniendo intactas las firmas de los restantes componentes originales. El suelo, triggers, colliders de salto y puerta conservan sus parámetros locales. Solo el dintel incorpora la elevación y el margen local de colisión descritos abajo.

### Safety recovery

Un único `Environment/TutorialOutOfBoundsVolume`, centro (0; -6; 23), trigger 100 × 10 × 120 m, contiene la protección inferior. La comprobación temprana de posición también recupera teleports que salten el trigger: Y<-2, X fuera de [-7,5; 7,5] o Z fuera de [-6,8; 51]. El límite frontal está después del trigger de salida; si por un fallo no se realiza la transición, no se permite seguir indefinidamente hacia el borde del suelo.

El último punto seguro se registra solo con cápsula apoyada, X entre ±6,1, Z entre -5 y 46, Y entre -0,1 y 1. Si todavía no existe, usa spawn (0; 0,05; 0). Recuperar limpia foco interactivo, reposiciona, resetea movimiento/animación transitorios, restaura yaw/pitch y cámara mediante las API existentes y sincroniza física. No usa CheckpointManager, persistencia ni llamadas a TutorialSequenceController; no hace avanzar pasos ni marca TutorialCompleted. No teletransporta por retroceder dentro del recorrido.

### Pilares Interact

Dos instancias de `PFB_ARQ_ColumnaMaya_01`, conservando sus tres LOD (3999 / 1799 / 599 triángulos). Bases visuales en (±2,25; 0; 36), dimensiones ajustadas a 0,75 × 4,9 × 0,8 m; escala aproximada (1,022; 1,633; 1,073) respecto a bounds LOD0 de la pieza. La altura enmarca también la piedra elevada. Son hijos fijos de `ENV_TutorialArt/PathContainment`, fuera de `Lift stone`; no se mueven con ella. La pared conserva `_movingStone`, raycast/prompt, desplazamiento +Y2,2 y velocidad originales. La luz, baliza y ruta Exit se mantienen.

### Crouch: medidas y apertura

`TutorialCrouchMeasurements.json` conserva una medición real de los tres personajes con su Avatar/controlador/animaciones de producción. Se muestrean vértices skinned por CPU, incluyendo cabello/ropa; no se usan bounds de Renderer inflados. Y se expresa en mundo, con Player sobre suelo de aproximadamente Y=0,1. Crouch Walk se muestrea durante dos segundos simulados; no garantiza todas las variantes/poses futuras.

| Personaje | Standing visual top Y | Crouch Idle top Y | Crouch Walk top Y |
|---|---:|---:|---:|
| Dwight | 1,8926 | 1,5204 | 1,5858 |
| Axel | 1,9106 | 1,5010 | 1,5682 |
| Mario | 1,6744 | 1,3531 | 1,4099 |

La cápsula real, común a los tres, mide **1,8 m / center Y=0,9** de pie y **1,0 m / center Y=0,5** agachada, escala sin modificar. Tops medidos en mundo: 1,9 y 1,1 m. Dwight determina el máximo visual crouched: **1,5858 m** (aproximadamente 1,486 m por encima del origen apoyado).

Una apertura inicial Y=1,67 daba 8,42 cm sobre el Crouch Walk de Dwight, pero la prueba de agacharse mientras se empuja contra la viga detectó penetración transitoria. Se midieron también picos de entrada dentro de la huella del techo: Dwight 1,7843, Axel 1,7792 y Mario 1,5852 m en ese piloto. Elevar conjuntamente collider y visual a 1,81 no resolvió la entrada de Axel (pico 1,8929 m); las poses asentadas solas no son un criterio suficiente. Esos intentos se conservan en la evidencia externa y no se consideran aprobados.

La altura visual se deriva de `ceil(max(máximo crouch asentado + 0,075; máximo de entrada piloto + 0,06) × 100) / 100`: **underside visual Y=1,85 m**, frente a 1,325 anterior. El dintel visual sube **0,525 m**, centro Y=2,075, grosor 0,45 m. Sobre el camino con top Y=0,07 quedan **1,78 m visuales**.

Se utiliza el ajuste local visual/collider permitido por el encargo: **underside físico Y=1,81 m**, 4 cm bajo el arte; cara superior común Y=2,30; collider 4,2 × 0,49 × 1,44 m, con margen delantero/trasero de 12 cm respecto al techo visible de 1,2 m de profundidad. No se oculta esta diferencia: evita iniciar el movimiento bajo la malla antes de que termine la transición de animación. El collider standing alcanza aproximadamente Y=1,9, por encima del umbral físico. La apertura física sobre el camino es 1,74 m. Los apoyos visuales se alargan hasta el techo, sin alterar sus colliders originales.

La cápsula global y todas las animaciones permanecen intactas. El validador compara la firma completa del collider original salvo `center/size`, que comprueba contra el ajuste exacto autorizado; también comprueba límites visuales y físicos por separado. No se da por aprobado el ajuste hasta repetir standing/crouch y medir los vértices reales con los tres personajes.

### Herramientas y alcance de validación

Los menús canónicos siguen siendo `Tikal/Authoring/Apply Tutorial Environment Art Pass` y `Tikal/Validation/Validate Tutorial Environment Art Pass`. El helper `TutorialGuidanceAuthoring` añade composición, límites, pilares y safety por nombres estables; ejecutar dos veces exige escena y prefabs idénticos. El validador incluye continuidad, primitivas, número único de objetos, proxies deshabilitados, pilares fijos y clearance derivado del informe de medidas, además de las comprobaciones existentes de referencias, Missing Scripts y shaders URP soportados.

`Tikal/Validation/Run Tutorial Environment Play Tests` incorpora `TutorialGuidancePlayTests`: caída/OOB con paso/estado/save intactos, movimiento real hasta safe point, ocho intentos laterales con sprint y salto, TPS paralelo y bajo presión contra el borde, cápsula standing rechazada y crouched aceptada con cada personaje, y comprobación CPU de vértices durante la transición hacia crouch sin preasentar la pose. Después recarga Tutorial y ejecuta el recorrido canónico completo TPS y FPS con prompt/raycast, elevación y Exit reales. Usa gamepad virtual, Editor interactivo normal y guardados aislados; no utiliza batchmode. Las desactivaciones temporales de secuencia durante pruebas de explotación no se serializan.

Movement Debug sigue condicionado por `UNITY_EDITOR || DEBUG`; no se elimina ni se amplía. Los checks instrumentados y las capturas no sustituyen el playtest humano A–J, especialmente percepción de barreras, repetición vegetal, cámara libre y estética. La aprobación humana de la primera pasada no se extiende automáticamente a esta segunda.

### Resultado final de las pruebas

**935 checks Editor y 69 checks Play Mode aprobados**, cero errores capturados en la ejecución final. Authoring dos veces: escena y prefabs byte-idénticos. Se observaron los tres personajes durante la transición real, sin preasentar crouch:

| Personaje | Máximo visual Y dentro del techo durante entrada/paso | Margen mínimo visual | Standing bloqueado Z | Crouched llega a Z |
|---|---:|---:|---:|---:|
| Dwight | 1.7839 | 6.61 cm | 17.940 | 21.549 |
| Axel | 1.7792 | 7.08 cm | 17.940 | 21.549 |
| Mario | 1.5807 | 26.93 cm | 17.940 | 21.549 |

Ningún vértice medido penetra el techo visual en estas entradas. Dwight y Axel cumplen el margen deseado de 5–10 cm incluso durante la transición; Mario conserva mayor holgura. No se garantiza cualquier futura pose, rig o animación sin repetir la prueba. Las capturas muestran los personajes bajo el dintel; la lectura MUÉVETE durante estas sondas es intencionada: secuencia deshabilitada solo en diagnóstico y recargada antes del recorrido canónico.

Se verificó un safe point distinto del spawn mediante movimiento apoyado real, caída forzada y retorno sin cambiar referencia/estado de step ni el JSON de guardado aislado. Ocho intentos de salida: izquierda/derecha en Z=4/15/28/44, con sprint y salto; pico real de pies Y=1,117, cero recuperaciones de safety durante los intentos de boundary. La protección no sustituye la barrera física.

TPS paralelo al borde: boom mínimo 3,421 m, variación relativa máxima medida 0. Al presionar deliberadamente contra la pared: boom mínimo 0,946 m y variación máxima por frame 0,193 m; cero saltos mayores de 0,5 m. Se acepta la compresión de colisión al contacto, con cámara fuera del personaje; no se declara distancia libre constante contra una pared. El recorrido central conserva boom 3,42–3,46 m. FPS, raycast/prompt de Interact, pared móvil y Exit pasan sus checks en ambos recorridos. La transición existente carga Level01, sin modificar ningún archivo de esa escena.

### Comparación de coste

| Métrica | Primera pasada | Segunda pasada |
|---|---:|---:|
| Placements | 114 | 126 (+10,53%) |
| LODGroups | 111 | 123 |
| Suma de LOD0 instanciados | 759671 | 808359 (+6,41%) |
| Suma de todos los niveles almacenados | 1261627 | 1348917 |
| Shadow-casting renderers (todos los LOD, no simultáneos) | 131 | 137 |
| Colliders habilitados de escena | 34 | 26 (14 restantes + 11 boundaries + 1 safety) |
| Colliders artísticos habilitados | 0 | 0 |
| Materiales / texturas | 22 / 37 | 22 / 37 |
| Media Editor instrumentada ms | 48,596 | 49,082 |
| P95 Editor instrumentado ms | 56,628 | 58,282 |

| Vista comparable | Triángulos antes | Triángulos después | Delta |
|---|---:|---:|---:|
| TPS_01_Start | 379501 | 418597 | +10.30% |
| TPS_02_Jump | 387655 | 418308 | +7.91% |
| TPS_03_CrouchExit | 342957 | 376335 | +9.73% |
| TPS_04_Sprint | 317013 | 340408 | +7.38% |
| TPS_05_Interact | 272043 | 299807 | +10.21% |
| TPS_06_Exit | 270243 | 298007 | +10.27% |
| FPS_01_Start | 391462 | 426208 | +8.88% |
| FPS_02_Jump | 378011 | 416357 | +10.14% |
| FPS_03_CrouchExit | 358790 | 381848 | +6.43% |
| FPS_04_Sprint | 276586 | 299234 | +8.19% |
| FPS_05_Interact | 269964 | 295140 | +9.33% |
| FPS_06_Exit | 273514 | 296840 | +8.53% |

El rango pasa de **269964–391462** a **295140–426208** triángulos visibles estimados. Los doce encuadres aumentan entre **6.43% y 10.30%**, por debajo del objetivo orientativo de +15%. Draw calls observadas: 117–176 antes, 120–177 después. La geometría se estima por frustum y LOD, sin occlusion ni pases de sombras; posiciones equivalentes de la ruta con pequeñas diferencias de movimiento, no una captura GPU fija.

Mismo Editor normal, instrumentación de ruta, captura a 30 fps simulados, warm-up de 45 frames y exclusión de cinco frames tras cada screenshot. Las sondas CPU de deformación/boundaries ocurren fuera de la ventana de timing. La media cambia +1.00% y P95 +2.92%; no permiten atribuir una mejora ni certificar rendimiento standalone/60FPS. Persisten los límites de Editor, instrumentación, shader/cache y alpha overdraw ya documentados.

Evidencia final y fallos de desarrollo: `D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/_TutorialGuidance_20261021`. Playtest humano A–J pendiente para esta segunda pasada; no se inventa aprobación humana. La auditoría por hashes y bloques YAML verifica el alcance antes del commit.
