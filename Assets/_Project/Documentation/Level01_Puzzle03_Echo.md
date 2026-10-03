# Level 01 — Puzzle 03: Cámara del Eco

## Concepto y recorrido

En Zone03_EchoChamber se escucha una secuencia breve y se devuelve usando tres piedras sonoras. EchoSource ocupa el foco central (X=33, Z=43), con las piedras alrededor fuera del corredor X≈29. Se reutilizan PuzzleAnchor, AudioAnchor y ClueAnchor como padres de objetos nuevos; los anchors conservan únicamente su Transform. La salida hacia Zone04 permanece abierta.

Una inscripción opcional reutiliza ReadableInscription e InscriptionPresenter: **Escucha. Después, devuelve el eco.** El prompt es Leer y el texto aparece cinco segundos en el Canvas existente. No otorga una pista ni cambia ningún estado de puzzle. Texto español y modelos son provisionales; no son escritura ni sonidos mayas históricos.

## Secuencia y audio provisional

La secuencia configurada en Inspector es **B → A → C**, exactamente tres notas. EchoSequencePuzzle admite una secuencia de tres notas válidas; las referencias se validan al habilitarse. EchoSource, con prompt **Escuchar**, reproduce los clips secuencialmente, excluye interacciones simultáneas y vuelve a estar disponible al terminar. Se puede escuchar ilimitadamente antes de resolver. Escuchar otra vez limpia el intento actual y cancela tonos anteriores para que la referencia se oiga sin superposición.

| Piedra | Frecuencia | Forma | Duración |
| --- | --- | --- | --- |
| A | 220 Hz, grave | Losa cuadrada baja | 0,45 s |
| B | 440 Hz, medio | Resonador redondo | 0,45 s |
| C | 660 Hz, agudo | Resonador alto bifurcado | 0,45 s |

Los WAV propios se generan con seno, envolvente de entrada/salida y amplitud 0,24, PCM mono de 44,1 kHz. No se importó audio externo. La pausa entre notas de referencia es 0,3 s; la escucha completa tarda aproximadamente 1,95 s. Los AudioClip son referencias serializadas reemplazables. AudioSource usa volumen moderado, mezcla espacial ligera, alcance limitado y Doppler desactivado. No se añadió AudioManager.

## Arquitectura y reintentos

EchoSequencePuzzle deriva de PuzzleBase en Tikal.Puzzles.Level01. Contiene la secuencia y el progreso, recibe elecciones de sus tres EchoStone y llama al TrySolve protegido únicamente tras tres aciertos consecutivos. No conoce controles físicos, UI ni ClueTracker. Comienza Available; escuchar o tocar inicia Active. Se permite intentar sin escuchar previamente para no introducir un requisito invisible.

EchoStone implementa IInteractable, con prompt **Tocar**. Cada elección reproduce su tono y produce un pulso de emisión discreto. Se impide repetir esa piedra mientras su tono sigue activo. Las tres formas utilizan el mismo material, por lo que su identidad no depende únicamente de color.

Una nota incorrecta limpia el progreso y produce un pulso central tenue de 0,2 s. No cambia de escena, no quita pistas y no penaliza al jugador. Al terminar el tono puede reintentar o volver a escuchar. EchoProgressFeedback escucha eventos y controla tres marcas ambientales: se iluminan con los aciertos, se apagan tras error/reescucha y permanecen iluminadas al resolver. No hay contador numérico ni polling por frame.

Solved es terminal: fuente y piedras dejan de aceptar acciones de puzzle, Reset y Begin se rechazan, y la recompensa no puede repetirse. Desactivar la fuente cancela su coroutine y audio; Reset de un intento activo cancela sonidos y limpia progreso.

## Accesibilidad e input

ReferenceNotePlayed en EchoSource y NotePlayed en EchoStone exponen identidades semánticas para una futura opción de indicadores visuales de audio. No se instaló un observador visual de la secuencia de referencia, ni menú. Los pulsos actuales corresponden solamente a las elecciones del jugador y a su progreso/error.

