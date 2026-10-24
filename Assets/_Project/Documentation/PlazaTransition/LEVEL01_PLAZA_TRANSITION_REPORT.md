# Level01 → Plaza Keynes: cinemática de transición

Fecha: 2026-10-24. Rama: `feature/level1-to-level2-transition`, basada en `4d42e68` de Plaza Keynes ya publicada. No merge.

## Resultado y límites de la validación

**Flujo funcional aprobado en Unity Editor Play Mode real para Dwight, Axel y Mario: 126 comprobaciones.** Once capturas reales de Unity acompañan este informe. No se ejecutó una batería global de puzzles ni se certifica rendimiento de un build.

El JSON conserva `transitionPassed: true`, `allThreeCharactersCompleted: true` y `passed: false`. El último campo refleja una excepción de infraestructura del Editor en `UnityEditor.Search.SearchDatabase.EnumerateAll` / `SearchInit.IndexationOnStartup`. Fue la única excepción registrada en la ejecución final; no se presenta el Editor como libre de errores. La traza se conserva íntegra en `Play_Validation.json`. No se atribuye esta excepción a los modelos. Las búsquedas de `JobTempAlloc`, `ALLOC_TEMP`, errores C# y `MissingReferenceException` en el registro final no produjeron coincidencias.

La comprobación Editor posterior de entrega aprobó 57 dependencias, cuatro bindings de Timeline y cero scripts/materiales ausentes. Su cierre sí registró `StackAllocator(ALLOC_TEMP_MAIN) m_LastAlloc not NULL`; se conserva en `Unity_Delivery.log`, sin evidencia para atribuirlo a un asset. Es distinto del registro de la ejecución Play Mode final.

## Auditoría real previa

- Level01: `Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity`.
- Puerta: `Level01_SecretEntrance/Environment/Entrance/EntranceDoor_Blockout`.
- Pivote de la hoja: `.../EntranceDoor_Blockout/Pivot`.
- Su envolvente visual original mide 3.6 × 4.8 × 0.24 m. La cinemática utiliza una copia visual independiente, escalada uniformemente por 3.35/4.8. La puerta de gameplay sigue intacta.
- `SecretEntranceCommandReceiver.CanExecuteCommand` exige input activo, estado Playing, Door cerrada y jugador dentro de zona/rango; rango real 3 m. Sus condiciones no se modifican.
- El único llamador de victoria de producción localizado era `SecretEntranceCommandReceiver.Receive`. `LevelOutcomeController` emite `OutcomeChanged` y desactiva gameplay. Antes de esta tarea, `LevelResultPresenter` mostraba un panel de victoria sin cargar una escena nueva.
- `SceneCatalog` tenía Level02 sin ruta. La escena real ya existía: `Assets/_Project/Scenes/Levels/Level02_PlazaKeynes.unity`.
- Plaza Keynes ya dispone de Explorer con llegada validada en aproximadamente `(0, 0.08, -72)`, mirando hacia +Z. No se ha cambiado su escena ni su composición.
- Se revisaron `IntroSequenceController` y `AncientMapIntroAuthoring`: se reutiliza el patrón PlayableDirector/Timeline, proxy visual y fade CanvasGroup. No se reutiliza el controlador de Intro porque incluye subtítulos, entrega del mapa y destino Tutorial ajenos a esta transición.

## Conexión mínima y exclusividad de activación

`SecretEntranceCommandReceiver.Receive` conserva el mismo `CanExecuteCommand`. La llamada de victoria se envuelve en `Level01PlazaTransition.TriggerSecretVictory`, que marca únicamente la emisión síncrona de este SecretCommand. No cambia cómo se decide si el comando es correcto.

`LevelResultPresenter.Show` entrega solo esa victoria de Level01 al nuevo servicio, antes de mostrar el panel de resultado. La ruta exige la marca del SecretCommand, outcome Victory, escena origen correcta y ambas escenas destino presentes en Build Settings. Abrir la Door, cargar una escena, un respawn, una derrota o una llamada de victoria ajena a esa marca no cumplen esta puerta de entrada. Si faltan las escenas, se conserva el resultado anterior con un error explícito.

Se invoca `GameFlowController.CompleteLevel(Level01)` existente una vez antes de salir: registra la finalización y desbloquea Level02 mediante el API actual. No se añaden formatos de guardado ni reglas de progreso. `CurrentLevelId` y la política de CONTINUAR no se rediseñan en esta tarea.

## Escena y Timeline nuevos

Escena: `Assets/_Project/Scenes/Cinematics/Transition_Level01_PlazaKeynes.unity`.

Timeline: `Assets/_Project/Timelines/PlazaTransition/TL_Level01_PlazaKeynes.playable`.

