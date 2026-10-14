# Mario — integración gameplay

Estado: integración y refinamiento de Walk/grounding en feature/character-mario-integration. El usuario confirmó satisfactorio el playtest humano inicial; la revisión humana de estas correcciones es independiente de las pruebas automatizadas indicadas abajo. No modifica Intro_AncientMap, Timeline, Elder, AncientMap, puzzles, input, formato de guardado ni ajuste de cámara TPS.

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


## Refinamiento Walk y alineación — 2026-10-13

### Fuente canónica y retirada de la anterior

Se auditó íntegramente la entrega READ-ONLY:
`C:\Users\Mario Cortez\Documents\Codex\2026-10-11\files-pasted-by-the-user-proyecto\outputs\CHR_Explorer_Walk_Update`.
Contiene siete archivos: clip, meta, README, manifiesto, verificación de protección, validación y texto de traspaso. Se leyeron documentos y datos; los seis hashes declarados en MANIFEST_SHA256.json coincidieron. La auditoría inicial/final incluye los siete archivos y no modifica la fuente.

La Walk canónica es `Assets/_Project/Art/Characters/Mario/Animations/CHR_Explorer_Walk.anim`, fileID 7400000, GUID **31ec003e1e834ebba8fa3954574ab9ae**. Corresponde a C aprobada/Upright, no a la Walk anterior ni a Natural B. Su SHA de entrega es `c56fe0890ba6ecf187548dc23e871459e2839d9aa365d221bb52844e3111a80d`. Se normalizó únicamente whitespace final y saltos de línea en la copia local para diff --check; curvas, ajustes, nombre y GUID se conservan. La fuente externa permanece idéntica.

La Walk anterior era un **.anim independiente**, GUID `b09c1f85f39f9c548895867b7b174c1f`; no un clip embebido en FBX. La búsqueda previa encontró únicamente su meta y la referencia del hijo central de ExplorerLocomotion. Se retiró su identidad/contenido por reemplazo auditado del clip y meta en la misma ruta, después de preparar la nueva referencia. No queda un asset de Walk antigua en Assets ni referencia activa del controller. El original es recuperable en Git y el respaldo local de auditoría permanece en Temp; no se elimina nada del master externo.

`AC_ExplorerGameplay` conserva Idle/Walk/Run, estructura, parámetros, transiciones y velocidades; cambia solo la referencia de Motion del hijo Walk al nuevo GUID. No se crea AC_Mario, otro Avatar ni un AnimatorOverrideController.

### Propiedades, loop y velocidad

- AnimationClip nativo no Legacy, Humanoid confirmado por Unity 6000.6.3f1.
- Duración 1.3666668 s, 30 fps, 130 bindings Animator sin rutas Transform de un esqueleto específico.
- Loop Time y Loop Pose activados, blending de orientación/Y/XZ conservado.
- RootT.x/y/z y RootQ.x/y/z/w presentes: datos de cuerpo Humanoid, no coordenadas métricas del Player. No se borran para inmovilizar físicamente al actor.
- RootT.x: −0.10045553..0.0003899905; RootT.y: 0.9381055..0.9824292; RootT.z: −0.014417871..0.042879738. Principio/final casi iguales: el ciclo no representa avance acumulativo horizontal. Orientación offset Y=0, KeepOriginalOrientation conservado.
- Animator.applyRootMotion sigue false en prefab y puente. PlayerMovement/CharacterController siguen como autoridad; ningún ajuste de transform cada frame.
- El paquete de prueba externo cita 1.60 m/s y 1.8723404 de cadencia. **No se trasladan esos valores de gameplay**: el juego conserva Walk=4 m/s, Sprint=6.5 m/s y Crouch=2 m/s.
- Se conserva el timeScale Walk **4.292876** ya existente, umbral **0.61538464**, Locomotion state speed=1; Run timeScale **1.7278966** intacto. No se añade multiplicador ni amortiguado nuevo en runtime.

### Diagnóstico y solución visual

CharacterVisualRoot tiene Y local 0. El Avatar existente es válido/humanoid, humanScale≈0.9002806; importScale=1.61052847, FBX, mapeo y bind poses permanecen intactos. El root del modelo tenía Y local **+0.15764016 m**, obtenido originalmente a partir de bounds del renderer. La geometría real de las suelas no coincide con ese límite de bounds: la medición CPU de skinning en 96 fases mostró Idle medio +0.15200025 m sobre el origen; la nueva Walk media +0.15205449 m. Diferencia entre sus apoyos medios: solo 0.054 mm. No hay motivo para corregir RootT de un clip de forma aislada.

En Play Mode sobre cubo plano, el CharacterController existente estabiliza el Player en **Y≈+0.03999989 m**, coherente con su skinWidth 0.04. Esa separación también debe considerarse visualmente; no se modifica la cápsula para quitarla.

Corrección definitiva, exclusivamente en el modelo anidado de `CHR_Mario_PlayerVisual.prefab`:

- Y anterior: **+0.15764016 m**.
- Y final: **−0.03436009 m**.
- Delta aplicado: **−0.19200025 m** (= −0.15200025 de calibración + −0.04 de separación física medida).
- Root exterior, X/Z, escala, rotación, huesos y Avatar sin cambios.

