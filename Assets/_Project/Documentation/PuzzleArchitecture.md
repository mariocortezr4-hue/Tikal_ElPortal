# Arquitectura reusable de acertijos

## Objetivo y límites

El framework modela ciclos de vida y descubrimientos de una sesión. No conoce teclas, escenas, puertas, HUD, guardado ni gestores globales. Los cinco puzzles de Level01 tienen definiciones de datos. El primer mural ya utiliza el framework en Zone01; sus reglas concretas se documentan en [Level01_Puzzle01_Mural.md](Level01_Puzzle01_Mural.md). El segundo puzzle utiliza el mismo framework en Zone02; véase [Level01_Puzzle02_Guardians.md](Level01_Puzzle02_Guardians.md). Los puzzles 03–05 conservan únicamente sus definiciones.

## Estado y ciclo de vida

`PuzzleBase` es un MonoBehaviour abstracto en `Tikal.Puzzles`. Su estado privado se expone mediante `State` e `IsSolved`, sin setter. `PuzzleDefinition` es un ScriptableObject con ID legible y nombre descriptivo. El ID se captura durante Awake: no depende del nombre del GameObject ni cambia durante la ejecución aunque se edite la definición.

| Transición | API | Política |
| --- | --- | --- |
| Inactive → Available | `MakeAvailable()` | Permite ofrecer el puzzle; repetir devuelve false |
| Available → Active | `BeginPuzzle()` | Comienza una vez por intento |
| Active → Solved | `TrySolve()` protegido | Solo una clase derivada decide la condición |
| Active → Available | `ResetPuzzle()` | Solo cuando `_allowReset` está habilitado |

Las instancias desactivadas no aceptan transiciones. El estado inicial configurado admite Inactive o Available; otros valores se normalizan a Inactive. Solved es terminal durante esa ejecución. Reset no borra una resolución ni pistas obtenidas, y no emite eventos si la transición no procede. No existe Failed porque todavía no hay una condición de fracaso concreta.

Las operaciones devuelven bool para distinguir una transición válida de una petición rechazada. Cambiar State está reservado a la base. Un guard evita transiciones reentrantes durante hooks y notificaciones: los observadores ven el nuevo estado, pero no pueden reiniciar/resolver otro ciclo dentro de la misma transición. Los hooks deben terminar normalmente; las excepciones siguen el comportamiento habitual de eventos C# y no hay un bus de errores global.

## Template methods y eventos

La base centraliza el cambio de estado y llama hooks virtuales protegidos: `OnPuzzleBegan`, `OnPuzzleSolved`, `OnPuzzleReset` y `OnPuzzleInteraction(elementId)`. Los derivados personalizan comportamiento sin reimplementar el ciclo de vida. Deben utilizar los hooks, no ocultar Awake ni reemplazar la gestión de estado.

Cada instancia expone eventos C#: `PuzzleStarted`, `PuzzleSolved` y `PuzzleReset`, con la instancia emisora como argumento. El hook se ejecuta después del cambio de estado y antes del evento. Begin, solve y reset repetidos no generan notificaciones duplicadas. No hay eventos estáticos, Singleton, EventBus, Update vacío ni búsquedas globales por frame.

## Composición con Interaction y varios elementos

`PuzzleBase` no hereda InteractableBase ni implementa IInteractable. `PuzzleInteractable` es un adaptador opcional que sí implementa IInteractable y referencia explícitamente un puzzle. Available permite comenzar; Active permite enviar un identificador de elemento; Inactive, Solved o un componente desactivado impiden interacción.

Varios adaptadores pueden apuntar al mismo puzzle con IDs de elemento distintos. `TryInteract(elementId)` entrega interacción semántica al hook derivado; no interpreta bindings ni condiciones de victoria. PlayerInteraction y los prompts existentes funcionan sin cambios. Los textos de interacción son configurables en el adaptador.

No se añadió IPuzzleElement: aún no existe una operación universal justificada para fragmentos, orientadores, piedras y símbolos. Cada puzzle futuro podrá poseer sus elementos por composición y resetearlos en su hook; un contrato común se añadirá únicamente si aparece una necesidad compartida concreta.

## Pistas y recompensas

`ClueDefinition`, en `Tikal.Puzzles.Clues`, contiene ID, título, descripción y orden. Es un ScriptableObject sin lógica visual. No se incluyó icono porque todavía no hay una UI que lo utilice.

