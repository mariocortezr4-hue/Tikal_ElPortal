# Level 01 — Puzzle 02: Los Guardianes del Camino

## Concepto y observación

Tres monolitos tienen formas geométricas distintas talladas en su frente, repetidas en su cara posterior para que la identidad pueda observarse aunque inicialmente estén de espaldas al acceso. Tres piedras ambientales repiten exactamente esas formas. El jugador observa las coincidencias y gira cada guardián hasta que su rostro mira hacia su marcador correspondiente. No hay instrucciones escritas, flechas ni significados históricos atribuidos a las formas.

La nariz, los ojos y la ceja blockout hacen legible la dirección frontal de cada monolito. Los símbolos se distinguen por forma, utilizando el mismo material de relieve: círculo dividido, contorno triangular y seis trazos radiales. Son referencias abstractas provisionales que deberán sustituirse por assets culturalmente revisados.

## Arquitectura y reglas

`PathGuardiansPuzzle : PuzzleBase`, namespace `Tikal.Puzzles.Level01`, posee tres `PathGuardian`, tres `DestinationMarker` y tres índices de solución. Verifica guardianes únicos, símbolos únicos, destinos únicos y correspondencia uno a uno. `DestinationMarker` solo expone el símbolo configurado, sin interacción ni polling.

`PathGuardian` implementa `IInteractable`, con texto **Girar**. Cada interacción aceptada avanza 0 → 1 → 2 → 3 → 0 alrededor del eje vertical local. Los índices equivalen a 0°/90°/180°/270°; la condición de resolución compara enteros, sin igualdad de floats.

La duración es 0,35 segundos, con interpolación suave y ajuste absoluto de la rotación final. El índice se confirma al terminar el giro; peticiones adicionales durante ese movimiento se ignoran. Una sola coroutine activa por pieza y un token de cancelación impiden continuaciones antiguas. Desactivar una pieza cancela la rotación y conserva el último índice confirmado. El collider de interacción permanece fijo en el soporte, mientras rota el cuerpo visual.

El puzzle empieza Available con A correctamente alineado, sin resolver. A mantiene su interacción: si el jugador lo gira, puede recuperar su orientación dando la vuelta al ciclo. La primera interacción comienza Active y realiza el giro. Solo tres guardianes correctos, activos y sin movimientos pendientes permiten llamar al `TrySolve()` protegido. Uno o dos alineados no bastan. Solved es terminal y bloquea todas las piezas. Reset durante Active cancela movimientos y devuelve el conjunto a su configuración inicial.

## Distribución espacial y solución

Todos los objetos nuevos pertenecen a `Zone02_PathGuardians/PuzzleAnchor/PathGuardians`. Se conservan montículos, vegetación, rutas, navegación y backtracking; no se añadió una barrera hacia Zone03. Las alturas se ajustan a los colliders de terreno existentes mediante la herramienta de authoring.

Orientaciones: 0 = norte (+Z), 1 = este (+X), 2 = sur (−Z), 3 = oeste (−X), con el soporte sin rotación global.

| Guardián / símbolo | Posición X,Z | Marcador X,Z | Inicial | Objetivo | Giros mínimos |
| --- | --- | --- | --- | --- | --- |
| A / círculo dividido | 32,5; 8 | 26,5; 8 | 3 | 3 / oeste | 0 |
| B / contorno triangular | 34,5; 11 | 37; 11 | 0 | 1 / este | 1 |
| C / seis trazos radiales | 31; 16 | 31; 4 | 0 | 2 / sur | 2 |

Configuración inicial **[3,0,0]**, solución **[3,1,2]**, **MinimumTurns = 3**: A correcto, B a un giro y C a dos. No hay aleatorización. Las tres direcciones finales difieren; las orientaciones oeste/sur insinúan de forma discreta la región de procedencia, sin señalar una puerta ni revelar otra mecánica.

