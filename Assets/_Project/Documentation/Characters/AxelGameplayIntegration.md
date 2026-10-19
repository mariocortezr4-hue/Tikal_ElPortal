# Integración de Axel como personaje de gameplay

Axel ocupa `character.02`, segundo slot del catálogo existente. Su visual se instancia mediante `CharacterVisualSpawner` dentro del Player compartido. La única ampliación de runtime es una referencia serializada de Axel en `CharacterVisualAssets` y la definición del catálogo; no tiene movimiento, input, cámara, controlador de animación ni clips propios. Mario sigue siendo `character.03`. Los otros slots conservan sus definiciones anteriores.

## Fuente y assets

Fuente externa de solo lectura: `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Materiales_Tikal_ElPortal\Personajes\CHR_Axel\CHR_Axel.zip`.

- Modelo: `Assets/_Project/Art/Characters/Axel/Models/CHR_Axel.fbx`.
- Textura: `Assets/_Project/Art/Characters/Axel/Textures/CHR_Axel_basecolor.PNG`.
- Material: `Assets/_Project/Art/Characters/Axel/Materials/MAT_Axel_Body.mat`.
- Prefab visual: `Assets/_Project/Prefabs/Characters/CHR_Axel_PlayerVisual.prefab`.
- Registro compartido: `Assets/_Project/Resources/Characters/GameplayCharacterVisuals.asset`.

El FBX y la BaseColor son copias idénticas a las entradas del ZIP; el original no se modifica. SHA-256 del ZIP: `3fba7d0d80b622d6aa69f0945001984db6d302500b1e5b00e5ad1c42d000913c`.

La auditoría encontró 67 huesos, 33.464 triángulos y 17.064 vértices de control. Unity importa 37.896 vértices después de separar atributos y utiliza 62 huesos en el skinned renderer. Los pesos tienen 1–4 influencias por vértice, normalizadas y sin vértices sin peso.

## Avatar, escala y orientación

Unity 6.6 (`6000.6.3f1`), Animation Type Humanoid y Avatar Definition Create From This Model. Axel utiliza su propio Avatar válido/Humanoid, con 52 correspondencias reconocidas. GUID del FBX: `5561908669f64e547a581f17ddd2a5c3`.

Model Import Scale Factor: `1.80000043`, con unidades del archivo activas. Altura de geometría en reposo: aproximadamente 1,80 m; el bounds conservador del renderer puede ser mayor y no debe usarse como talla física. Prefab y wrappers visuales con Scale `(1,1,1)`. No se reconstruye el Avatar al ejecutar el authoring de gameplay.

La configuración Humanoid anterior produjo un mismatch al cambiar la escala. El primer rebuild limpio del piloto lo resolvió: escala fijada, Generic/NoAvatar, eliminación de HumanDescription previo y regeneración Humanoid desde el modelo actual. No hubo un segundo intento ni cambio de GUID. El error no justificó modificar el skeleton fuente.

En bind pose la geometría importada parece mirar +X; el retarget Humanoid con los clips compartidos la orienta hacia +Z. Up efectivo +Y, Forward efectivo +Z, sin rotación correctiva. No añadir una rotación de 90 grados basada solo en el bind pose.

## Jerarquía visual y material

```text
CHR_Axel_PlayerVisual
└── AxelVisualOrientation  [local Y = +0.01414936 m, Rotation 0, Scale 1]
    └── AxelModel          [Animator y rig/malla importados]
```

Animator: Avatar propio, `AC_ExplorerGameplay`, Apply Root Motion desactivado. El prefab no contiene CharacterController, PlayerMovement, PlayerLook, PlayerInteraction, Camera ni AudioListener. Las conversiones de unidades internas del skeleton se conservan.

Material `Universal Render Pipeline/Lit`, BaseColor PNG RGBA 2048×2048 opaca, Metallic 0 y Smoothness 0,25. No existe Normal Map y no se genera una artificial.

## Arquitectura compartida

`CharacterCatalog` resuelve `character.02` con Display Name Axel y el prefab del registro visual. `CharacterVisualSpawner.Apply` crea el visual bajo `CharacterVisualRoot` y conecta el Animator al `PlayerAnimationController` compartido; este bridge se añade a runtime si falta. No es necesario serializar un bridge especial de Axel en las escenas.

El controlador existente `Assets/_Project/Animation/Characters/Explorer/AC_ExplorerGameplay.controller` se asigna por referencia y permanece intacto. Reutiliza los seis clips Humanoid existentes de Idle, Walk, Run, Jump, CrouchIdle y CrouchWalk. No hay AC_Axel, override controller, clips copiados ni lógica especial por personaje.

Se conservan Walk 2,2× y Run 1,5896648×, velocidades físicas Walk 4 m/s y Sprint 6,5 m/s, y todos los parámetros/umbrales/estados existentes. CrouchIdle conserva el alias retenido en fase 0,25; Jump conserva el mecanismo de fase de ascenso/caída/aterrizaje existente.

## Selección, escenas y cámara

El Main Menu toma las etiquetas del catálogo al iniciar su selector: la segunda tarjeta muestra Axel. Focus, marca persistente y Confirm reutilizan `CharacterSelectionController` y `MenuSelectableVisual`; el guardado usa `GameProgressData.SelectedCharacterId` sin cambiar el esquema. No se reescribe el menú ni su escena.

`TUT_ExplorerBasics` y `Level01_SecretEntrance` reutilizan sus Players/spawners actuales sin cambios específicos de escena. FPS oculta el visual completo; TPS lo muestra. Cambiar de modo desde Pause Settings conserva la misma instancia y no añade cámaras ni brazos FPS. Cambiar Axel → Mario → Axel debe conectar el Avatar correcto cada vez.

