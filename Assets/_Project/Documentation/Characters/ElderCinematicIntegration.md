# Anciano real — Intro Ancient Map

Unity 6000.6.3f1 (Unity 6.6), URP. Integración exclusivamente cinematográfica en `feature/elder-intro-integration`, sobre develop `a0f06e3055ae34ce886c69b7173d89a9d35c14f0`. No incorpora al Anciano al catálogo ni altera personajes jugables, movimiento, input, cámaras gameplay o persistencia.

## Source master y modelo

Fuente leída: `D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Materiales_Tikal_El_Explorador/Personajes/CHR_Anciano`.

Incluye CHR_Anciano.zip, REFERENCE_ANCIANO.jpeg y siete FBX de animación. No hay documentación adicional ni VO. El ZIP contiene `tripo_convert_c4d91c9b-099b-4a99-a5e1-589cfc5effa6.fbx` y `CHR_Anciano_basecolor.JPEG`. SHA-256 del ZIP: `6f05352897c66e44f8e4c314bd801812bf1ac6d66a23081c67ae5ad29ec00a29`.

Se entregan únicamente el modelo, BaseColor y los dos FBX seleccionados. Las cinco copias utilizadas para audición y descartadas se retiraron del proyecto; ningún original fue modificado. `Elder/ElderSourceAudit.json` documenta todos los archivos originales, sus tamaños, hashes y datos FBX.

FBX binario 7400, una malla, un material, 5.335 vértices de control y **9.990 triángulos**, confirmado por Unity. El importador produce 13.800 vértices al separar atributos. Rig Mixamo de 65 huesos, 65 clusters; sin vértices sin peso, máximo cuatro influencias, error máximo de normalización 5,2154×10^-8. Una bind pose de 67 nodos; TransformLink coincide exactamente con las matrices de bind de sus huesos.

## Avatar, escala y orientación

Avatar propio del Anciano: Humanoid / Create From This Model, válido y humano. Se generó primero la escala en Generic y después una configuración Humanoid limpia. Configure Avatar se inspeccionó: T-pose correcta y asignaciones verdes, incluidos tronco, brazos, piernas, manos, pies, UpperChest y dedos; no se necesitó Enforce T-Pose. La última importación de modelo y clips terminó sin Rig Errors.

| Medida | Valor |
|---|---:|
| Altura inicial Unity | 0,999511659 m |
| Scale Factor inicial | 1 |
| Scale Factor final | 1,720840334892273 |
| Altura humana de pie | 1,720000029 m |
| Avatar humanScale | 0,977822244 |
| Prefab root Scale | 1,1,1 |
| Prefab root Rotation | 0,0,0 |

FBX declara UnitScaleFactor=100 cm/unidad. La geometría local usa Z vertical y los nodos del exportador la convierten a Y; el resultado importado y retargeteado es +Y Up / +Z Forward. No se editaron huesos ni fuentes para compensar orientación.

Los FBX de animación tienen otro padre de Hips y otras proporciones. **No usar Copy From Other Avatar con el Avatar del modelo**: esa configuración produce errores de jerarquía y longitudes. Cada FBX seleccionado crea su Avatar de origen válido; Unity retargetea sus clips Humanoid al Avatar del Anciano. El modelo conserva su propio Avatar. Esto no requiere duplicar la malla ni modificar las animaciones originales.

## Material y prefab

`Art/Characters/Elder/Models/CHR_Elder.fbx`, `Textures/CHR_Elder_basecolor.jpeg`, `Materials/MAT_Elder_Body.mat`, `Animations/CHR_Anciano_Seated_Idle.fbx` y `Animations/Sitting Talking.fbx`.

BaseColor JPEG RGB 2048×2048, sRGB; no existen Normal, Metallic, Roughness ni AO. Shader Universal Render Pipeline/Lit, opaco, Metallic=0 y Smoothness=0,20. No se generaron mapas ni audio.

`Prefabs/Characters/CHR_Elder_Cinematic.prefab` contiene raíz, VisualRoot, modelo, SkinnedMeshRenderer y Animator con Avatar propio. Animator Controller=null, Root Motion=false, Always Animate. No tiene componentes de gameplay, cápsula, cámara, AudioListener ni CharacterVisualSpawner.

## Animaciones realmente disponibles

Las duraciones son medidas del FBX y confirmadas mediante los clips Unity. El FBX no declara explícitamente una intención loop; las opciones importadas se eligieron después de revisar las poses.

