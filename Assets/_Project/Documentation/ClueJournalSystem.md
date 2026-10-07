# Diario de pistas y organización de UI

## Flujo y fuente de verdad

### Clue05: diagnóstico del cuadrado en PlayStation

La cadena real es SecretKeyDiscoveryPuzzle.Interact (Examinar) → PuzzleSolved →
PuzzleClueReward → ClueTracker.ClueDiscovered → ClueDiscoveryPresenter.Show/RenderClue →
CluePresentation.Description → SecretCommandAction → InputPromptResolver → TMP_Text.
La escena conecta el reward al asset level01.revelation (SecretCommand semántico), y conecta
popup/Journal al mismo PlayerInputReader e InputDeviceManager. No copia el binding de Interact,
no usa un icono cacheado y no hay un segundo resolver de discovery.

El resolver entrega U+25B3 (△) desde Gamepad/buttonNorth para PlayStation. Sin embargo,
InteractionPromptFont es un atlas estático que solo contiene U+25A1 (□), no U+25B3.
Su fallback global Liberation Sans tampoco contiene U+25B3 en el cmap del TTF.
TextMeshProUGUI reemplaza un carácter no encontrado por TMP_Settings.missingGlyphCharacter,
o por U+25A1 cuando ese setting es cero (configuración actual). El cuadrado observado es el
glifo de sustitución, no el control Interact. ClueDiscoveryPresenter._description es el texto
que muestra esa sustitución; ClueJournalPresenter._content usaba la misma fuente.

CluePromptSymbols añade un único glifo original U+25B3 con atlas Alpha8 SDF estático,
referenciado en el fallback de InteractionPromptFont. No depende de fuentes del sistema ni de
generación dinámica en un build. Tools/GenerateCluePromptSymbols.py reproduce el asset usando
solo la biblioteca estándar de Python. No cambia la tipografía de los caracteres existentes.
La escena y prefabs ya referencian la fuente compartida: no requieren regeneración ni authoring.

El mapping sigue en InputPromptResolver.GamepadControlText: West = X/□/Botón oeste;
North = Y/△/Botón norte. Keyboard usa el binding efectivo E/G del Input System.
Discovery y Journal conservan ActiveDeviceChanged y no modifican el progreso al refrescar.

FinalPuzzlesRedesignValidation ahora activa Zone05 mediante E o buttonWest en cada una de
las cuatro familias. Comprueba el prompt antes de Examine y el SecretCommand inmediatamente
después del evento real; luego cambia dispositivo con popup y Journal abiertos. ForceMeshUpdate
comprueba textElement.unicode y visibilidad para detectar la sustitución aunque .text sea correcto.
La validación de edición exige cobertura de △ en ambas fuentes/fallbacks y mantiene Missing Scripts.
La ejecución de Play Mode y la confirmación visual con hardware siguen siendo verificaciones
interactivas; no se ejecutan con batchmode.

Verificación de este fix: comprobados cmap sin U+25B3, sustitución U+25A1 en el código TMP,
referencias materializadas del popup/Journal, mapa Unicode→glyph, tamaño/contorno/hueco del atlas,
fallback sin ciclos y reproducción determinista. Preview del contorno inspeccionado visualmente.
Runtime y Editor compilan sin errores ni nuevas advertencias (217/2 existentes).
Las nuevas pruebas completas de Examine→evento→malla TMP están añadidas pero no ejecutadas
en Play Mode en esta sesión; falta confirmar el resultado visible con el mando real.

Auditoría Clue05: el asset level01.revelation ya almacena SecretCommand semántico, con texto
«Ya conoces la llave.». CluePresentation ya resolvía SecretCommandAction mediante
InputPromptResolver y el binding efectivo; Journal y discovery ya escuchaban ActiveDeviceChanged.
No se encontró una G universal ni confusión con Interact en esta ruta.

La presentación también refresca ahora ante BoundControlsChanged, incluso sin cambiar de
dispositivo ni cerrar/reabrir el Journal. Las suscripciones se retiran al desactivar el presenter;
la notificación exige referencias al reader y al manager. Renderizar nunca modifica ClueTracker.
Clue05 mantiene su gate de Puzzle04/Zone05 y enseña una acción que ya existe desde el inicio.

| Dispositivo | Interact | SecretCommand revelado por Clue05 |
| --- | --- | --- |
| Teclado | E | G |
| Xbox | X / buttonWest | Y / buttonNorth |
| PlayStation | □ / buttonWest | △ / buttonNorth |
| Generic | Botón oeste | Botón norte |

Los bindings siguen siendo Keyboard/e, Keyboard/g, Gamepad/buttonWest y Gamepad/buttonNorth.
Los símbolos los produce el resolver existente. ActiveDeviceChanged vuelve a renderizar la pista
descubierta en el Journal abierto y en la notificación visible; no exige redescubrimiento.
DynamicPromptValidation comprueba las ocho combinaciones de texto y sus bindings efectivos.
FinalPuzzlesRedesignValidation comprueba semántica, referencias, Missing Scripts, gate y refresco
en vivo del Journal/discovery, incluyendo override y restauración con el mismo dispositivo.
Estas pruebas interactivas deben ejecutarse en Unity; compilar no equivale a ejecutarlas.