Checkpoint/respawn utiliza los hooks existentes de PlayerMovement y PlayerAnimationController. Debe conservar el offset y limpiar estados residuales de Run, Crouch y Falling. Door, Interact, Journal, puzzles y SecretCommand mantienen sus autoridades y código actuales.

## Contacto con el suelo y ropa: pending human visual review

El offset único aprobado permanece en `+0.01414936 m`; no se agregan correcciones por frame, modificaciones de CharacterController ni ajustes específicos de playback.

Mediciones piloto con la raíz visual sobre plano Y=0, después del offset:

| Estado | Mínimo de suela | Máximo de suela |
|---|---:|---:|
| Idle | −1,014 mm | +0,889 mm |
| Walk | −38,398 mm | +52,851 mm |
| Run | −14,246 mm | +144,972 mm |

Los máximos de Run incluyen vuelo. Estas medidas son relativas al plano del piloto. El Player real agrega su separación física existente con el suelo. En el plano de validación, con la cápsula original y sin modificar el offset, Idle dio una suela a aproximadamente +39,356 mm del plano (raíz física +40 mm). Esta separación también queda pendiente de revisión humana. No se debe interpretar la penetración del piloto como una medición idéntica del gameplay.

Los pliegues de mangas/axilas se aceptan en v1 según el dictamen B del piloto, sin modificar weight painting. Severidad perceptual de penetración, sliding y ropa: **pending human visual review**. Si el playtest humano lo exige, una feature posterior puede evaluar Foot IK/contact polish con alcance y pruebas propios. Esta integración no promete contacto perfecto.

## Authoring y validación

- `Tikal/Authoring/Apply Axel Gameplay Character`: valida el piloto existente, configura únicamente el visual si hay diferencias y registra Axel preservando Mario. Es idempotente. No regenera el rig ni escribe el controlador compartido.
- `Tikal/Validation/Validate Axel Gameplay Character`: valida catálogo, Avatar, importación, material, jerarquía visual, referencias y compatibilidad estructural de escenas.
- `Tikal/Validation/Run Axel Gameplay Play Tests`: ejecuta pruebas en Play Mode del Editor normal; no usar batchmode. Ejecuta Apply dos veces y compara bytes de assets clave para verificar idempotencia.

Los resultados se guardan en `Characters/Axel/AxelEditorValidation.json` y `Characters/Axel/AxelPlayValidation.json`. Capturas y logs temporales quedan en Temp o fuera de Assets; no se versionan. La herramienta de diagnóstico piloto y su .meta se retiraron de Assets y se conservaron fuera del repositorio; los 12 archivos visuales validados se formalizaron en sus rutas originales.

Las pruebas usan el Player, sus cámaras, el selector y las escenas reales con entradas de movimiento procesadas y llamadas de UI/acciones programáticas. La persistencia de selección/settings se escribe en un directorio temporal aislado mediante el mismo GamePersistenceService; no se crea otro formato de guardado. No equivalen a playtest humano ni a un recorrido completo de aceptación de todos los puzzles.

## Playtest humano pendiente

1. Seleccionar Axel en PERSONAJE; salir y volver, verificar marca y nombre guardado.
2. Tutorial TPS: Idle, Walk, Run, Jump, Crouch y Crouch Walk; mirar mangas y axilas.
3. Vista lateral durante Walk/Run: juzgar contacto/sliding sin corregir automáticamente.
4. Pause Settings: TPS → FPS → TPS, verificar ocultación/aparición y cámara/reticle.
5. Level01: Interact, Door, Journal, puzzles y SecretCommand en sus condiciones normales.
6. Restart From Checkpoint: verificar offset y ausencia de Falling/Run/Crouch residuales.
7. Main Menu: cambiar a Mario, entrar a gameplay, volver a Axel y comprobar Avatars separados.

## Resultados de integración

Validación final local en Unity 6.6: **26 comprobaciones de Editor y 123 de Play Mode aprobadas**, con cero errores de runtime y cero errores del Editor capturados en la corrida final. Authoring aplicado dos veces sin diferencias en los bytes de prefab, controlador, registro y .meta del modelo.

La corrida verificó selección/Confirm/focus/marca persistente, guardado aislado character.02, Idle/Walk/Run y sus transiciones, Jump/Air/landing, CrouchIdle/CrouchWalk, magnitudes de movimiento procesadas, FPS/TPS y repetición de selección sin reinstanciar. También comprobó Tutorial y Level01, cambio Axel/Mario en ambas escenas, Journal, Pause Settings, Interact sobre la hoja real de Door, apertura de Door, SecretCommand inelegible sin efecto sobre la puerta, cinco puzzles incluyendo contenido inicialmente oculto, giro real de un fragmento de mural y Restart From Checkpoint desde Pause.

Las pruebas se sincronizan con Time.frameCount para avanzar una sola vez por frame real de juego; sincronizan el collider del plano antes de crear el Player y usan geometría CPU-skinned para medir tamaño. runInBackground y captureFramerate se restauran al finalizar. Las comprobaciones de contacto no sustituyen aceptación humana ni exigen tocar el suelo perfectamente.

Los archivos de Mario, controlador/clips compartidos, Player, cámaras, reticle, mecánicas y escenas no se modificaron. Dictamen visual conservado: **B — READY WITH MINOR FIXES**, con contacto/sliding y mangas/axilas pendientes de revisión humana.