| Archivo original | Duración | Resultado de audición |
|---|---:|---|
| CHR_Anciano_Seated_Idle.fbx | 4,000 s | Elegido para Idle/Listen; loop habilitado. |
| Sitting Talking.fbx | 4,333333 s | Elegido por segmentos; loop fuente habilitado, Timeline fuerza Off en los recortes. |
| Hanging Idle.fbx | 2,333333 s | Descartado: postura de suspensión incompatible. |
| Looking Around.fbx | 6,333333 s | Descartado: personaje de pie. |
| Sitting Clap.fbx | 6,500 s | Descartado: aplausos excesivos para esta narrativa. |
| Sitting Dazed.fbx | 8,333333 s | Descartado: postura reclinada/aturdida incompatible. |
| Sitting Drinking.fbx | 15,200 s | Descartado: acción/contexto y desplazamiento incompatibles; no es una entrega. |

`Elder/ElderPilotValidation.json` contiene muestras de pelvis, cabeza, manos, pies y bounds de los siete clips. Las capturas de audición se generaron con skinning CPU de las matrices evaluadas, evitando imágenes desactualizadas del render de Edit Mode. La comprobación final utiliza además Play Mode real.

## Mapeo de actuación

| Función solicitada | Clip y uso |
|---|---|
| Seated Idle | CHR_Anciano_Seated_Idle, loop para cubrir los huecos. |
| Seated Talk | Sitting Talking, segmento 2,90–4,20 s ralentizado para una actuación sobria. Se excluye el gesto amplio sobre la cabeza. |
| Seated Listen | Fallback a Seated Idle durante las frases del Explorer. |
| Seated Look At Explorer | Fallback a orientación del anchor y cabeza presente en los clips sentados. No se usa el clip de pie ni se añade IK/head-look procedural. |
| Seated Give Map | Segmento inicial 0–0,40 s de Sitting Talking, donde la mano derecha se extiende; distribuido entre 65–71,45 s. **No existe Give Map específico**. |
| Seated Final Warning | Segmento suave 2,90–4,20 s de Sitting Talking entre 87–94,25 s; sin efectos, cambios de tono sobrenatural ni lip-sync. |

Se reutilizan archivos existentes; no se generan curvas corporales, animaciones nuevas, controladores de actuación complejos o paquetes. La calidad del gesto de entrega sigue siendo la de un fallback, sin agarre de dedos ni actuación de recepción específica del Explorer.

## Timeline y narrativa

Se conserva `Scenes/Cinematics/Intro_AncientMap.unity`, su director y `Timelines/Intro/TL_Intro_AncientMap.playable`. Duración global **97 s**. Se añaden solo dos Animation Tracks bajo Blocking: `Elder Seated Performance` y `Explorer Cinematic Visual`. Los tracks previos y los ocho roles de cámaras permanecen. Las regiones se mezclan durante 0,25 s, sin huecos que produzcan T-pose.

Actuación del Anciano: Idle hasta 48,25; preguntas 48–53,25; Listen 53–57,65; historias 57,4–65,25; ofrecer mapa 65–71,45; explicación 71,2–80,25; Listen/partida 80–87,25; warning 87–94,25; Idle final 94–97. Los solapamientos son blends, no cambios de texto ni de tiempo de cues.

Los **19 bloques de cues originales se conservan idénticos**, incluidos la narrativa, todos los subtítulos y el único AncientMapReceived a 71 s. No se reescribe ninguna línea. Los subtítulos son la autoridad mientras no exista VO.

## Posición sentada y helpers

ElderAnchor conserva posición `(2,3; 0,8; 12)` y su orientación original de 205° Y. HeadLookTarget y ChestLookTarget conservan sus objetos y se recolocan al modelo real; las cámaras correspondientes solo ajustan dirección de encuadre, manteniendo posición, FOV y ventanas de activación.

VisualRoot del prefab: localPosition `(0; -0,52; 0,30)`. El offset del AnimationPlayableAsset Idle es `(0; -0,04; 0,244)`; Talking utiliza cero. Son offsets estáticos de colocación por clip para alinear sus pelvis, sin edición corporal ni corrección por frame. La pelvis permanece aproximadamente a 0,90 m sobre el asiento proxy cuya superficie está en Y=0,80 m. El bloque de asiento original no se cambia.

