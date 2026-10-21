# Dwight: integraciÃ³n jugable Humanoid

Proyecto TIKAL: El Explorador. Unity 6000.6.3f1 (Unity 6.6), URP. Rama de trabajo `feature/character-dwight-integration`; base `34a96319f5e7440fe7361a1b8db5bfd1f4b2b7a8` coincidente con develop remoto al inicio.

## Identidad y arquitectura

| Slot | ID existente | Nombre | Estado |
|---|---|---|---|
| 1 | character.default | Dwight | Jugable |
| 2 | character.02 | Axel | Jugable |
| 3 | character.03 | Mario | Jugable |
| 4 | character.04 | DESCONOCIDO | Bloqueado, sin modelo |

No existe un quinto ID. `CharacterVisualAssets` conserva las referencias de Axel/Mario y aÃ±ade Dwight. `CharacterVisualSpawner` instancia el visual bajo el Player existente; `PlayerAnimationController.Attach` conecta su Animator al movimiento compartido. Se comparte exactamente `AC_ExplorerGameplay`, sus seis clips Humanoid y sus parÃ¡metros, sin overrides ni clips duplicados. Cada personaje utiliza su propio Avatar.

## Source master y auditorÃ­a

Fuente exclusivamente leÃ­da: `D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Materiales_Tikal_El_Explorador/Personajes/CHR_Dwight/CHR_DwightCortez_Base.zip`.

SHA-256 inicial: `706107cde9cdbf58f47beacee63ab2a7f65b4f843b2e1bff02d0e8c479cf9969`.

Contiene solo el FBX `tripo_convert_2c72a4d8-5a33-4d33-9507-8c7e8f219020.fbx` (1.726.604 bytes) y su BaseColor JPEG (4.095.788 bytes). No incluye documentaciÃ³n, clips, Normal, Roughness, Metallic ni otros auxiliares. Se copiaron Ãºnicamente esos dos archivos, byte por byte; los nombres internos del FBX se conservan. El material URP usa una referencia explÃ­cita a la textura externa importada, sin depender de conversiÃ³n automÃ¡tica de materiales FBX.

FBX binario 7400; una malla, un material, 21.319 vÃ©rtices de control, 23.445 polÃ­gonos (4.574 triÃ¡ngulos y 18.871 quads): **42.316 triÃ¡ngulos reales**, confirmado por Unity. Unity genera 39.367 vÃ©rtices tras separar atributos UV/normales.

## Rig, pesos y bind pose

65 huesos `mixamorig`, 65 clusters, 52 con influencias no vacÃ­as, una Skin, una BindPose con 67 nodos. No hay vÃ©rtices sin peso ni pesos negativos/no finitos. MÃ¡ximo cuatro influencias; desviaciÃ³n mÃ¡xima de la suma respecto de 1: 0,000000090. Los TransformLink coinciden exactamente con las matrices de los huesos en BindPose. Estas comprobaciones estÃ¡ticas no sustituyen la revisiÃ³n animada.

ImportaciÃ³n Humanoid con Create From This Model, Avatar propio vÃ¡lido y humano, cero Rig Errors durante su generaciÃ³n. 52 asignaciones: Hips; Spine; Chest=Spine1; UpperChest=Spine2; Neck; Head; hombros, brazos, antebrazos, manos; muslos, piernas, pies y dedos de ambos lados. La lista completa estÃ¡ en `Dwight/DwightPilotValidation.json`. Configure Avatar se inspeccionÃ³ en el editor: indicadores verdes y T-pose correcta; no fue necesario Enforce T-Pose. No se reutilizÃ³ el Avatar de Mario/Axel.

## Escala, orientaciÃ³n y pies

FBX UnitScaleFactor=100 cm por unidad; UpAxis=Y. La geometrÃ­a local usa Z vertical con una transformaciÃ³n de nodo -90Â° X; su bind pose mundial ya es Y vertical. No debe inferirse una correcciÃ³n adicional a partir de los vÃ©rtices locales aislados.