Verificación ejecutada sin batchmode: auditoría de los cuatro bindings y del asset semántico,
seis comprobaciones del método real del resolver para nombres de mando, compilación runtime
y Editor sin errores ni advertencias nuevas (217/2 ya existentes), y git diff --check.
Las comprobaciones visuales, de Missing Scripts y del Journal en Play Mode quedan pendientes.

PuzzleBase → PuzzleClueReward → ClueTracker → ClueDiscovered. La pista permanece registrada en el tracker durante la sesión; ClueDiscoveryPresenter muestra la notificación temporal durante seis segundos, y ClueJournalPresenter permite consultarla posteriormente. El diario no descubre pistas ni resuelve puzzles. Las inscripciones siguen siendo ayuda temporal independiente.

ClueTracker conserva su diccionario por ID y mantiene el orden de descubrimiento. DiscoveredClues expone una vista IReadOnlyList respaldada por ReadOnlyCollection; los consumidores no pueden modificarla. Count cuenta solo descubrimientos y TryGetClue consulta por ID. Discover deduplica antes de notificar. No hay un segundo inventario de pistas en la UI.

El diario ordena por ClueDefinition.Order. Los empates conservan el orden de descubrimiento mediante ordenación estable, también después de restaurar un snapshot. Solo renderiza pistas descubiertas; no contiene una lista completa de pistas, casillas bloqueadas, placeholders, número de pendientes ni spoilers.

## Snapshot y futura persistencia

ClueProgressSnapshot es un DTO serializable que copia y expone únicamente discoveredClueIds mediante una vista de solo lectura. CaptureSnapshot devuelve IDs en orden de descubrimiento, sin títulos, descripciones ni referencias de Unity.

TryRestoreSnapshot recibe un snapshot y un catálogo de ClueDefinition conocidas. Resuelve y valida todos los IDs antes de sustituir el progreso: un ID desconocido o un catálogo ambiguo rechaza la operación sin pérdida del estado anterior. IDs repetidos del snapshot se deduplican conservando la primera aparición. Un snapshot vacío restaura una sesión sin pistas.

Al terminar emite ProgressRestored una vez; no emite ClueDiscovered. Así, el diario se refresca si está abierto y no se repiten notificaciones de descubrimiento/recompensas. El SaveManager general futuro deberá guardar/cargar este DTO junto con el resto del estado del juego y proporcionar el catálogo. Restaurar pistas no restaura por sí solo puzzles, ubicación del jugador o puertas.

No se implementó SaveManager, almacenamiento en disco ni archivos JSON independientes. El test usa un roundtrip JSON únicamente en memoria para comprobar el contrato serializable.

## Input y gameplay

Journal pertenece a Global, tipo Button:

- KeyboardMouse: <Keyboard>/j.
- Gamepad: <Gamepad>/select, control selectButton validado en el paquete instalado Input System 1.20.0; equivale a View/Select en Xbox y Share en PlayStation.

El ScriptedImporter oficial regenera TikalInputActions.cs en su ubicación existente y namespace Tikal.Input. El wrapper no se edita manualmente. PlayerInputReader e IPlayerInputReader exponen JournalPressed; PlayerInputReader.JournalAction sirve únicamente para resolver la indicación visual mediante InputPromptResolver.

ClueJournalPresenter escucha ese evento y alterna apertura/cierre sin conocer bindings físicos. Usa PlayerController.SetGameplayActive(false) para detener movimiento, cámara e interacción y liberar cursor. Global permanece habilitado para cerrar con la misma acción. Al cerrar restaura el estado previo de gameplay y cursor; si gameplay ya estaba deshabilitado, sigue deshabilitado. OnDisable cierra el diario y retira suscripciones. No altera Time.timeScale ni crea un Pause Menu.

La indicación de cierre utiliza el resolver existente: J / View / Share según dispositivo. Los prompts E/X/□ y las acciones Interact/SecretCommand mantienen sus bindings. No se añadió un consumidor de SecretCommand.

## Organización visual

Un solo Canvas y Canvas Scaler existentes:

- InteractionPrompt.prefab continúa independiente, con Context anclado y pivotado abajo izquierda, posición (50,50) en píxeles de referencia y texto alineado a izquierda. No es una coordenada absoluta de una única resolución.
- Discovery panel queda arriba al centro, pivot superior y margen superior de 50 píxeles de referencia. Conserva DESCUBRIMIENTO, título, texto y seis segundos.
- ClueJournal.prefab contiene su presentación independiente: velo, tarjeta con márgenes proporcionales, encabezado PISTAS, contenido legible y acción de cierre. No convierte todo el HUD en un único prefab.
- El centro no contiene un prompt o notificación permanentes. No se implementó retículo.