Se conservan las piernas colgando según esos clips y ese asiento alto: el mínimo de geometría ronda Y=0,107 m en Idle y Y=0,287 m en Talking. No se declara contacto de ambos pies con el suelo; no es una pose de personaje de pie. Revisar artísticamente el contacto de ropa/muslos con el bloque de asiento durante la pasada ambiental posterior.

## Map handoff

AncientMap_Proxy conserva su GameObject, mesh proxy, Animator, track y cue conceptual. Es independiente de ambas mallas. Un ParentConstraint con dos fuentes usa las manos derechas del Anciano y del Explorer; sus pesos son animados en el **clip de mapa existente**, no en los clips corporales.

El Anciano sostiene el proxy; entre 70,35–71,00 s se mezclan donante/receptor. En 71 s el peso del Explorer llega a 1 y el cue AncientMapReceived se dispara una vez. El constraint sigue activo después para que el mapa viaje con el Explorer; desactivarlo devolvía el objeto a su pose de reposo y tapaba la cara del Anciano. Los pequeños offsets de agarre sitúan la superficie del proxy entre las palmas, sin deformar los esqueletos. El mapa conserva su independencia para una sustitución futura.

El encuadre CAM_MapHandoff usa la zona de manos y deja margen sobre los subtítulos. El prop se orienta longitudinalmente entre los actores. La geometría sigue siendo el proxy rectangular existente; no es el mapa artístico final. No se modifican inventario, progreso ni lógica de IntroSequenceController.

El track de posición existente del Explorer se conserva. Se deshabilita Remove Start Offset en los clips de blocking/mapa para respetar sus posiciones absolutas; la opción anterior sumaba cinco metros al Explorer. Sus claves de encuentro se acercan a `(1,3; 0; 10,9)` para permitir la entrega, sin ampliar la duración.

## Dwight como Explorer cinematográfico

Sustitución realizada bajo ExplorerAnchor/CharacterVisualRoot con una instancia del prefab Dwight, sin cambiar ese prefab ni el roster. Su Animator Controller se anula solo en la escena y se controla con los clips Idle/Walk ya existentes mediante Timeline. Conserva su propio Avatar. El wrapper recibe +0,04 m únicamente en esta escena, porque no hay cápsula física de gameplay.

No se añade un selector o spawner jugable: la Intro usa el Explorer canónico Dwight, sin reemplazar la selección guardada del usuario. HeadLookTarget y ChestLookTarget del Explorer se mantienen. El placeholder se conserva inactivo para trazabilidad.

## Herramientas, validación y límites

- `Tikal/Authoring/Apply Elder Cinematic Character`: actualiza la escena existente, prefab, tracks propios, bindings y constraint. Repetir no duplica tracks ni actores. Limpia poses temporales del muestreo antes de guardar y conserva la pose inicial del blockout y su volumen de audio original.
- `Tikal/Validation/Validate Elder Intro Integration`: modelo real, Humanoid, material, anchors/helpers, binding, clips, cámaras, mapa/cue, duración, Missing Scripts y ruta al Tutorial.
- `Tikal/Validation/Run Elder Intro Play Tests`: Editor interactivo, reproducción natural de los 97 segundos con saves aislados. Mide estabilidad sentada, evento único, pesos de entrega, encuadres, subtítulos y transición real a Tutorial; también compara bytes tras repetir authoring.

El antiguo `Build Ancient Map Intro` regenera el blockout original; para esta integración utilizar Apply Elder Cinematic Character, sin reconstruir el Timeline desde cero. Resultados medidos: `Elder/ElderEditorValidation.json` y `Elder/ElderPlayValidation.json` (consultar passed/checks/errors). Capturas temporales: `Temp/ElderCinematicPlay`.

La revisión automatizada y las capturas no sustituyen el playtest humano de actuación, continuidad de miradas, manos y ropa. No existe lip-sync, Give Map dedicado ni recibir objeto específico de Dwight. El entorno, asiento y mapa son proxies existentes; no se incorporan assets ambientales. VO, ambiente, música, mapa final y ajuste artístico del asiento se reservan para su fase posterior. No se cambia el sistema de personajes jugables ni sus clips/velocidades.

## Resultado registrado

28 comprobaciones Editor y 102 comprobaciones Play Mode aprobadas, sin errores. Se verificaron los 97 segundos completos, AncientMapReceived exactamente una vez, transición natural a TUT_ExplorerBasics y authoring repetido idéntico byte por byte. Capturas finales revisadas: materiales sin magenta, mapa visible sobre el área de subtítulos y rostro despejado durante Final Warning. La aceptación artística humana sigue pendiente.