No se introduce un componente de grounding, IK, otro motor ni offset dinámico. El origen visual se mantiene también durante Jump; no se pega al suelo en el aire. El authoring usa la calibración al crear el prefab y el validador comprueba su valor y suelas reales, evitando el falso criterio basado solo en renderer.bounds.min.y.

### Medición de geometría y límites

`MarioGroundBefore.json` registra el diagnóstico anterior con la Walk antigua; `MarioGroundValidation.json` registra el nuevo clip y configuración final. Los valores de este último están relativos al origen del Player; para el plano de suelo de la prueba, sumar la separación física medida de 0.04 m.

| Pose | Suela mínima / máxima respecto al suelo plano (m) | Interpretación |
|---|---|---|
| Idle, 96 fases | −0.000847 / +0.000785 | Apoyo estable alrededor del plano |
| Walk nueva, 96 fases | −0.006142 / +0.014476 | Apoyo medio ≈+0.000054; leve variación de suela en el ciclo |
| Run existente, 96 fases | −0.023692 / +0.094316 | Incluye fase aérea y penetración puntual de geometría de bota heredadas |
| CrouchIdle, fase .25 | +0.025234 | Alias existente, conservado; no es un nuevo idle de agachado |
| CrouchWalk existente, 96 fases | −0.062844 / +0.026272 | Variación heredada de puntas/botas; no certifica contacto perfecto |
| Jump existente, 96 fases | +0.004245 / +0.596730 | Elevación visual propia del clip, conservada |

Los huesos de pie permanecen por encima del plano; su posición no equivale a la planta de la bota. No se afirma cero deslizamiento ni ausencia total de penetración de puntas en Run/Crouch. La corrección elimina el desplazamiento constante; una corrección detallada de esas poses sería un trabajo adicional sobre clips/IK y no se oculta como validación perfecta.

### Validación y evidencias

La ejecución autoritativa está en `MarioPlayValidation.json` y `MarioEditorValidation.json`: Editor normal, nunca batchmode. Las pruebas Play Mode usan simulación uniforme de 60 Hz para que las capturas GPU no falseen el intervalo de medición; ese ajuste temporal se restaura al finalizar y no modifica ProjectSettings. La medición de continuidad comienza después de estabilizar Idle, excluyendo la inicialización de bind pose/Rebind. No se cambia el puente runtime para disimular un artefacto del arranque de la prueba.

La suite comprueba referencia canónica/GUID, Walk antigua inactiva, root motion false, character.03/Mario, visual sin scripts faltantes, clips presentes, Idle/Walk/Run y transiciones inversas, conservación de offset en Crouch/Jump/Land/FPS/TPS/reset y RestartFromCheckpoint real de Level01. También valida selector y persistencia con un save aislado y presencia del Player/visual en Tutorial y Level01. No cambia objetivos Move/Sprint/Crouch del Tutorial ni puzzles/Journal/Door/SecretCommand/checkpoints; no equivale a un nuevo recorrido humano completo de sus mecánicas.

Capturas actualizadas: Mario_Idle.png, Mario_Walk.png, Mario_Run.png, Mario_CrouchIdle.png y Mario_CrouchWalk.png. Se comprueban malla visible y ausencia de magenta; las capturas fueron revisadas visualmente. Son evidencia de poses concretas, no video de todo el ciclo.

`Tikal/Validation/Validate Mario Gameplay Character` incluye ahora referencia Walk canónica, antigua inactiva, loop, offset calibrado y baseline Idle/Walk. `Tikal/Validation/Audit Mario Feet Baseline` permite repetir diagnóstico por clip sin alterar assets. Los experimentos fallidos iniciales no se presentan como aprobación; el JSON final es el dictamen vigente.

`MarioWalkSourceAudit.json` y `MarioWalkProtectionVerification.json` registran identidad, fuentes READ-ONLY y cotejo de archivos versionados. PlayerMovement, cápsula, gravedad, terreno, cámaras, escenas, catálogo, UI, código runtime y demás clips permanecen intactos. El commit inicial 92958f3 no se modifica. El nuevo commit y push se reportan al cerrar la sesión; no hay merge ni cambios de rama/main.


Resultado definitivo del refinamiento: **10 controles Editor y 72 comprobaciones Play Mode aprobados**, 0 errores runtime y 0 errores Editor registrados. En el ensayo final, Idle queda a −0.000517972 m del plano (0.52 mm de penetración imperceptible), Player Y=0.03999989 m y modelo local Y=−0.03436009 m. Continuidad después de estabilizar Idle: máximo salto de pelvis por muestra de 60 Hz=0.03283203 m; control de regresión de teleport (>0.08 m/muestra) superado. Esto no certifica ausencia de todo popping perceptible en cualquier framerate/terreno. Walk→Idle y Jump→Land regresan al baseline con tolerancia de 3 mm, sin corrección dinámica. La autoría se ejecutó dos veces y mantuvo bytes idénticos en prefab/controller/registry/model meta.
