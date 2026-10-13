# Mario — integración gameplay

Estado: integración materializada en feature/character-mario-integration; validación automatizada Editor/Play Mode y revisión de capturas realizadas. El playtest humano completo permanece pendiente. No modifica Intro_AncientMap, Timeline, Elder, AncientMap, puzzles, input, formato de guardado ni ajuste de cámara TPS.

## Fuente y auditoría

SOURCE MASTER, solo lectura: `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Materiales_Tikal_ElPortal\Personajes\CHR_Explorer`.

Se inspeccionaron los 44 archivos, documentación, scripts, datos serializados, tamaños y SHA-256 antes de copiar. Se copiaron únicamente un FBX, una textura y seis .anim, con sus .meta tras comprobar colisiones GUID. No se copian el prefab externo, su controlador ni ExplorerMotor/Input/Ground/Camera/Animator: duplicarían locomoción o dependencias del motor externo.

Modelo: `Art/Characters/Mario/Models/CHR_Explorer.fbx`, 2,536,972 bytes. Humanoid, Create From This Model. Unity confirmó Avatar CHR_ExplorerAvatar válido, 65 huesos, 30,632 triángulos y 32,753 vértices. Se verifican Head, Spine, Chest, ambos Upper/Lower Arms, Hands, Upper/Lower Legs y Feet.

Textura real: `CHR_Explorer_BaseColor.JPEG`, 2048×2048, 1,350,079 bytes. No existe Normal, Roughness, Metallic ni AO en esta fuente; no se generan sustitutos ficticios. Un único material `MAT_Mario`, URP/Lit, BaseColor sRGB, mipmaps, Metallic 0 y Smoothness .25. Los bytes de textura y FBX copiados se conservan idénticos al master; el YAML local de los clips/.meta se normaliza únicamente para eliminar espacios finales y mantener diff --check limpio, sin alterar curvas, valores o GUID; la escala cambia solo en el importador del proyecto.

## Clips exactos

| Clip | Duración s | Uso |
|---|---:|---|
| CHR_Explorer_Idle | 5.600000 | Locomotion |
| CHR_Explorer_Walk | 1.366667 | Locomotion |
| CHR_Explorer_Run | .533333 | Locomotion |
| CHR_Explorer_Jump | 2.333333 | Un clip completo compartido por Air/Land; fase según estado físico |
| CHR_Explorer_CrouchWalk | 1.033333 | CrouchWalk |
| CHR_Explorer_CrouchIdle | 1.033333 | Alias de CrouchWalk retenido en fase .25; no es un clip idle independiente |

No hay FBX adicionales de animación: los clips ya son .anim Humanoid y se retargetean al único Avatar del visual. No se necesita duplicar Avatars o configurar Copy From Other Avatar.

Ausentes: Jump Start, Fall/Jump Land independientes, Crouch Enter/Exit, Turn Left/Right, Look Around, Interact, Receive Object/Map, Inspect Map, Store Map, Talk, Listen/Nod, React/Look Back. No se inventan clips. No hay animaciones cinematográficas en este master para catalogar o conectar, y no se cambia Timeline Intro.

## Arquitectura

`CharacterIds.Character03` sigue siendo `character.03`; el catálogo existente devuelve Mario y su prefab. `CharacterVisualAssets` contiene una referencia serializada al visual dentro de Resources; no es otro catálogo, selector o guardado. El runtime no contiene rutas externas ni dependencia del master.

`CHR_Mario_PlayerVisual.prefab`: MarioRoot con escala 1; un modelo skinned y Animator. Sin CharacterController, PlayerMovement/Look/Interaction, input, cámara ni AudioListener. La altura de referencia del renderer es 1.80 m. El importador aplica escala 1.61052847 y el offset de base/centro está dentro del prefab. Up +Y y forward +Z, siguiendo orientación del rig aprobado. La cápsula existente permanece intacta.

`CharacterVisualSpawner` sigue usando el guardado actual, instancia bajo CharacterVisualRoot y evita recrear el mismo visual al repetir selección. El proxy se conserva para personajes sin visual. `PlayerCameraController.RefreshCharacterVisualVisibility` cambia solamente visibilidad: FPS oculta todo el cuerpo; TPS muestra Mario y oculta el proxy cuando hay visual real. No cambia perfiles, pivotes de cámara, shoulder offset, input, reticle o interacción.

