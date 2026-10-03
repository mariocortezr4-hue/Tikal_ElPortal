# Level 01 — Puzzle 01: Mural de los fragmentos

## Concepto y experiencia

Cuatro fragmentos de piedra reconstruyen un círculo con cuatro trazos radiales. Cada pieza aporta un cuarto distinto del dibujo; una pequeña talla intacta en el borde inferior permite inferir la composición sin instrucciones escritas. El dibujo dorado es relieve blockout, no arte final.

El mural está bajo `Zone01_MuralFragments/PuzzleAnchor`, mirando hacia el sur de la plaza, detrás de la plataforma existente. Se puede observar desde el acceso o acercándose a sus escalones. El camino central hacia Zone02 sigue abierto, sin puertas adicionales ni barreras ligadas al puzzle. Las otras cuatro zonas conservan sus objetos y geometría.

La intención es observar → interactuar → deducir → recibir pista → continuar explorando.

## Reglas y configuración

`MuralFragmentsPuzzle : PuzzleBase`, namespace `Tikal.Puzzles.Level01`, referencia exactamente cuatro `MuralFragment` distintos. `_solution` es un array de cuatro índices configurables en Inspector; la validación rechaza referencias duplicadas/nulas o índices fuera de 0–3.

Orden: A superior izquierda, B superior derecha, C inferior izquierda, D inferior derecha.

| Pieza | Inicial | Solución | Giros mínimos |
| --- | --- | --- | --- |
| A | 2 | 1 | 3 |
| B | 0 | 3 | 3 |
| C | 1 | 0 | 3 |
| D | 3 | 2 | 3 |

Los cuatro estados iniciales son incorrectos, deterministas y sin aleatorización. El puzzle empieza Available. La primera interacción lo lleva a Active y realiza el primer giro en esa misma interacción. Cada giro avanza 0 → 1 → 2 → 3 → 0 en 0,3 segundos, con interpolación suave y ajuste final absoluto del quaternion. La comparación de solución utiliza exclusivamente índices enteros.

El índice se confirma al terminar el movimiento visual. La solución requiere todas las piezas activas, sin rotaciones pendientes y con su índice correcto; llama al `TrySolve()` protegido del framework. Solved es terminal: ya no se permite girar ni reiniciar. El reset durante Active cancela cualquier animación pendiente y devuelve las cuatro piezas a sus índices iniciales y al estado Available.

El dibujo de cada pieza está diseñado para su índice de solución. Cambiar `_solution` sin reorientar el relieve de referencia cambiaría la relación entre la condición lógica y la solución visible; deben editarse juntos al diseñar nuevas variantes. El pivote visual debe mantener escala unitaria y su orientación cero representa el espacio local del soporte.

## Interacción y animación

`MuralFragment` implementa `IInteractable`, expone texto semántico **Girar** y delega al propietario antes de iniciar un giro. No lee controles físicos ni conoce teclas, familias de mando o comandos secretos. `PlayerInteraction` y `InteractionPromptPresenter` conservan la autoridad para raycast, distancia, bloqueo por obstáculos y presentación E/X/□ según el dispositivo activo.

Cada fragmento utiliza una sola coroutine de animación, únicamente mientras gira. Un token invalida cualquier continuación antigua si se cancela/reinicia. Las interacciones adicionales se ignoran durante el giro; desactivar una pieza cancela el movimiento y vuelve al último índice confirmado. Los eventos `OrientationChanged` y `RotationFinished` se emiten al terminar cada giro aceptado. No hay polling de input ni Update vacío en el framework.

## Resolución y pista

El movimiento constituye el feedback provisional de cada interacción. `MuralSolvedFeedback` escucha los eventos del puzzle y activa una luz cálida al resolver; la lógica de orientación no muestra UI.

`PuzzleClueReward` compone el puzzle y el `ClueTracker` de sesión, de forma independiente de `PuzzleBase`. La recompensa utiliza el asset existente `Data/Clues/level01.mural_fragments.asset`:

- ID: `level01.clue01`.
- Título provisional: **El camino**.
- Texto provisional: **El camino verdadero no siempre está delante de ti.**

La pista no revela controles secretos, una solución posterior ni un destino obligatorio. El tracker impide duplicados por ID.