`ClueTracker` mantiene un Dictionary por instancia, con comparación ordinal de IDs. `Discover(definition)` registra una pista nueva y emite `ClueDiscovered` una sola vez. Un segundo asset con el mismo ID también se deduplica. Ofrece `HasClue(id)` y `Count`; referencias nulas o IDs vacíos se rechazan sin modificar la colección. No implementa guardado, diario ni singleton. El tracker puede conectarse explícitamente a los consumidores que necesite cada sesión.

`PuzzleClueReward` compone puzzle, tracker y definición. Se suscribe a PuzzleSolved al activarse y se desuscribe al desactivarse. Si se reactiva después de que el puzzle se resolviera, entrega la pista pendiente; la deduplicación del tracker hace esta recuperación idempotente. PuzzleBase no depende del tracker y puede existir sin recompensas.

## Definiciones de Level01

En `Data/Puzzles` existen las cinco definiciones:

- `level01.mural_fragments`
- `level01.path_guardians`
- `level01.echo_chamber`
- `level01.glyph_gallery`
- `level01.revelation`

En `Data/Clues` hay cinco pistas. La del mural utiliza el ID estable `level01.clue01`, título provisional «El camino» y texto provisional «El camino verdadero no siempre está delante de ti.». La pista de guardianes utiliza `level01.clue02`, título «El origen» y texto provisional «Todo camino recuerda dónde comenzó.». Las otras tres mantienen IDs terminados en `.clue` y texto `[PLACEHOLDER]`. Ninguna revela controles del comando oculto; la narrativa sigue siendo provisional. Los IDs actuales se deben conservar para futuras referencias de progreso. No existe consumidor nuevo de SecretCommand.

## Testing y demostración POO

`Scenes/Testing/PuzzleFrameworkTest.unity` contiene Explorer, TestPuzzle, dos adaptadores de elementos, ClueTracker, recompensa, prompt existente y controles mínimos. La definición `testing.puzzle_framework` pertenece exclusivamente al fixture. TestPuzzle resuelve después de dos interacciones; `CompleteForTesting` permite comprobar resolución repetida. Nunca se coloca en Level01.

Los controles existen solo en Editor/Development. Al comenzar liberan el cursor mediante la API existente de PlayerController; el botón Explore habilita la exploración y la acción semántica Pause regresa a controles. Make available, Begin, Solve/repeat y Reset active permiten probar el ciclo. El resumen de ID/estado/cantidad se reconstruye por eventos o acciones del panel, no por polling ni generación de strings cada frame.

La prueba automática `Tikal.Tests.Editor.PuzzleFrameworkValidation.Run` reabre la escena guardada, verifica referencias y datos, luego valida estados, eventos únicos, reset, ID estable, dos derivados independientes, interacción real por el pipeline existente, múltiples elementos, pistas distintas y repetidas, y suscripciones/recompensas tras reactivar componentes. Un segundo derivado de soporte, PuzzleLifecycleProbe, existe únicamente en Editor y completa con un comportamiento diferente. Se ejecutan aparte Level01Validation y DynamicPromptValidation para las regresiones completas.

Para el jurado: PuzzleBase representa **abstracción**; los derivados muestran **herencia** y **polimorfismo** al compartir API con hooks diferentes; State privado y TrySolve protegido muestran **encapsulamiento**; adaptador y recompensa muestran **composición**; PuzzleSolved y ClueDiscovered muestran programación dirigida por **eventos**.

## Cómo añadir un puzzle futuro

1. Crear su clase derivada de PuzzleBase y asignar una definición con ID estable.
2. Mantener el progreso específico y las referencias a elementos en esa clase.
3. Personalizar los hooks necesarios; cuando la condición propia se cumpla durante Active, llamar TrySolve.
4. Usar PuzzleInteractable solo si necesita interacción directa. Cada elemento puede enviar su ID.
5. Añadir PuzzleClueReward solo si debe otorgar una pista, con tracker y asset explícitos.
6. Probar intentos inválidos, resolución repetida, reset permitido y aislamiento entre instancias.

No se han implementado acertijos reales, condiciones de victoria, guardado ni HUD final.

## Validación del checkpoint

Unity 6000.6.3f1 batchmode terminó con código 0 para PuzzleFrameworkValidation (63 comprobaciones), Level01Validation y la suite completa Input / Player / Game Feel / Interaction / Dynamic Prompts. Ambas escenas reabren correctamente y no presentan Missing Scripts ni referencias críticas rotas. Ningún archivo existente de Level01, runtime, Packages o ProjectSettings fue modificado por esta feature. Las herramientas temporales de construcción fueron retiradas.