## Animator compartido

`Animation/Characters/Explorer/AC_ExplorerGameplay.controller`, reutilizable por otros Humanoid compatibles. Parámetros:

- Speed float: CurrentSpeed / SprintSpeed.
- IsGrounded bool.
- VerticalVelocity float.
- IsCrouching bool.
- IsLanding bool: breve fase visual tras contacto, no controla física.
- JumpPhase float: muestreo del único clip Jump según velocidad vertical/aterrizaje.
- CrouchPose float: .25 para pose idle aprobada.

Locomotion usa Blend Tree 1D: Idle=0, Walk=4/6.5≈.61538, Run=1 con las velocidades actuales. Se consulta el prefab existente para calcular umbrales. No se alteran walk 4 m/s, sprint 6.5 m/s, crouch 2 m/s, gravedad, salto, collider o mecánica crouch. Cadencias iniciales derivadas de la referencia del paquete y velocidades del Player; requieren revisión humana de foot sliding, sin prometer cero deslizamiento.

CrouchIdle/CrouchWalk se separan por Speed. Air y Land reutilizan Jump real mediante tiempo normalizado; no se fabrican clips. No se añade turn-in-place ni Interact en v1.

`Animator.applyRootMotion=false`, UpdateMode Normal, Culling CullUpdateTransforms. El Player/CharacterController es la única autoridad física. Al reaparecer en TPS se actualizan parámetros y se reinicializa el visual; ocultar FPS no destruye el personaje.

## Puente y respawn

`PlayerAnimationController` vive en el Player existente, lee estados y solo escribe parámetros. `PlayerMovement` recibe únicamente propiedades READ-ONLY WalkSpeed, SprintSpeed, CrouchSpeed. No hay nuevo motor ni input.

El puente se suscribe a `CheckpointManager.RespawnCompleted`; ResetForRespawn limpia fase aérea/aterrizaje y reinicializa Animator al ser visible. No cambia CheckpointManager, reset targets, respawn architecture o puzzles. Tutorial/Level01 ya usan el mismo CharacterVisualSpawner, por lo que no se reescriben escenas ni se modifica GameFlow.

## Herramientas y pruebas

- Tikal/Authoring/Apply Mario Gameplay Character: valida recursos/Avatar, escala importada, material, controller, prefab y asignación al catálogo. No duplica assets existentes; valida antes de reutilizarlos.
- Tikal/Validation/Validate Mario Gameplay Character: valida catálogo, Humanoid mapping, renderer, materiales, duplicados, Tutorial/Level01 y la presencia del selector actual de cuatro slots.
- Tikal/Validation/Run Mario Gameplay Play Tests: prueba en el Editor normal, prohíbe batchmode, verifica autoría idempotente, cápsula física existente, walk/run/jump/crouch, FPS/TPS sin reinstanciar, reset, save roundtrip aislado y carga de Tutorial/Level01. Captura evidencias URP y separa errores internos de Unity Search de errores del asset.

`MarioEditorValidation.json` y `MarioPlayValidation.json` registran pruebas efectivamente ejecutadas. La ejecución final pasó las ocho categorías Editor y 53 comprobaciones Play Mode, con cero errores de ejecución o Editor registrados. La autoría se aplicó dos veces y sus assets mantuvieron los mismos bytes (idempotencia). Las pruebas automatizadas no equivalen al playtest humano de WASD, Pause Settings, door/Journal/puzzles/SecretCommand. Ese playtest de aceptación se mantiene pendiente y no se finge.

## Corrección del Avatar y pruebas visuales

El .meta externo llevaba un HumanDescription serializado cuya raíz era `CHR_Explorer(Clone)`. Era aceptado como Avatar válido, pero al reproducir clips generaba huesos a decenas de metros del Player (Head≈140 m). La autoría reconstruye una vez el mapeo Humanoid desde la jerarquía real del FBX local, conservando Create From This Model y marcando la operación en userData. El Avatar ahora tiene humanScale≈0.90028 y Head≈1.56 m relativo al Player en Idle. No se cambia ningún byte del FBX master, ni se aplica un offset corrector en runtime.