`ClueDiscoveryPresenter` escucha `ClueDiscovered`, muestra DESCUBRIMIENTO, título y texto durante seis segundos reales y después oculta el panel. Es una presentación provisional reutilizable, sin diario, navegación, puntuación ni persistencia. Utiliza el Canvas contextual y la fuente TMP existentes; no captura raycasts ni añade un segundo Canvas. Un descubrimiento posterior reemplaza la notificación actual.

## Dificultad y métricas

Objetivo de diseño: fácil/media-baja, <60 segundos para quien entiende el patrón y 30–120 segundos para un jugador nuevo. Estos tiempos requieren pruebas con personas; las pruebas automatizadas no validan comprensión, dificultad o ritmo humano.

Se registran `RotationCount` y `ElapsedSeconds` desde la primera interacción hasta la resolución, usando tiempo de juego escalado. El contador registra giros completados; los intentos ignorados durante el movimiento no cuentan. Reset inicia un nuevo intento. En Editor/Development Build se emite `MURAL_METRICS` al resolver. No hay puntuación ni ajuste automático de dificultad.

La ruta determinista mínima requiere 12 giros y 3,6 segundos de animación si se giran las piezas secuencialmente; pueden girarse piezas diferentes simultáneamente. Esta cifra es un presupuesto mecánico, no el tiempo esperado de un jugador. Registrar en playtests: tiempo, giros, dudas sobre la referencia, errores de selección y decisión de continuar sin resolver. Revisar composición o dificultad únicamente a partir de esa evidencia.

## Validación

`Tikal/Tests/First mural and clue` (`MuralValidation.Run`) valida referencias, cuatro piezas, índices iniciales, giros cíclicos, estabilidad de rotación, repetición de input, reset y cancelación, condición de solución, resolución única, estado terminal, bloqueo, recompensa sin duplicados, presentación y desaparición de pista, luz y controles nativos simulados mediante Input System.

La prueba de animación deja avanzar frames reales de Unity; las pruebas repetitivas avanzan el mismo método privado de interpolación con pasos deterministas de 0,02 segundos. Las pruebas de prompt utilizan el raycast real contra cada collider, el input reader, teclado, XInput y DualShock. Los comandos secretos no producen cambios en el mural.

Regresiones complementarias: `PuzzleFrameworkValidation.Run`, `InteractionValidation.Run` (incluye Input/Player/Dynamic Prompts), `ExplorerFeelValidation.Run` y `Level01Validation.Run` (recorrido físico hasta Zone05 y backtracking, puerta inicial, desniveles y prompts). No se teletransporta el jugador en la prueba de navegación. `MuralSceneAuthoring.Run` es una herramienta explícita de instalación inicial; no corre al importar y rechaza una segunda instalación.

## Alcance

Solo Puzzle01. No se añadieron consumidores de comandos secretos, otras recompensas narrativas, gates hacia Zone02, Puzzle02–05, audio final, VFX final, inventario o diario. Packages, ProjectSettings, escenas de pruebas y main permanecen sin cambios.

## Resultado del checkpoint

- MuralValidation: 267 comprobaciones, Unity batchmode código 0.
- PuzzleFrameworkValidation: aprobado.
- InteractionValidation: aprobado, incluyendo Input/Player y Dynamic Prompts.
- ExplorerFeelValidation: aprobado.
- Level01Validation: aprobado; las cinco zonas, puerta inicial y backtracking siguen funcionando.
- Todos los procesos batchmode de la regresión finalizaron con código 0, sin errores C#, Missing Scripts ni referencias críticas rotas.
- Métrica automatizada: solución mínima de 12 giros, 3,6 segundos de animación secuencial. No se midió dificultad con jugadores humanos.
- Revisión de la escena por IDs serializados: ningún bloque existente eliminado; solo se ampliaron PuzzleAnchor, Systems y el prefab contextual para incorporar los objetos nuevos del mural y sus referencias/presentación.
- Warnings de compilación preexistentes UAC0009 sobre DEVELOPMENT_BUILD en InteractionDebugPrompt y PuzzleFrameworkDebugControls; el código nuevo utiliza Debug.isDebugBuild y no añade ese warning.