Cambiar índices, posiciones o símbolos requiere mantener la correspondencia visual y espacial. La prueba de authoring también comprueba líneas de visión libres desde el acceso central (X=29, Z=12) al ejemplo A y su destino. Los símbolos de destino se amplían al 125 %, se elevan a 1,8 m sobre su base y se repiten detrás, manteniendo sus posiciones y formas. La prueba de authoring comprueba que la dirección cardinal de cada solución apunta al marcador que comparte su forma y que no existe un obstáculo sólido entre ambos. El tercer par está desplazado lateralmente para evitar que otro guardián tape su destino. La identidad posterior no indica una segunda dirección frontal: únicamente el rostro y la nariz definen hacia dónde mira el monolito. Los puntos de observación e interacción permanecen fuera del corredor central X≈29.

## Feedback y recompensa

El giro mecánico proporciona feedback inmediato. Una piedra baja central actúa como foco neutro; `GuardiansSolvedFeedback` escucha la resolución y activa una luz cálida discreta sobre ella. No hay audio ni VFX finales.

`PuzzleClueReward` entrega la definición existente `Data/Clues/level01.path_guardians.asset` al `ClueTracker` de sesión compartido:

- ID estable: **level01.clue02**.
- Título provisional: **El origen**.
- Texto provisional: **Todo camino recuerda dónde comenzó.**

La recompensa no modifica ni elimina Clue01. La deduplicación por ID sigue siendo responsabilidad del tracker. `PuzzleBase` permanece independiente de pistas y presentación.

Se reutiliza la única instancia de `ClueDiscoveryPresenter`: DESCUBRIMIENTO, título y descripción durante seis segundos reales. Una nueva pista reemplaza la notificación anterior. No se añadió una segunda UI, Canvas, diario o almacenamiento de progreso. `PlayerInteraction` y el presenter contextual existentes resuelven los controles E/X/□; las piezas no conocen input físico ni comandos secretos.

## Dificultad y métricas

Objetivo humano revisado: observador 20–40 segundos; jugador nuevo 30–75 segundos. El mecanismo debe enseñar su lógica mediante un ejemplo espacial, sin frustración ni búsqueda oculta. Estos tiempos y la claridad del concepto deben verificarse con personas; los tests automatizados no miden comprensión humana.

`RotationCount` registra giros completados; `ElapsedSeconds` mide tiempo de juego escalado desde la primera interacción hasta Solve; `AlignedCount` expone cuántos guardianes están correctos y quietos. No se muestran como puntuación, no ajustan dificultad y no revelan la solución en la UI del jugador. Reset empieza otro intento. En Editor/Development Build se registra `GUARDIANS_METRICS` al resolver.

La prueba determinista utiliza tres giros mínimos: 1,05 segundos de animación secuencial, sin contar observación, desplazamiento o selección. El tiempo 0,00 que puede registrar la fase de solución del test se debe al avance manual de la interpolación dentro de un mismo frame y no representa una experiencia humana. La primera rotación se comprueba aparte dejando avanzar frames reales de Unity.

## Pruebas y regresiones

`GuardiansValidation.Run` valida tres referencias únicas, cuatro estados cíclicos, ausencia de drift tras múltiples giros, rechazo de input repetido, reset y cancelación, configuraciones incorrectas, uno/dos/tres guardianes alineados, Solve único, bloqueo terminal, Clue02 sin duplicados y conservación de Clue01 obtenida resolviendo realmente el mural.

También valida correspondencia de símbolos, direcciones espaciales, visibilidad de destinos, acceso por raycast a las tres piezas, prompt Girar, teclado, XInput, DualShock, comandos secretos sin efecto, notificación compartida, duración de presentación y luz de resolución. Las pruebas de input usan dispositivos sintéticos del Input System y restauran sus ajustes al finalizar.

Regresiones: PuzzleFrameworkValidation, MuralValidation, InteractionValidation (incluye Input/Player/Dynamic Prompts), ExplorerFeelValidation y Level01Validation. Esta última comprueba recorrido físico hasta Zone05 y regreso, puerta inicial y desniveles sin teletransportar al jugador.

