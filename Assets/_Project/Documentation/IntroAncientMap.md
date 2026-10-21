# Introducción: Ancient Map

## Objetivo narrativo

La introducción presenta las ruinas de Tikal bajo la selva en 1500 d.C., establece la búsqueda de la Fuente del Conocimiento y entrega al explorador un mapa provisional. La última advertencia del anciano, «No confíes en el sistema.», debe conservar el mayor peso dramático de la secuencia.

La escena conserva el entorno y mapa de blockout. Ahora utiliza el Anciano real con actuación sentada y Dwight como Explorer visual canónico; no incorpora entorno final ni VO. Véase `Characters/ElderCinematicIntegration.md` para fuente, retarget, colocación y límites.

## Escena y Timeline

- Escena: `Assets/_Project/Scenes/Cinematics/Intro_AncientMap.unity`
- Timeline: `Assets/_Project/Timelines/Intro/TL_Intro_AncientMap.playable`
- Duración objetivo de la segunda pasada: 97 segundos
- Authoring: `Tikal/Authoring/Build Ancient Map Intro`
- Validación: `Tikal/Validation/Validate Ancient Map Intro`

La Timeline controla las cámaras, el movimiento provisional del explorador y del mapa, el fade y los cues narrativos. `IntroSequenceController` controla inicio, finalización, skip y transición de Game Flow. La herramienta histórica Build Ancient Map Intro regenera el blockout original. Para conservar la integración de personajes se utiliza `Tikal/Authoring/Apply Elder Cinematic Character`, que actualiza la escena existente sin rehacerla.

## Storyboard

| Tiempo | Contenido |
|---|---|
| 0–1.5 s | Negro y entrada gradual del futuro ambiente. |
| 1.5–4.5 s | `1500 d.C.` con permanencia legible. |
| 4.5–6 s | Pausa en negro. |
| 6–13.5 s | Primer texto del narrador, con tiempo adicional de lectura. |
| 13.5–15 s | Pausa en negro. |
| 15–23.5 s | Segundo texto del narrador, con énfasis posible en «fuente de conocimiento». |
| 23.5–25 s | Salida del texto y transición visual hacia la selva. |
| 25–35 s | Plano general de selva, ruinas y sendero. |
| 35–45 s | El explorador avanza y descubre al anciano. |
| 45–53 s | Encuentro y primeras preguntas. |
| 53–65 s | La Fuente del Conocimiento y el riesgo del camino. |
| 65–80 s | Entrega visual del mapa y mención de la entrada oculta. |
| 80–87 s | El explorador agradece y se dispone a partir. |
| 87–94 s | Advertencia final: `No confíes en el sistema.` |
| 94–97 s | Partida, fade a negro y título opcional. |

## Cámaras

La versión actual usa cámaras Unity normales y `ActivationTrack`, porque Cinemachine no está instalado. Las cámaras son `CAM_Establishing`, `CAM_ExplorerWalk`, `CAM_TwoShot`, `CAM_ExplorerDialogue`, `CAM_ElderDialogue`, `CAM_MapHandoff`, `CAM_FinalWarning` y `CAM_ExplorerDeparture`. Una migración futura a Cinemachine debe conservar estos roles y el ritmo aprobado.

Cada personaje tiene `CinematicTargets/HeadLookTarget` y `ChestLookTarget`, separados del mesh provisional. Los targets del anciano respetan su pose sentada. Las cámaras de diálogo apuntan al rostro y hombros; `CAM_FinalWarning` usa el plano facial más íntimo, mientras `CAM_MapHandoff` conserva deliberadamente un encuadre bajo sobre el mapa y las manos. `CAM_ExplorerDeparture` permanece abierta para mostrar sendero y entorno.

## Placeholders y reemplazo futuro

`ExplorerAnchor` es la autoridad espacial animada por Timeline. Su hijo CharacterVisualRoot contiene ahora el visual canónico Dwight; ExplorerPlaceholder queda inactivo. La Intro usa Timeline y su propio binding visual, sin modificar CharacterCatalog, CharacterVisualSpawner ni la selección guardada.

`ElderAnchor` contiene CHR_Elder_Cinematic y conserva ElderPlaceholder inactivo. Sus targets y las cámaras existentes se mantienen, con framing para el modelo real. Dwight se instancia bajo CharacterVisualRoot sin cambiar su prefab jugable ni utilizar el sistema de selección.

`AncientMap_Proxy` es independiente de ambos personajes. Un ParentConstraint controlado en su track mezcla las manos del Anciano y Dwight entre 70,35 y 71 s y lo mantiene con el receptor durante la partida. El cue conceptual `AncientMapReceived` registra que la entrega narrativa ocurrió. El cue prepara una integración posterior con inventario o progreso sin hacer depender ese estado solo de la animación visual.

## Subtítulos y audio

`CinematicSubtitlePresenter` separa nombre del hablante y diálogo, y también presenta fecha, narrativa y título. Los cues viven de forma centralizada en la Timeline. Todos los textos visibles actuales están en español.

La Timeline incluye canales `Ambience`, `Music`, `Voice`, `VOICE_NARRATOR` y `SFX`. El sistema busca automáticamente un clip existente llamado `AUD_AMB_Jungle_Main_01`; si aparece en el proyecto, lo enlaza en bucle con fade de entrada, fade de salida y una reducción suave durante la advertencia final. Actualmente ese asset no existe y los únicos WAV disponibles son tonos del puzzle Echo, por lo que no se usa audio provisional inadecuado ni se crean archivos falsos.

Los hooks de narrador son:

- `VO_NARRATOR_INTRO_001`: «Bajo la inmensidad de la selva descansaban las ruinas de una ciudad olvidada.»
- `VO_NARRATOR_INTRO_002`: «Se decía que en algún lugar de ella permanecía oculta una fuente de conocimiento.»

Las ventanas de 7.5 y 8.5 segundos incluyen margen aproximado de 0.8–1.2 segundos después de una lectura grave y pausada. Los futuros subtítulos deben conservar entre 0.5 y 1 segundo de lectura después de cada locución cuando sea apropiado.

`DateCard` y `NarrativeText` usan curvas de opacidad propias: entrada suave, permanencia legible y salida suave antes de cada pausa en negro.

## Fade y skip

El fade usa un `CanvasGroup` animado por Timeline: negro al inicio, fade in de imagen y fade out final. El skip requiere mantener la acción semántica `UI.Cancel` durante 1.25 segundos. `InputDeviceManager` e `InputPromptResolver` actualizan el texto para teclado/ratón, Xbox, PlayStation o gamepad genérico.

Final normal y skip comparten la misma ruta idempotente:

- First Run: marca `IntroSeen` y continúa a `TUT_ExplorerBasics`.
- Replay desde Cinemáticas: vuelve a `MainMenu` sin modificar personaje seleccionado, nivel actual o tutorial completado.

## IntroPlaceholder

`Assets/_Project/Scenes/Core/IntroPlaceholder.unity` queda **DEPRECATED / technical fallback**. El authoring conserva su referencia histórica como entrada deshabilitada en Build Settings. `SceneCatalog` usa exclusivamente `Intro_AncientMap` como ruta semántica de Intro.

## Validación manual

Después de ejecutar el authoring y la validación en Unity Editor:

1. Restablecer progreso local y elegir `JUGAR`; comprobar Intro → Tutorial.
2. Abrir Cinemáticas → Introducción; comprobar Intro → MainMenu.
3. En ambos contextos, mantener el control de omisión y comprobar el mismo destino que el final normal.
4. Revisar framing, legibilidad, pausas, transferencia del mapa, warning final y fade.