| Medida | Valor |
|---|---:|
| Altura inicial real en Unity | 0,914551258 m |
| Scale Factor inicial | 1 |
| Scale Factor final | 1,9900470972061157 |
| Altura final de geometrÃ­a | 1,819999814 m |
| Avatar humanScale | 1,027905703 |
| Prefab/model local scale | 1,1,1 |
| RotaciÃ³n raÃ­z y wrapper | 0,0,0 |
| DwightVisualOrientation local Y | **-0,02711736038327217 m** |

La escala se resolviÃ³ primero como Generic; despuÃ©s se creÃ³ un HumanDescription nuevo y el Humanoid. Esto evita conservar un Avatar calculado a otra escala. El movimiento retargeteado mira hacia +Z, con +Y arriba; no se necesitÃ³ yaw correctivo. Los nodos internos conservan las transformaciones del exportador.

La base de origen en bind pose estÃ¡ en Yâ‰ˆ0. Se ajustÃ³ Ãºnicamente el wrapper visual constante. La cÃ¡psula fÃ­sica compartida deja aproximadamente 0,04 m de separaciÃ³n; se incluyÃ³ en la mediciÃ³n, sin tocar CharacterController ni PlayerMovement. Las siguientes medidas manuales de PlayableGraph son relativas al root; para comparar con suelo fÃ­sico Y=0 se suma 0,04 m:

| Pose | Suela mÃ­nima relativa | MÃ¡xima | Media |
|---|---:|---:|---:|
| Idle | -0,040949 | -0,039180 | -0,040000 |
| Walk | -0,088517 | 0,010551 | -0,044075 |
| Run | -0,064349 | 0,090756 | 0,011826 |
| CrouchIdle (fase 0,25) | -0,049139 | -0,049139 | -0,049139 |
| CrouchWalk | -0,108158 | 0,010391 | -0,042489 |

Idle queda aproximadamente sobre el suelo. Walk puede penetrar puntualmente unos 4,85 cm; Run unos 2,43 cm; CrouchWalk unos 6,82 cm. La elevaciÃ³n de Run incluye su fase aÃ©rea. Se documenta contacto imperfecto y posible sliding del retarget; no se introdujeron IK, correcciones por frame, cambios de clips ni fÃ­sicas para disimularlo. La valoraciÃ³n de comodidad visual y contacto final requiere playtest humano.

## Material y prefab

`Art/Characters/Dwight/Models/CHR_Dwight.fbx`, `Textures/CHR_Dwight_basecolor.jpg`, `Materials/MAT_Dwight_Body.mat`. BaseColor JPEG RGB 4096Ã—4096, sRGB, max import size 4096. Shader **Universal Render Pipeline/Lit**, opaco, Metallic=0, Smoothness=0,25; ningÃºn Normal/Metallic/Roughness inventado. La resoluciÃ³n de origen se conserva; el coste final se perfilarÃ¡ en hardware objetivo.

`Prefabs/Characters/CHR_Dwight_PlayerVisual.prefab`: raÃ­z, wrapper DwightVisualOrientation y modelo con su Animator/SkinnedMeshRenderer. Solo componentes visuales; sin cÃ¡psula, movimiento, interacciÃ³n, cÃ¡mara, audio o input duplicados. Root Motion=false, Animator.speed=1. Walk mantiene 2,2Ã— y Run 1,5896648Ã—; velocidad fÃ­sica Walk=4 m/s, Sprint=6,5 m/s.

## Retarget y deformaciones

Se muestrearon Idle, Walk, Run, Jump, CrouchIdle y CrouchWalk con los clips existentes. Se inspeccionaron capturas URP de las poses y despuÃ©s capturas de Play Mode del Player real. No se observaron inversiones de rodilla, colapsos, estiramientos graves, fugas de pesos ni torsiones severas en las muestras revisadas.

| Zona | ValoraciÃ³n visual de las muestras |
|---|---|
| Hombros | GOOD |
| Axilas | ACCEPTABLE: pliegues de camisa |
| Codos | ACCEPTABLE: compresiÃ³n de mangas dobladas |
| MuÃ±ecas | GOOD |
| Cadera | GOOD |
| Rodillas | GOOD |
| Tobillos | GOOD |
| Pies | ACCEPTABLE: contacto de retarget descrito arriba |