Las capturas iniciales vacías fueron rechazadas. La prueba final exige bounds/huesos coherentes junto al Player y más de 1,000 píxeles diferentes respecto del mismo render sin la malla; una captura vacía no puede pasar por ausencia de magenta. Las cinco capturas finales se revisaron: Idle, Walk, Run, CrouchIdle y CrouchWalk muestran el personaje natural, vertical, mirando +Z, sin T-pose ni material magenta.

La prueba usa la escena real MainMenu, su CharacterSelectionController, Select(2), Confirm y cierre/reapertura del panel, inyectando únicamente en la instancia de prueba un GamePersistenceService temporal. Se comprueba nombre Mario, escritura de character.03 y marcador persistente. La partida real del usuario no se sobrescribe. Después se cargan Tutorial y Level01 en Play Mode y se aplica esa selección persistida; también se ejecuta el RestartFromCheckpoint real de Level01.

La validación completa antigua MainMenuFlowValidation contiene una expectativa anterior de catálogo sin ningún visual; se actualiza únicamente esa aserción para admitir placeholders y visuals con Animator. En una ejecución se detectó además el fallo ajeno a esta integración `Main Menu visuals: no old Main Menu branding` por texto existente EL PORTAL. No se modifica branding/escena del menú ni se declara aprobada esa suite completa. Los tests específicos del selector Mario quedan registrados por separado.

Warnings de deprecación existentes en FinalPuzzlesRedesignValidation (FindObjectsSortMode/FindObjectsByType) no se corrigen en esta feature. Las nuevas búsquedas de Mario usan las APIs sin orden por InstanceID.

## Limitaciones

Cruce camisa/pantalón y botas en pendientes: incidencias heredadas, sin reconstrucción cosmética. No hay Foot IK avanzado, brazos FPS ni nuevo preview RenderTexture del menú. El menú usa nombres/cards y estados existentes. Los clips cinematográficos ausentes, giro en el sitio y animación Interact se integrarán en otra feature si se entregan fuentes.

Ningún archivo copiado supera 90 MB; el mayor es el FBX de 2.54 MB. El master conserva todos sus hashes originales. Auditoría detallada y comprobación final se adjuntan en esta documentación antes del commit.

## Evidencias y alcance final

- `Mario/MarioEditorValidation.json`: Avatar, huesos críticos, altura, material, prefab y compatibilidad de escenas.
- `Mario/MarioPlayValidation.json`: ejecución efectiva de pruebas en Editor normal, resultados y errores.
- `Mario/MarioSourceAudit.json`: inventario READ-ONLY de los 44 archivos, tamaños y SHA-256 iniciales.
- `Mario/MarioProtectionVerification.json`: cotejo final de master y archivos previamente versionados.
- `Mario/Mario_Idle.png`, `Mario_Walk.png`, `Mario_Run.png`, `Mario_CrouchIdle.png`, `Mario_CrouchWalk.png`: evidencias capturadas por URP dentro del Play Mode real.

No se modificaron escenas, Timeline Intro, Elder, AncientMap, cápsula física, objetivos del Tutorial, puzzles, CheckpointManager, SecretCommand, GameFlow, save schema, input mappings o configuración TPS. Tampoco la biblioteca de Axel ni assets publicados de vegetación. Los únicos archivos previamente versionados modificados son CharacterCatalog, CharacterVisualSpawner, PlayerCameraController (solo visibilidad), PlayerMovement (tres getters) y la aserción de catálogo en MainMenuFlowValidation.

Quedan para aceptación humana: navegación real con teclado/gamepad, MainMenu→Tutorial/Continue completo, Pause Settings FPS/TPS, foot sliding prolongado, puertas, Journal, puzzles y SecretCommand. No se afirma haber realizado ese recorrido manual.

Control Git: stage explícito, comprobación de tamaños individuales antes del commit, diff --check; commit/push exclusivamente a feature/character-mario-integration. El hash y resultado de push se entregan en el reporte de la sesión, sin merge ni cambios en main.