`GuardiansSceneAuthoring` es una herramienta explícita de instalación, no ejecutada al importar, que rechaza duplicados. La revisión por IDs serializados de la escena permite distinguir los objetos añadidos del orden de serialización de Unity y verificar que no se retiró contenido existente.

## Decisiones de alcance

Solo Puzzle02. Se mantienen las clases concretas de elementos independientes del mural para no introducir un contrato universal sin necesidad ni cambiar su comportamiento validado. Se reutilizan materiales de piedra/relieve, el tracker y toda la UI existentes. No se añadieron consumidores de comandos secretos, Puzzle03, audio final, packages, puntuación o guardado. main permanece sin cambios y esta feature no se integra todavía a develop.

## Resultado del checkpoint

- GuardiansValidation: 293 comprobaciones aprobadas, Unity batchmode código 0.
- PuzzleFrameworkValidation: 71 comprobaciones aprobadas.
- MuralValidation: 267 comprobaciones aprobadas.
- InteractionValidation/Input/Player/Dynamic Prompts: aprobados.
- ExplorerFeelValidation: aprobado.
- Level01Validation: aprobado, incluyendo navegación física hasta Zone05, montículo local de Zone02, puerta y backtracking.
- Todos los procesos de la validación final terminaron con código 0, sin errores C#, Missing Scripts, referencias críticas rotas o assertions fallidas.
- Métricas deterministas: tres giros, 1,05 segundos de animación secuencial, alineación inicial 1/3 y progresión hasta 3/3. Dificultad y ritmo humano todavía no medidos.
- La revisión por IDs serializados no encontró bloques existentes eliminados; los cambios de escena de esta pasada se limitan a orientaciones iniciales, tallas de los marcadores y referencias/hijos para el feedback de Zone02. Los objetos y rutas de las otras zonas se conservan.
- Incidencia histórica del checkpoint inicial: el primer proceso de regresión previo al merge pasó sus comprobaciones, pero quedó bloqueado al cerrar Unity. Se detuvo únicamente ese proceso batchmode, se repitió toda la validación y las repeticiones terminaron correctamente con código 0.
- Warnings conocidos: UAC0009 preexistentes por DEVELOPMENT_BUILD en componentes de debug; avisos Git LF/CRLF. No se modificaron esos componentes ni la configuración Git.

## Pasada de simplificación UX

El reto principal del nivel será relacionar las pistas, por lo que se reduce la manipulación mínima de seis a tres giros sin sustituir la mecánica. Guardian A mira al marcador del círculo desde el inicio y ofrece un ejemplo visual silencioso: observar su símbolo, comparar el destino y reconocer la dirección. B y C requieren todavía acciones diferentes; no es posible completar con un único giro.

`GuardiansAlignmentFeedback` controla seis marcas grabadas: una en cada pedestal fijo y tres en la superficie de la piedra central existente. La emisión tenue refleja cada alineación discreta sin mostrar números, checks, instrucciones ni texto flotante. La presentación se actualiza por `AlignmentChanged`, en respuesta al inicio/final del giro y al reset, sin polling por frame ni material clonado. Se utiliza MaterialPropertyBlock sobre un material URP propio de estas marcas.

Al entrar, únicamente la marca A está activa, tanto en el pedestal como en la piedra central. Durante un giro se apaga la marca de esa pieza hasta confirmar el siguiente índice. El sistema puede representar 0/3, 1/3, 2/3 y 3/3; las tres activas acompañan la luz cálida final ya existente. Reset vuelve al ejemplo inicial con una activa. La base PuzzleBase, el ciclo discreto y la duración de 0,35 segundos se conservan.

Clue02 y el Input System permanecen sin cambios. La prueba final verifica indicadores reales mediante sus bloques de material, incluyendo la inicialización tras Awake/Start, el giro del ejemplo, reset y la solución mínima. La claridad y los tiempos humanos siguen pendientes de playtest.
