# Level01 — La Entrada Secreta

## Diseño para el jurado
El final plantea una decisión humana: continuar por el paso revelado o regresar al primer contacto. Las pistas enseñan la solución; no desbloquean el comando mediante una condición de progreso.

| Pista | Texto |
|---|---|
| El camino | El camino verdadero no siempre está delante de ti. |
| El origen | Todo camino recuerda dónde comenzó. |
| El primer contacto | Lo primero que tocaste aún guarda algo. |
| El séptimo lugar | La respuesta ocupa el séptimo lugar entre las letras. |
| El ciclo | El inicio es el final. |

La deducción canónica relaciona la séptima letra (G), la puerta original y el inicio del recorrido. El jugador puede consultar las cinco pistas ordenadas en el Journal y volver físicamente por todas las zonas. No hay flecha, waypoint, mensaje de regreso ni prompt secreto.

## Entrada y comando secreto
SecretEntranceCommandReceiver escucha IPlayerInputReader.SecretCommandPressed: G, Xbox Y, PlayStation △ y buttonNorth en un mando genérico. No modifica Door, IInteractable ni los bindings. La interacción normal E/X/□ conserva Abrir/Cerrar.

Las referencias a jugador, cámara, target, zona, colliders móviles y outcome son explícitas. La validación se ejecuta al pulsar, sin Update, búsquedas globales ni raycasts continuos:
- target original (0, 1.6, 0.05);
- zona BoxCollider con centro (0, 1.5, 0) y tamaño (3.4, 3, 5.8);
- la zona contiene la posición del jugador más un metro vertical;
- distancia cámara–target mayor que 0.001 y hasta 3 m;
- dot de la cámara hacia el target al menos 0.7;
- un RaycastNonAlloc de línea de visión ignora triggers, el propio jugador y exclusivamente la hoja móvil de la puerta. Cualquier otra obstrucción bloquea el comando; una saturación de 32 impactos también lo bloquea.

La orientación permite aproximadamente 45° de tolerancia. La hoja móvil no determina el target: funciona con puerta cerrada, abierta, abriéndose o cerrándose, desde ambos lados si se cumplen las condiciones. No hay dependencia de Puzzle01–05, ClueTracker ni progreso del Journal. El comportamiento modal existente del Journal pausa el mapa Player mientras está abierto.

Una partida nueva puede ganar directamente junto a la puerta, con cero pistas y todos los puzzles sin resolver. Este speedrun es intencional.

## Camino aparente y advertencia
FalsePathReveal responde al evento PuzzleSolved de Puzzle05. Antes, el sello lateral bloquea físicamente el paso y FalsePath está inactivo; después, el sello desaparece y el corredor se activa. Resolver Puzzle05 no termina el nivel.

La apertura está en X=6, Z=78. Se ajustan las divisiones de la pared posterior y el talud, y un bloque visual de vegetación, conservando el monumento y el regreso por las cinco zonas. Se usan materiales propios existentes. El corredor entre roca tiene suelo hasta Z=98 y una apariencia provisional neutra.

FinalWarningStele_Root, en (4.9, 0.6, 83.5), separa VisualProxy del collider y ReadableInscription. Su prompt es **Leer** y su texto funcional:
> Si continúas, termina tu recorrido.

No entrega Clue06 ni registra texto en el Journal. El jugador puede leer y regresar sin derrota. El arte final sustituirá exclusivamente VisualProxy; deben preservarse root, collider, texto y referencias de interacción. La futura escritura inspirada en cultura maya requiere revisión cultural e histórica; este proxy no pretende representar una inscripción auténtica.

## Punto sin retorno y desenlaces
El centro del trigger está en (6, 1.8, 92); su tamaño es (3.2, 2.4, 1.2). Hay 8.5 m de separación longitudinal desde la estela hasta el centro y 7.9 m hasta el frente del trigger. El jugador dispone de espacio real para decidir regresar.

PointOfNoReturnTrigger acepta exclusivamente el CharacterController referido. Al cruzarlo, aparece un cierre detrás en Z=88.5 y se emite Defeat una sola vez. No hay muerte gráfica.

LevelOutcomeController encapsula Playing/Victory/Defeat y emite OutcomeChanged. Solo la primera transición tiene efecto; un resultado no puede reemplazar al otro. No hay persistencia ni GameManager general. Victory ilumina provisionalmente el sello de Entrance con emisión cálida.

LevelResultPresenter muestra un fade de 0.45 s, con tiempo no escalado:
- Victory: **NIVEL COMPLETADO**, **La Entrada Secreta**.
- Defeat: **HAS QUEDADO ATRAPADO**.
- Ambos: **REINTENTAR**.

Al terminar se desactivan gameplay, interacción y Journal; se ocultan retículo, prompts e inscripciones y se libera el cursor. Un EventSystem con InputSystemUIInputModule utiliza las acciones UI existentes y referencias persistentes oficiales del asset. El botón seleccionado admite teclado, ratón y gamepad. No hay botón de Main Menu.

## Reinicio
REINTENTAR recarga Level01_SecretEntrance mediante SceneManager. Restablece jugador, puerta, cinco puzzles, pistas, false path, outcome, interfaz y retículo. No guarda la sesión.

La escena se agrega mediante EditorBuildSettings a las escenas habilitadas para permitir esta recarga también en un ejecutable. La entrada previa SampleScene se conserva.

## Validación
EndingValidation comprueba condiciones espaciales, línea de visión, cuatro estados de Door, cuatro familias de dispositivo, ausencia de gating por pistas, idempotencia, warning sin recompensa, trigger físico, UI y recargas reales desde botones nativos.

Los recorridos integrales usan CharacterController y simulación física, sin teletransportar el jugador ni asignar estados de puzzles:
1. Canónico: interacción normal con Door, cinco puzzles reales, Journal con cinco pistas, lectura y regreso desde la advertencia, backtracking y victoria en la puerta.
2. Derrota: cinco puzzles, advertencia, cruce físico del trigger, derrota y reintento.
3. Speedrun: aproximación desde spawn y victoria sin pistas.

Las pruebas semiautomatizadas aceleran animaciones de rotación invocando sus ticks de producción; utilizan input nativo para interactuar y no sustituyen una prueba humana de ritmo y presentación. También se ejecutan las suites previas de Input/Player, Game Feel, Interaction/Dynamic Prompts, Framework, Puzzle01–05, Journal, Comfort y navegación. Sprint mantiene Shift y pulsación LS/L3.

La herramienta de preview renderiza imágenes temporales mediante URP y no guarda sus cambios de presentación. El scene authoring utiliza exclusivamente APIs de Unity para serializar escena y configuración; no edita YAML manualmente.

### Resultado del checkpoint
Unity 6000.6.3f1: las 13 suites terminaron con código 0. EndingValidation pasó 343 comprobaciones y los tres recorridos integrales. Las regresiones de navegación conservan los 42 colliders de roca originales y verifican el collider adicional de la división oriental, sólido y basado en la misma malla propia.

Se revisaron renders temporales URP del corredor y de las pantallas Victory/Defeat. Persisten los dos warnings UAC0009 previos por DEVELOPMENT_BUILD en herramientas de debug ajenas a este cambio. Git puede informar normalización LF/CRLF según su configuración existente.