| Elemento | Configuración |
|---|---|
| Duración del plano | 10.5 s, más fades externos y tiempo de carga |
| Fade inicial Level01 | 0.75 s, a negro, tiempo no escalado |
| Entrada al plano | 0.7 s desde negro |
| Puerta | Subida local +Y de 3.65 m entre 0.8 y 2.8 s; duración 2 s; sin física |
| Personaje | Proxy exclusivamente visual, z=-3.2 a z=10.5 entre 2.6 y 9.8 s |
| Cámaras | Una cámara cinematográfica con recorrido de establecimiento a seguimiento trasero/lateral |
| Túnel | Seis tramos cortos de piedra, techo, suelo, laterales y cierre visual al fondo |
| Iluminación | Una direccional y un punto local sin sombras; interior oscuro y niebla sencilla |
| Fade final | 0.9 s, entre 9.6 y 10.5 s |
| Llegada Level02 | Carga bajo negro y fade de 0.8 s; control devuelto al terminar |

El Timeline tiene cuatro pistas AnimationTrack: `Door_Lift`, `Character_Passage`, `Camera_Passage` y `Passage_Fade`. Usa offsets explícitos y evaluación manual mediante PlayableDirector con tiempo no escalado. El controlador de secuencia solo reproduce el Timeline, prepara la visual seleccionada y solicita la siguiente escena; no contiene lógica permanente de puzzles.

La puerta proxy comparte la malla y el material reales de la hoja de Level01. Se retiraron únicamente scripts/colliders de esa copia. El mecanismo de la Door original no cambia de eje ni de comportamiento.

El set reutiliza los prefabs reales `PFB_ARQ_MuroMaya_01`, `PFB_ARQ_PisoPiedraMaya_01`, `PFB_ARQ_BloqueMaya_02`, `PFB_VEG_ArbustoTropical_02`, `PFB_VEG_HelechoTropical_02` y `PFB_VEG_ArbolTropical_02`. Un cubo de respaldo con material de piedra cierra el decorado fuera del recorrido. No se crean FBX ni se modifican los originales.

No se localizó un audio de puerta/piedra adecuado entre los archivos de audio del proyecto inspeccionados. El plano queda sin sonido añadido, sin VO, diálogo ni subtítulos. El audio activo del origen se desvanece durante el primer fade y desaparece al descargar Level01. No se implementa Skip nuevo: el de Intro está acoplado a su propio flujo.

## Selección de personaje y Walk

Se consulta `SelectedCharacterId` mediante la persistencia existente y se resuelve el prefab con `CharacterCatalog`.

| Personaje | Prefab visual real | Resultado final |
|---|---|---|
| Dwight | `Assets/_Project/Prefabs/Characters/CHR_Dwight_PlayerVisual.prefab` | Recorrido y llegada aprobados |
| Axel | `Assets/_Project/Prefabs/Characters/CHR_Axel_PlayerVisual.prefab` | Recorrido y llegada aprobados |
| Mario | `Assets/_Project/Prefabs/Characters/CHR_Mario_PlayerVisual.prefab` | Recorrido y llegada aprobados |

Los tres controladores existentes resuelven el Walk real `Assets/_Project/Art/Characters/Mario/Animations/CHR_Explorer_Walk.anim`; no es una suposición por nombre: se verificó el clip activo en Play Mode en los tres casos. Los avatares son Humanoid. Se mantiene `applyRootMotion=false`. El Timeline mueve el proxy y el controlador existente reproduce Walk en su umbral de locomoción. Su velocidad de reproducción se acompasa a 13.7/7.2 m/s frente a los 4 m/s configurados para Walk en gameplay.

Durante la cinemática no hay PlayerController, PlayerInputReader, Journal, Map, SecretCommand ni controlador de pausa de gameplay. En la salida de Level01 se desactiva también el reader completo para bloquear sus acciones Global. En Plaza Keynes se reutiliza su único Player y se aplica la misma selección; el control permanece bloqueado hasta terminar el fade de llegada.

## Pruebas realmente ejecutadas

Las 126 comprobaciones incluyen:

- Escena real Level01 y receptor configurado, sin disparo al cargar.
- SecretCommand G mediante eventos reales de InputSystem: fuera de zona no dispara.
- Apertura ordinaria de Door: no dispara la transición.
- Condiciones actuales de puerta cerrada, zona, rango e input satisfechas antes del comando correcto.
- Victoria real, opacidad intermedia del fade y desactivación de movimiento/look/acciones.
- Carga de escena separada y selección visual correcta para cada uno de los tres personajes.
- Posiciones iniciales correctas de cámara y personaje.
- Ascenso de puerta sin desplazamientos X/Z y altura final 3.65 m.
- Clip Walk activo, sin root motion, desplazamiento por el túnel y alcance de los seis momentos del plano.
- Carga real de Plaza Keynes bajo negro; control bloqueado al llegar y restituido al terminar el fade.
- Selección persistente sin cambios, llegada apoyada en collider de suelo, orientación +Z.
- Movimiento W real del Player tras la llegada.
- Un solo Player en destino, una sola MainCamera activa y un solo AudioListener activo.
- Ausencia de Missing Scripts y materiales nulos/shader de error en cinemática y destino.
- Ausencia de fuentes de audio reproduciendo VO en la cinemática.