La lectura vacía indica que todavía no hay pistas. Los títulos descubiertos aparecen en mayúsculas y las descripciones conservan su texto, con separación vertical. TMP tiene richText desactivado y tamaño adaptativo para las tres pistas actuales. No existe polling por frame: el contenido se actualiza al abrir, descubrir una pista o restaurar progreso; el hint cambia al seleccionar dispositivo.

Mientras el diario está abierto el mapa Player está deshabilitado: PlayerInteraction pierde foco y InteractionPromptPresenter comprueba además GameplayEnabled para ocultar inmediatamente el prompt. Las notificaciones pueden vencer normalmente y las pistas continúan en el tracker.

## Contenido actual

01 — EL CAMINO  
El camino verdadero no siempre está delante de ti.

02 — EL ORIGEN  
Todo camino recuerda dónde comenzó.

03 — EL PRIMER CONTACTO  
Lo primero que tocaste aún guarda algo.

Cada entrada aparece solamente cuando su ID ha sido descubierto. El diario no revela Puzzle04 o Puzzle05.

## Validación

JournalValidation comprueba acumulación/deduplicación, búsqueda, vistas de solo lectura, orden estable con empates, snapshots copiados y restauración atómica, roundtrip serializable en memoria y ausencia de notificaciones al restaurar. Valida apertura/cierre con dispositivos nativos sintéticos Keyboard, XInput y DualShock, bloqueo de movimiento/look/interacción, conservación de Global, cursor, estado previo, actualización dinámica y listeners tras Enable/Disable.

Comprueba anclajes, separación de los paneles a 1920×1080, 1280×720, 1280×960, 2560×1080 y 3840×1080 mediante el modelo del Canvas Scaler, convivencia con descubrimientos, ocultamiento con diario abierto, retención al vencer la notificación y prompts E/X/□.

Regresiones completas: EchoValidation, InscriptionValidation, PuzzleFrameworkValidation, MuralValidation, GuardiansValidation, InteractionValidation (Input/Player/Dynamic Prompts), ExplorerFeelValidation y Level01Validation (puerta, recorrido físico hasta Zone05 y backtracking). Unity batchmode debe terminar con código 0, sin errores C#, Missing Scripts, referencias críticas rotas ni assertions fallidas.

## Alcance

La feature parte de develop después del merge normal de Puzzle03 y sus regresiones. Se corrigió únicamente una espera de test de audio que era sensible al arranque frío del Editor: espera la finalización real con timeout, conservando las comprobaciones de tres notas deterministas y el runtime original.

No se modifican main, Packages ni ProjectSettings. No hay Puzzle04, Pause Menu, persistencia final, audio nuevo ni nuevas mecánicas. La feature del diario permanece separada de develop hasta revisión posterior.


## Resultado del checkpoint

- JournalValidation: 99 comprobaciones aprobadas.
- EchoValidation: 126; InscriptionValidation: 18; PuzzleFrameworkValidation: 76; MuralValidation: 267; GuardiansValidation: 293.
- Input/Player/Interaction/Dynamic Prompts, Explorer Feel y Level01 navigation/backtracking: aprobados.
- Los nueve procesos de la validación final terminaron con código 0, sin errores C#, Missing Scripts, referencias críticas rotas o assertions fallidas.
- Preview GPU del diario renderizada e inspeccionada: las tres entradas, acentos y cierre son legibles; el render también terminó con código 0. La preview no guarda cambios de escena ni assets.
- La revisión del InputActionAsset conserva exactamente IDs, propiedades, bindings y schemes anteriores. Solo añade Global/Journal con dos bindings; importador/wrapper mantienen metadatos y GUIDs.
- La revisión de la escena conserva sus 2313 bloques existentes y añade la instancia independiente de UI; no se eliminan objetos del nivel.
- Snapshot no escribe archivos y restaurar IDs no genera notificaciones nuevas.
- Se preservan por separado el estado previo del coordinador PlayerController, el mapa Player y el cursor al cerrar el diario.
- Warnings preexistentes: UAC0009 en componentes de debug y avisos Git LF/CRLF. Unity también registra avisos de extensiones de exportación WindowsStandalone/WebGL; no se ejecutó ni se verificó un build exportable en esta tarea.

## Extensión: La llave

`ClueDefinition` mantiene `Narrative` como valor predeterminado y permite `ClueSemanticContent.SecretCommand`. `ClueDiscoveryPresenter` y `ClueJournalPresenter` usan `CluePresentation` e `InputPromptResolver` para mostrar el binding actual de la acción semántica.

`ActiveDeviceChanged` refresca también el cuerpo del Journal abierto. La pista conserva un único ID (`level01.clue05`) aunque cambie entre Keyboard, Xbox, PlayStation o gamepad genérico.
