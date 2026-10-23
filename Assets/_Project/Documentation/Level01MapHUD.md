# Level01 Map HUD

## Arquitectura y contenido

No existía un sistema runtime de mapas/discovery que reutilizar. `LevelMapHUD` reutiliza
PlayerInputReader, InputPromptResolver, InputDeviceManager y los bloqueos de gameplay del
Player. `LevelMapDefinition` guarda únicamente siluetas XZ de once suelos/conectores públicos
ya existentes. No consulta puzzles, pistas, SecretCommand, objetos ocultos ni False Path.
El mapa no presenta respuestas ni destinos secretos. No se implementa fog-of-war en esta entrega.

`ExplorationMapGraphic` dibuja esas siluetas con geometría de Canvas: no hay cámara de
minimapa, RenderTexture, AudioListener, iluminación extra ni escaneo del environment por frame.
Los paneles reutilizan la piedra UI existente y un borde dorado discreto. El minimapa ocupa
224×224 unidades a referencia 1080p, con margen superior/derecho de 32 y hints debajo.
El overlay expandido ocupa el 76% central de cada dimensión, con márgenes y leyenda norte.

Norte fijo: +Z del mundo corresponde a +Y del mapa. Ambos marcadores leen el Player root,
independientes de MainCamera, shoulder offset y visuales Dwight/Axel/Mario. La flecha rota
`-Player.eulerAngles.y` y usa la misma proyección XZ en ambos tamaños. Fuera del perímetro
público conocido, la posición se limita al borde: no revela geometría secreta.
La posición se actualiza cada LateUpdate y al recibir RespawnCompleted; no se cambia checkpoint.

## Input y prompts

Acción semántica `Global/Map` (Button), dentro del TikalInputActions existente:

| Dispositivo | Map | Journal existente |
|---|---|---|
| Teclado | M | J |
| Xbox | D-Pad Up | View |
| PlayStation | D-Pad Up | Share |
| Generic Gamepad | D-Pad Up | Select |

No existen conflictos de M/D-Pad Up con Player/Global. UI.Navigate usa la cruceta completa
en menús: Map ignora su acción durante Pause, y el overlay de mapa solo habilita UI.Cancel,
sin navegación propia. No se cambia el Input del Main Menu ni se añade HUD al Tutorial.
PlayerInputReader expone la acción del mismo asset; no hay polling de KeyCode ni binding
runtime paralelo. El wrapper se regenera mediante el importador oficial de Input System.

Los hints permanentes `[binding] MAPA` y `[binding] VER DIARIO`, y el hint de cierre,
usan InputPromptResolver. Se refrescan por ActiveDeviceChanged y BoundControlsChanged,
incluidos overrides de las acciones reales, sin guardar preferencias nuevas.

## Comportamiento modal

Map captura el estado previo, cierra Journal, llama `Player.SetGameplayActive(false)` y
deshabilita así Move/Look/Interact/SecretCommand; Global Map/Journal/Pause permanecen activos.
La misma puerta de gameplay oculta retículo y prompts existentes. El minimapa se oculta
mientras hay mapa expandido, Journal, Pause, respawn u otro bloqueo de gameplay.
Map no escribe Time.timeScale y no crea un segundo Pause Menu.

Map vuelve a cerrar con su acción o UI.Cancel (B/Circle). Escape ya pertenece tanto a
Global.Pause como a UI.Cancel: se aplica prioridad única de Pause, cerrando Map antes de
abrir Pause. Start/Menu/Options sigue el mismo traspaso. Al cerrar Map se restaura el estado
previo y el cursor, siempre que el nivel siga jugable. El marcador no depende del modo FPS/TPS.

Journal publica Opening justo antes de capturar el bloqueo previo; Map cierra en ese evento.
Así Journal→Map y Map→Journal no superponen overlays ni restauran controles bajo otro modal.
Pause no se modifica: Map libera el bloqueo en PausePressed antes del LateUpdate de Pause,
y utiliza PauseChanged para la visibilidad. Outcome/transición impiden reaperturas.

## Autoría, validación y reutilización

`Tikal/Authoring/Apply Level01 Map HUD` crea datos, paneles, hints y referencias solo en Level01.
Una segunda ejecución conserva la instancia configurada sin duplicar objetos ni cámaras.
No reconstruye el environment ni toca la jerarquía de puzzles. La operación requiere Edit Mode
y guardar antes cualquier edición manual pendiente. La definición usa una lista explícita de
suelos públicos; no se debe ampliar automáticamente a superficies ocultas.

`Tikal/Validation/Validate Level01 Map HUD` ejecuta configuración e Input audit y después
Play Mode con guardado aislado: teclado, pads virtuales Xbox/PS/Generic, cambio de dispositivo,
overrides de hints, Map/Journal/Pause, bloqueo de acciones, FPS/TPS, visuales de personajes,
checkpoint y marcador, cámaras/AudioListener y Missing Scripts. No resuelve puzzles.
Evidencia local fuera de Assets: `_Level01MapHUD_20261023`.

Para Level02 se reutilizan los componentes con otra definición explícita de áreas públicas
y referencias a su Player/UI. No se crea mapa de Level02. Un mapa progresivo podrá añadirse
posteriormente como una política sobre áreas autorizadas, sin introducir un segundo mapa.

## Resultado de esta entrega (2026-10-23)

Autoría aplicada dos veces sin duplicados. Pasaron 14 comprobaciones de Editor y 42 de Play Mode, sin errores reportados. Las pruebas usaron dispositivos virtuales del Input System (teclado, Xbox, PlayStation y Generic), no mandos físicos. Se capturaron las vistas normal/expandida y FPS/TPS. Los archivos personales de progreso y ajustes conservaron sus hashes.

La comparación semántica de la escena conservó todos los bloques originales; la única modificación preexistente fue añadir el HUD al Canvas. El resto son objetos nuevos del HUD. El playtest humano con hardware físico queda como aceptación adicional.