No equivale a una revisiÃ³n artÃ­stica exhaustiva de todos los frames ni a una prueba humana WASD.

## SelecciÃ³n, bloqueo y persistencia

El slot 1 conserva `character.default`; nuevas partidas siguen la semÃ¡ntica default existente. No hay migraciÃ³n que sustituya Axel o Mario guardados. El catÃ¡logo distingue ID registrado de ID seleccionable mediante IsLocked/IsSelectable. El selector desactiva la cuarta tarjeta y muestra DWIGHT, AXEL, MARIO y DESCONOCIDO [BLOQUEADO], usando el estado Disabled de MenuSelectableVisual y evitando depender de un glifo emoji no disponible.

Select y Confirm rechazan IDs bloqueados. El setter de SelectedCharacterId rechaza slot 4 preservando la selecciÃ³n jugable previa; un save antiguo con slot 4 se normaliza a Dwight al cargar. El spawner tambiÃ©n ignora solicitudes directas bloqueadas. No se inventÃ³ condiciÃ³n de desbloqueo ni un save system nuevo.

## Herramientas y evidencias

- `Tikal/Authoring/Apply Dwight Gameplay Character`: promociona el prefab piloto validado, configura sus referencias y registra Dwight; idempotente, sin reconstruir Avatar ni reescribir controller/clips.
- `Tikal/Validation/Validate Dwight Gameplay Character`: roster, bloqueo, propios Avatar/controller, material, escala, componentes, referencias, clips y escenas.
- `Tikal/Validation/Run Dwight Gameplay Play Tests`: Editor interactivo, frames reales con simulaciÃ³n fija a 60 Hz; restaura scene setup y timing al terminar. Usa saves aislados en Temp del sistema, sin cambiar la elecciÃ³n real del usuario.
- `Dwight/DwightSourceAudit.json`, `DwightPilotValidation.json`, `DwightEditorValidation.json`, `DwightPlayValidation.json`: resultados medidos; consultar passed, checks y errors.
- Capturas temporales en `Temp/DwightGameplayValidation`; no se generan escenas de prueba persistentes.

La validaciÃ³n Play Mode cubre Player real, locomociÃ³n compartida, Jump/Crouch, FPS/TPS, guardado/reapertura del panel para los tres personajes, rechazo de slot 4, cambio entre Avatars/materiales y ausencia de visuales residuales, Tutorial, Level01, Journal, Pause/Settings, Door mediante el resolver de Interact, un giro del puzzle mural y Restart From Checkpoint. La comprobaciÃ³n de SecretCommand es de configuraciÃ³n y rechazo cuando no aplica; no representa completar ese comando ni todos los puzzles. False Path y la lÃ³gica de los puzzles se conservan; su aceptaciÃ³n completa sigue pendiente de recorrido humano. No se modificÃ³ la lÃ³gica de esas mecÃ¡nicas, escenas ni cinemÃ¡tica.

## LÃ­mites y aceptaciÃ³n

La importaciÃ³n y el retarget son aptos para el sistema compartido. El contacto imperfecto de pies y los pliegues de ropa son limitaciones menores documentadas para playtest; Foot IK podrÃ­a evaluarse globalmente despuÃ©s para los tres personajes. No se prometen FPS ni rendimiento sin perfilado en equipo objetivo. No se tocaron Intro_AncientMap, Timeline, Anciano ni AncientMap; no se realizÃ³ merge.

## Resultado de verificación

27 comprobaciones de assets/roster en Editor y 179 comprobaciones Play Mode, incluyendo las rutas Main Menu → Dwight → Tutorial, Main Menu → Axel → Tutorial y Main Menu → Mario → Level01. Los JSON entregados contienen el resultado de la última ejecución, sus checks y errores; son evidencia automatizada, no aceptación humana. La suela Idle medida en el Player físico quedó en -0,0006013624370098114 m frente a Y=0.

Clasificación de preparación: **B — READY WITH MINOR FIXES** por contacto de pies y pliegues menores, sin rework de rig requerido. La integración jugable utiliza el sistema compartido y queda lista para el playtest humano; este documento no declara completadas pruebas humanas ni el recorrido íntegro de puzzles/False Path.