PlayerInteraction y el presenter existentes generan **Escuchar / Tocar / Leer** con E, X y □ según el dispositivo. Ningún componente del puzzle contiene bindings físicos. SecretCommand no tiene consumidores ni efecto sobre este mecanismo.

## Clue03 y descubrimiento

PuzzleClueReward, ubicado bajo ClueAnchor, conecta el puzzle con el ClueTracker de sesión compartido. La definición existente level01.echo_chamber.asset conserva su GUID y orden 3, con:

- ID: **level01.clue03**.
- Título: **El primer contacto**.
- Texto: **Lo primero que tocaste aún guarda algo.**

Se preservan Clue01 y Clue02. El tracker deduplica por ID. Se reutiliza exactamente ClueDiscoveryPresenter y su notificación DESCUBRIMIENTO durante seis segundos; no hay otro Canvas, diario o pista otorgada por leer.

## Dificultad y límites

Objetivo humano: 15–40 segundos para quien presta atención y 30–90 segundos para un jugador nuevo. Los tests automatizados verifican reglas y tiempos técnicos, pero no miden comprensión, memoria ni percepción auditiva humana. La escucha y distinción final de tonos deben revisarse con personas y equipos de audio reales. Se mantiene una secuencia de solo tres notas y reescucha ilimitada.

## Validación

EchoValidation.Run comprueba estructura y referencias, formas distintas, PCM/duración/frecuencias, tres replays reales deterministas usando teclado/XInput/DualShock, prompts nativos, bloqueo de superposición, ausencia de solución visual durante la escucha, error/retry, progresión ambiental, solución única, Clue03 única y conservación de las pistas anteriores obtenidas resolviendo sus puzzles. Verifica ambas ayudas, aislamiento de SecretCommand y cierre de la notificación existente.

Regresiones: InscriptionValidation, PuzzleFrameworkValidation, MuralValidation, GuardiansValidation, InteractionValidation (Input/Player/Dynamic Prompts), ExplorerFeelValidation y Level01Validation. La última recorre físicamente hasta Zone05, vuelve y comprueba la puerta sin teletransportar al personaje.

EchoSceneAuthoring es una herramienta explícita de instalación con rechazo de duplicados; no modifica el proyecto al importar. Crea escena, audio y material propios mediante APIs del Editor. No se alteran Packages, ProjectSettings, escenas de pruebas ni puzzles futuros. Puzzle03 permanece en su propia rama sin merge a develop/main.

## Resultado del checkpoint

- EchoValidation: 126 comprobaciones de runtime aprobadas, además de la validación estática de escena/audio/referencias previa al Play Mode; Unity batchmode código 0.
- InscriptionValidation: 18; PuzzleFrameworkValidation: 75; MuralValidation: 267; GuardiansValidation: 293 comprobaciones aprobadas.
- Interaction/Input/Player/Dynamic Prompts, Explorer Feel y Level01 navigation/backtracking: aprobados. Cada proceso terminó con código 0.
- La prueba de navegación recorre físicamente la ayuda, el acceso alrededor del tabique existente, la fuente y las tres piedras, luego continúa hasta Zone05 y regresa. No teletransporta al personaje.
- La revisión por IDs conserva los 2214 bloques serializados existentes; añade 93. Los únicos cambios a bloques anteriores son listas de hijos de Zone03/anchors y la segunda fuente del presenter de inscripciones.
- Sin errores C#, Missing Scripts, referencias críticas rotas o assertions fallidas en la validación final. Se conservan los warnings UAC0009 preexistentes de componentes de debug y avisos Git LF/CRLF.
- La API de precarga de audio se configura mediante AudioImporterSampleSettings, compatible con Unity 6.6. Se ajustaron los objetos nuevos para permitir interacción dentro del alcove sin alterar su tabique.
- Tiempos humanos y percepción auditiva siguen pendientes de playtest; los tiempos técnicos no equivalen a tiempo humano de resolución.