No se ejecutaron todas las pruebas de puzzles, False Path o checkpoints. La exclusión de derrota/respawn se respalda por el guard de activación y la inspección del flujo, sin afirmar que se ejecutó una regresión global. Tampoco se certifica un build distribuido ni métricas FPS/GPU de producción.

Los intentos previos y sus errores se conservan fuera del repositorio. El primero detectó reproducción del Timeline incorrecta; la evaluación manual corrigió la aplicación de pistas. Se corrigieron además offsets relativos que movían el actor/cámara al origen. Un fallo de selección del harness se debía a cambiar el archivo de selección después del Awake de GameFlow; la prueba final guarda la selección y recarga Level01 como hace el flujo real. Se cerró una abertura visual al fondo del túnel antes de la ejecución final.

## Evidencias y reproducción

- `Play_Validation.json`: resultado de la ejecución final; no sustituirlo por resultados de intentos anteriores.
- `Source_Audit.txt`: puerta, medidas y prefabs/clips reales encontrados.
- `Editor_Delivery_Audit.json`: integridad y hashes de la entrega.
- `Captures/Transition_DoorClosed.png`
- `Captures/Transition_DoorOpening.png`
- `Captures/Transition_WalkIntoEntrance.png`
- `Captures/Transition_Tunnel.png`
- `Captures/Transition_DarkTunnel.png`
- `Captures/Transition_FinalFade.png`
- `Captures/Transition_PlazaKeynesArrival.png`
- Cuatro capturas adicionales del túnel y llegada de Axel/Mario.

Capturas generadas mediante la cámara real y URP SingleCameraRequest en Play Mode. Para incluir los fades, el verificador transforma temporalmente los Canvas overlay a camera-space durante la captura y restaura su configuración; no guarda esos cambios en las escenas.

Copia utilizada: `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Tikal_ElPortal\_PlazaKeynes_20261024`.

Logs, hashes previos, respaldos y resultados de intentos: `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Tikal_ElPortal\Para_Nivel_2\Transition_20261024`.

Repetición focalizada en esa copia: ejecutar Unity 6000.6.3f1 con `-batchmode -projectPath <copia> -executeMethod Tikal.Tests.Editor.PlazaTransitionValidation.Begin -logFile <nuevo log>`. El validador protege el proyecto canónico y usa un productName de pruebas para no modificar la partida del usuario. La autoría inicial `PlazaTransitionAuthoring.Run` rechaza sobrescribir una escena existente.

## Archivos y protección

Solo se autorizan cambios existentes en:

1. `Assets/_Project/Code/Core/SecretEntranceCommandReceiver.cs`: envoltorio de la victoria aceptada, sin cambiar CanExecuteCommand.
2. `Assets/_Project/Code/UI/LevelResultPresenter.cs`: desvío de esa victoria a la transición, conservando el resto del panel.
3. `Assets/_Project/Code/Core/SceneCatalog.cs`: ruta real de Level02.
4. `ProjectSettings/EditorBuildSettings.asset`: añadir la escena nueva y Plaza Keynes, conservando las anteriores.

Se añaden dos scripts de secuencia/fades, herramientas Editor de autoría/validación, escena nueva, Timeline, cuatro clips y esta documentación/evidencias. Las rutas exactas y hashes se registran en el manifiesto externo de entrega.

Se respaldan los cuatro archivos existentes antes de copiarlos desde la versión probada. Se comparan por SHA-256 todos los archivos previamente registrados de Assets/Packages/ProjectSettings. Las escenas originales Level01, Intro, Tutorial, MainMenu y Plaza Keynes, sus prefabs y su arte deben permanecer idénticos. No se realizan operaciones sobre la biblioteca de Axel ni las bibliotecas de assets externas, y no se afirma haber recalculado sus árboles completos de hashes.

El editor del proyecto principal ya estaba abierto: no se cerró ni se utilizó para ejecutar esta validación. Todas las ejecuciones solicitadas se lanzaron contra la copia indicada. No se añaden puzzles, objetivos, UI funcional, misiones ni otras mecánicas de Level02.

Commit y push: consultar la entrega Git de esta rama y `Para_Nivel_2/Transition_20261024/Git_Delivery.json`, generado después del commit para no introducir autorreferencias en sus hashes.

## Normalización de empaquetado

Después de las pruebas se retiraron exclusivamente espacios al final de línea y líneas vacías finales de los archivos de texto nuevos para superar `git diff --cached --check`. No se cambiaron curvas, valores, bindings, GUID, geometría, materiales ni código de ejecución. El JSON de auditoría conserva los hashes originales probados. `Formatting_Only_Verification.json`, junto al manifiesto externo de entrega, registra los bytes anteriores y finales y comprueba equivalencia por esa transformación exacta. Las capturas PNG permanecen idénticas. Los cuatro archivos existentes de conexión no reciben esta normalización.
