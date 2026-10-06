# Introducción: Ancient Map

## Objetivo narrativo

La introducción presenta las ruinas de Tikal bajo la selva en 1500 d.C., establece la búsqueda de la Fuente del Conocimiento y entrega al explorador un mapa provisional. La última advertencia del anciano, «No confíes en el sistema.», debe conservar el mayor peso dramático de la secuencia.

Esta fase es un blockout para evaluar ritmo, encuadres y claridad narrativa. No contiene personajes, mapa, vegetación, animación ni audio finales.

## Escena y Timeline

- Escena: `Assets/_Project/Scenes/Cinematics/Intro_AncientMap.unity`
- Timeline: `Assets/_Project/Timelines/Intro/TL_Intro_AncientMap.playable`
- Duración objetivo: 80 segundos
- Authoring: `Tikal/Authoring/Build Ancient Map Intro`
- Validación: `Tikal/Validation/Validate Ancient Map Intro`

La Timeline controla las cámaras, el movimiento provisional del explorador y del mapa, el fade y los cues narrativos. `IntroSequenceController` controla inicio, finalización, skip y transición de Game Flow. La escena y la Timeline se regeneran de forma idempotente desde el menú de authoring.

## Storyboard

| Tiempo | Contenido |
|---|---|
| 0–8 s | Negro, `1500 d.C.` y dos textos narrativos breves. |
| 8–18 s | Plano general de selva, ruinas y sendero. |
| 18–28 s | El explorador avanza y descubre al anciano. |
| 28–36 s | Encuentro y primeras preguntas. |
| 36–48 s | La Fuente del Conocimiento y el riesgo del camino. |
| 48–63 s | Entrega visual del mapa y mención de la entrada oculta. |
| 63–70 s | El explorador agradece y se dispone a partir. |
| 70–77 s | Advertencia final: `No confíes en el sistema.` |
| 77–80 s | Partida, fade a negro y título opcional. |

## Cámaras

La versión actual usa cámaras Unity normales y `ActivationTrack`, porque Cinemachine no está instalado. Las cámaras son `CAM_Establishing`, `CAM_ExplorerWalk`, `CAM_TwoShot`, `CAM_ExplorerDialogue`, `CAM_ElderDialogue`, `CAM_MapHandoff`, `CAM_FinalWarning` y `CAM_ExplorerDeparture`. Una migración futura a Cinemachine debe conservar estos roles y el ritmo aprobado.

## Placeholders y reemplazo futuro

`ExplorerAnchor` es la autoridad espacial animada por Timeline. Su hijo `CharacterVisualRoot/ExplorerPlaceholder` es reemplazable sin rehacer cámaras o blocking. Cuando estén disponibles los prefabs finales, `SelectedCharacterId`, `CharacterCatalog` y `CharacterVisualSpawner` podrán resolver el aspecto seleccionado dentro de `CharacterVisualRoot`.

`ElderAnchor/ElderPlaceholder` representa al anciano sentado. Debe sustituirse dentro del anchor existente para conservar posición, mirada y cobertura de cámaras.

`AncientMap_Proxy` es independiente de ambos personajes. Su track representa la entrega, y el cue conceptual `AncientMapReceived` registra que la entrega narrativa ocurrió. El cue prepara una integración posterior con inventario o progreso sin hacer depender ese estado solo de la animación visual.

## Subtítulos y audio

`CinematicSubtitlePresenter` separa nombre del hablante y diálogo, y también presenta fecha, narrativa y título. Los cues viven de forma centralizada en la Timeline. Todos los textos visibles actuales están en español.

La Timeline incluye canales vacíos `Ambience`, `Music`, `Voice` y `SFX`. Son slots para producción futura; esta fase no crea clips falsos, voces sintetizadas ni descarga audio. Los subtítulos funcionan sin voz.

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
