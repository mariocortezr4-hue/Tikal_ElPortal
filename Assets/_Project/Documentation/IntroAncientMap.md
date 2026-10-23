# Intro Ancient Map — estado final de branding, audio y mapa

## Especificación vigente (2026-10-23)

Esta pasada modifica únicamente Intro_AncientMap, su Timeline y dependencias directas. No
retima sus 85,5 s, diálogos, subtítulos, cámaras o evento AncientMapReceived (59,5 s). La lógica
IntroSequenceController/GameFlowController y los destinos Intro → Tutorial / replay → MainMenu
permanecen intactos. No se modifica Menu, Tutorial, Level01 ni gameplay.

### Branding

El cue final y TitleText dicen exactamente `TIKAL EL EXPLORADOR`; se elimina el título anterior
de la escena y del Timeline activos. La tarjeta reutiliza el logo final
`Assets/_Project/Art/UI/MainMenu/UI_Tikal_StoneGold_Logo.png` y la piedra existente del menú.
El texto TMP conserva el contenido semántico del cue, deshabilitado visualmente para evitar
superponerlo al logo completo. El arte se muestra bajo la misma activación de TitleCard.
No se modifica el asset ni la escena del menú. Encuadre central amplio, marco dorado discreto.

### Música, duck y continuidad

Se conserva el master `MUS_Intro_Suspenso.wav` sin cambios. Se deriva
`Assets/_Project/Audio/Music/Cinematics/MUS_Intro_Suspenso_Continuous.wav`:
PCM 24-bit, 48 kHz estéreo, 85,5 s. Conserva el comienzo original; entre 57,504 y 61,504 s
funde durante 4 s con una reprise del propio archivo desde el segundo 18. Ganancias lineales
complementarias, sin normalización, cambios de velocidad o un reinicio abrupto. No se promete
que el empalme sea musicalmente imperceptible sin aceptación auditiva humana.

Timeline conserva una sola pista Music, un solo clip y el mismo AudioSource 2D. La importación
es Streaming/Vorbis 0,85; no se agregan fuentes, mixers ni lógica de reproducción runtime.
IntroMusicEnvelope conserva su código y multiplicadores de MusicVolume/MasterVolume.

| Fase | Ganancia lineal antes de MusicVolume |
|---|---:|
| Entrada desde 0 a 0,75 s | 0 → 0,20 |
| Narración o diálogo | 0,006 |
| Ambiente entre bloques y tras último diálogo | 0,10 |
| Salida final de 84,9 a 85,5 s | 0,10 → 0 |

La envolvente se deriva de los cues Narrative/Subtitle. Ataque anticipado de 0,65 s y recuperación
de 1,1 s; pausas menores de 1,6 s se agrupan para evitar bombeo. No hay sidechain ni nuevas voces.
Duck frente al ambiente: −24,44 dB. El antiguo suelo de 0,039 pasa a 0,006: **16,26 dB más bajo**.
La recuperación ya no devuelve el nivel inicial de 0,26: solo 0,10 ambiental. La música cubre
el resto de la película con su fade deliberado de salida; se elimina el silencio desde 61,504 s.

La auditoría no encontró clips VO en las pistas Voice/VOICE_NARRATOR. El duck está sincronizado
con las ventanas canónicas de narración/diálogo de Timeline; el balance frente a voces grabadas
requerirá aceptación al incorporarlas. No se inventan voces ni se afirma escucha humana.

### Prop de mapa utilizado

`Assets/Tikal/NonCharacterKit/Prologue/Prefabs/PFB_PRP_MapaAntiguo_01.prefab`

Procede del paquete local `TIKAL_NON_CHARACTER_INTEGRATION_KIT/UnityPayload`, con GUIDs conservados
y sin colisiones. No estaba importado en este proyecto. Las otras coincidencias de librería son
revisiones del mismo PRP_MapaAntiguo_01: se elige el prefab Unity preparado del kit en lugar de
regenerar o importar el Blender/FBX de staging sin su contrato de ejes y materiales.

- Modelo: `Assets/Tikal/NonCharacterKit/Prologue/Models/PRP_MapaAntiguo_01.fbx`.
- Material: `Assets/Tikal/NonCharacterKit/Prologue/Materials/PRP_MapaAntiguo_01_Map_Geometry_0.mat`.
- 3.032 triángulos, 1 material URP/Lit, BaseColor/NormalGL/MetallicSmoothness existentes.
- Dimensiones del prefab: 0,62 × 0,008 × 0,42 m; conserva corrección de ejes y referencias Grip_L/R.
- Instancia `Ancient map final visual` bajo `AncientMap_Proxy`. El carrier conserva Animator,
  ParentConstraint de dos manos y bindings de Timeline; su renderer/collider de bloque se deshabilitan.
- La escala local inversa compensa la escala no unitaria del antiguo cubo sin modificar su
  animación, su posición, su rotación ni la transferencia de manos. Sin colliders nuevos.

### Herramientas y evidencia

Aplicar: `Tikal/Authoring/Apply Intro Cinematic Final Polish`.
Validar: `Tikal/Validation/Run Intro Final Polish Play Tests`.
No ejecutar Build Ancient Map Intro ni la autoría histórica de audio sobre la versión final:
esos comandos representan el blockout/mix previo. La autoría final se ejecutó dos veces con
escena y Timeline idénticos en bytes. Evidencia fuera de Assets: `_IntroFinalPolish_20261023`.
Resultado: 176 comprobaciones de reproducción completa/skips y 3 de captura final de branding aprobadas, sin errores. Guardados personales sin cambios (hashes verificados).
Las pruebas usan guardado temporal y teclado virtual; comprueban película completa, audio PCM
tras el antiguo corte, subtítulos, mapa visible, título y skips de primera partida/replay.

### Próxima pasada artística (pendiente)

1. Iluminación de rostros/manos y separación suave del fondo, conservando tono de suspenso.
2. Sustituir cilindros/bloques de entorno por vegetación y asiento/ruina con profundidad.
3. Refinar contraste y lectura del dibujo del mapa en el plano de entrega.
4. Refinar contacto fino de dedos y gesto Give Map: sigue siendo la actuación provisional
   existente; esta pasada no crea animación ni altera rigs.
5. Revisar composición de fondo y salida, y aceptación auditiva del empalme/duck con VO real.

---

## Registro histórico de la pasada anterior (sustituido por la especificación vigente)

Las cifras, limitaciones de silencio y título legado que siguen describen exclusivamente el
estado anterior; no son requisitos de la implementación final indicada arriba.

# Introducción: Ancient Map

## Estado y alcance

Escena `Assets/_Project/Scenes/Cinematics/Intro_AncientMap.unity`; Timeline `Assets/_Project/Timelines/Intro/TL_Intro_AncientMap.playable`, 30 fps. La Intro presenta Tikal en 1500 d.C., la búsqueda de la Fuente del Conocimiento y un mapa provisional. Conserva los 19 cues, textos, diálogos y actuaciones existentes, con Dwight canónico y Anciano real. Entorno y mapa siguen siendo blockout; no incorpora voces ni lip sync.

El pulido de audio, pacing y cámara se aplica sobre la integración del Anciano (`d0a10f3`). No cambia gameplay, roster, Tutorial, Level01, guardados, rigs, Avatars, materiales ni prefabs de personajes. Las limitaciones de la actuación sentada y del Give Map provisional se documentan en `Characters/ElderCinematicIntegration.md`.

## Authoring y validación

- Aplicar: `Tikal/Authoring/Apply Intro Audio And Pacing Polish`.
- Validar: `Tikal/Validation/Validate Ancient Map Intro`.
- Pruebas interactivas automatizadas: `Tikal/Validation/Run Intro Audio Polish Play Tests`.

La herramienta de pulido es idempotente: retima una sola vez y reutiliza Music/AudioSource. Repetirla conserva bytes de escena, Timeline y metadatos del audio. `Apply Elder Cinematic Character` detecta el pulido y delega para conservarlo. El comando histórico `Build Ancient Map Intro` reconstruye el blockout: **no usar sobre esta escena integrada**. Los validadores históricos reconocen la versión de pulido y usan las comprobaciones actuales.

## Ritmo medido

| Medida | Antes | Después |
|---|---:|---:|
| Inicio del fade de fecha | 1,5 s | 0,5 s |
| Fecha totalmente opaca | 1,9 s | 0,9 s |
| Ventana de lectura de fecha | 3 s | 3 s |
| Primer narrador | 6–13,5 s | 5–12,5 s |
| Segundo narrador | 15–23,5 s | 14–22,5 s |
| Fin del último fade textual | 23,5 s | 22,5 s |
| Imagen de escena completamente revelada | 25 s | 23,5 s |
| Inicio de cámara/track de caminata | 35 s | 23,5 s |
| Gap último texto → caminata/cámara | 11,5 s | 1 s |
| Duración total | 97 s | 85,5 s |

Se eliminan **11,5 s**: 1 s de lead-in y 10,5 s de transición/espera intermedia. La estimación de 92–94 s no correspondía a las pausas serializadas: no se fuerza esa duración. El fade visual hacia la escena ocupa ahora 1 s. La curva de movimiento conserva posiciones y recorrido; el encuadre de ExplorerWalk se conserva, por lo que Dwight puede entrar en cuadro después de comenzar su track.

Se preservan exactamente las ventanas de lectura de fecha (3 s), narrador 1 (7,5 s) y narrador 2 (8,5 s), incluidas sus curvas de opacidad desplazadas. No se aceleran diálogos ni Give Map. Desde el antiguo t=35 s, todos los clips, cues y activaciones se adelantan 11,5 s; las curvas nativas de bloqueo, fades y constraint del mapa se retiman de forma coherente. Los clips Humanoid externos, clipIn, velocidades y solapamientos de actuación no cambian. Los markers/signals existentes se desplazan con la misma correspondencia.

| Tiempo actual | Contenido |
|---|---|
| 0–0,5 | Respiración negra y entrada musical |
| 0,5–3,5 | Fecha `1500 d.C.` |
| 3,5–5 | Pausa textual existente |
| 5–12,5 | Primer narrador |
| 12,5–14 | Pausa textual existente |
| 14–22,5 | Segundo narrador; comienza duck musical a los 14 s |
| 22,5–23,5 | Fade de imagen hacia selva |
| 23,5–33,5 | Caminata |
| 33,5–41,5 | Encuentro y preguntas |
| 41,5–53,5 | Conversación sobre la Fuente y el camino |
| 53,5–68,5 | Entrega y entrada oculta |
| 68,5–75,5 | Agradecimiento y partida |
| 75,5–82,5 | Advertencia final |
| 82,5–85,5 | Partida, fade y título existente |

`Y recuerda...`: 75,9–78 s, conserva 2,1 s. `No confíes en el sistema.`: 78,7–82,5 s, conserva 3,8 s. No hay dependencia runtime de los antiguos 97 s: el evento stopped del director finaliza la secuencia.

## Audio fuente e importación

Master externo de solo lectura: `D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Materiales_Tikal_El_Explorador/AUDIO/Intro_Suspenso.m4a`.

- MP4/M4A, AAC-LC aproximadamente 192 kb/s; 48.000 Hz, estéreo; 1.489.966 bytes.
- Duración PCM decodificada: **61,504 s**. Un demuxer antiguo informa 61,54 s por redondeo/contenedor.
- Metadatos: encoder Clipchamp, handler SoundHandler y comentario promocional Clipchamp. No prueban autoría o licencia.
- SHA256 master: `9c4d5e6402f7bc708712f63db8ab2ac96e72d4009080b4b6e2f8dad0bffde166`.
- Destino: `Assets/_Project/Audio/Music/Cinematics/MUS_Intro_Suspenso.wav`.

M4A no figura entre los formatos importables documentados por Unity: [compatibilidad oficial](https://docs.unity.com/en-us/engine/6000.5/manual/audio/files/compatibility). Se decodificó con el FFmpeg ya instalado de CapCut a WAV PCM de 24 bits, 48 kHz estéreo, sin normalizar, recortar ni remuestrear. No mejora la calidad del AAC original; evita otra compresión con pérdida fuera de Unity. El master no se mueve ni modifica. Solo el WAV necesario entra al proyecto; no se instala software.

AudioImporter: **Streaming, Vorbis quality 0,85, Preserve Sample Rate, Force To Mono desactivado**. Adecuado para música larga sin descomprimirla completa en RAM. El audio dura menos que la Intro. **No hay loop, estiramiento ni repetición**: no existe un empalme musical verificado. Se conserva el final mediante fade, con silencio posterior hasta Tutorial, incluida la advertencia final. Para cubrir toda la película se necesitará una versión musical más larga o un loop aprobado mediante escucha humana; no se presume esa aprobación.

## Routing, niveles y duck

No existe AudioMixer/Music bus utilizable en el proyecto. Se conserva la convención actual: MasterVolume controla `AudioListener.volume`. El componente localizado `IntroMusicEnvelope` multiplica la envolvente por **MusicVolume del mismo GameSettingsData/GamePersistenceService existente**, sin PlayerPrefs, archivos de preferencias ni guardados nuevos. Se suscribe a PersistentSettingsApplier cuando existe; de lo contrario lee la configuración existente al iniciar la Intro. No multiplica MasterVolume dos veces.

Se reutilizan el AudioSource `Systems/Audio/Music` y el AudioTrack **Music**, que contiene un único `MUS_Intro_Suspenso`. El director controla playback. Source 2D, sin Play On Awake, sin loop. Siguen existiendo los cuatro AudioSources originales (Ambience, Music, Voice, SFX); no se agregan fuentes. El audio no tiene ruta externa en runtime.

| Tiempo | Envolvente lineal antes de MusicVolume |
|---|---:|
| 0 | 0 |
| 0,75 | 0,26 |
| 14 | 0,26; empieza duck con la última frase del prólogo |
| 16 | 0,039 |
| 16–59,004 | 0,039 constante |
| 59,004–61,504 | Fade suave de 2,5 s hasta cero |
| 61,504–85,5 | 0 |

Ducking relativo: **0,15×, −16,48 dB**, en 2 s con interpolación suave. No hay recuperación entre líneas. Con MusicVolume predeterminado 0,8 y MasterVolume 1, el Source pasa de 0,208 a 0,0312. El nivel final también queda afectado por MasterVolume en el Listener. El pico del PCM fuente llega a 0 dBFS y su RMS aproximado es −13,81 dBFS; se adopta un nivel atmosférico moderado, pendiente de aceptación auditiva humana. No se afirma que la mezcla haya sido escuchada o aprobada por una persona.

La salida musical ocurre antes del fin de película porque el archivo solo dura 61,504 s. Al llegar al Tutorial ya está en silencio; al omitir se destruye la fuente con la escena y no queda música huérfana. Se conserva la lógica existente de skip.

Ambience y los slots Voice/VOICE_NARRATOR/SFX se conservan. `AUD_AMB_Jungle_Main_01` no existe: no se reutilizan los tonos del puzzle Echo como ambiente. Hooks de VO pendientes: `VO_NARRATOR_INTRO_001`, `VO_NARRATOR_INTRO_002` y todos los diálogos; sin audio de voces, side-chain ni lip sync en esta feature. Ajustar balance final con VO requerirá una revisión posterior.

## Cámara de entrega

Solo cambia el transform/FOV de **CAM_MapHandoff**; las otras siete cámaras conservan sus transformaciones y óptica. Sus activaciones se adelantan junto a la acción.

| Parámetro | Anterior | Actual |
|---|---|---|
| Posición mundial | (−0,2; 1,7; 10,6) | (−2,3; 2,15; 12,4) |
| FOV vertical | 31° | 40° |
| Objetivo | Manos, ligeramente por debajo | (1,7; 1,25; 11,3) |
| Composición | Detalle bajo y próximo | Plano lateral medio/abierto de la acción |

La altura sube 0,45 m y la cámara se aleja lateralmente del intercambio. Se busca separar las siluetas, incluir torso/parte superior de Dwight y torso/brazo del Anciano, con el mapa subordinado al gesto. No se modifica Give Map: conserva el segmento existente de Talking; una animación específica y un mapa final siguen pendientes. Revisar las capturas en Play Mode y confirmar artísticamente mediante playtest humano.

## Actores, mapa y subtítulos

ExplorerAnchor conserva Dwight bajo CharacterVisualRoot y ExplorerPlaceholder inactivo; ElderAnchor conserva CHR_Elder_Cinematic y ElderPlaceholder inactivo. No usa la selección del CharacterCatalog. Los dos Avatars, materiales, prefabs y ajustes de Root Motion se conservan.

AncientMap_Proxy conserva su ParentConstraint de dos manos y su identidad. La transferencia entre fuentes ocurre ahora de 58,85 a 59,5 s (antes 70,35–71). **AncientMapReceived** sigue siendo único, a 59,5 s (antes 71), y la sujeción permanece con Dwight durante la partida. Los textos de todos los cues se conservan byte a byte semánticamente; únicamente cambian sus posiciones temporales.

CinematicSubtitlePresenter sigue separando fecha, narrativa, hablante, diálogo y título; las curvas de opacidad conservan lectura y los cues se desplazan junto a la acción. El título legado no se reescribe en esta feature.

## Flujo, skip y comprobaciones

IntroSequenceController inicia el director y finaliza al recibir stopped. No espera 97 s fijos. La omisión mantiene UI.Cancel durante 1,25 s y comparte la ruta idempotente de finalización:

- Primera partida: IntroSeen → TUT_ExplorerBasics.
- Replay: MainMenu, sin alterar selección ni progreso narrativo.

Las pruebas de Editor comprueban audio, routing, duck, fades, lectura, gap, duración, cámara, actores, mapa, cues, bindings y Missing Scripts. Las pruebas de Play Mode reproducen la película completa en Editor normal, observan audio PCM real, multiplicadores Master/Music, subtítulos, poses, mapa/evento y carga natural de Tutorial; después ejercitan UI.Cancel con un teclado virtual en primera partida y replay. Usan persistencia temporal aislada y restauran la configuración del Editor. Resultados y capturas quedan en `Temp/IntroAudioPolish`, fuera de assets entregables. Los hashes del resto de archivos se contrastan externamente antes del commit.

Playtest humano pendiente A–I: inicio rápido con fade, lectura cómoda, balance musical, duck perceptible, transición sin espera, composición lateral de entrega, peso de advertencia, salida/Tutorial y omisión. La automatización técnica y la revisión de capturas no sustituyen aceptación auditiva/artística humana.

`IntroPlaceholder` permanece como fallback técnico deshabilitado en Build Settings; SceneCatalog continúa usando Intro_AncientMap. No se añaden escenas ni cámaras.

## Resultado técnico de esta pasada

Unity 6000.6.3f1, Editor interactivo normal: 40 comprobaciones estáticas y 158 comprobaciones de Play Mode aprobadas, sin errores. Authoring repetido con escena/Timeline/metadatos idénticos. La fecha se observó visible a 0,533 s a 30 fps; el primer desplazamiento del ExplorerAnchor mayor de 1 cm se observó a 23,70 s (1,20 s tras el fin del último texto), con cambio de cámara/track a 23,5 s. Estas medidas separan inicio serializado y movimiento observado; no afirman que el actor ya esté dentro del encuadre en ese instante.

Se observó salida real del AudioSource durante el prólogo, MusicVolume=0 silenciándolo y MasterVolume=0,4 aplicado al Listener. La película terminó naturalmente en Tutorial con IntroSeen y un solo AncientMapReceived. Los dos skips mantuvieron UI.Cancel durante su umbral y llegaron a Tutorial/MainMenu con progreso aislado correcto. Las capturas de entrega confirman separación de ambos personajes y mapa subordinado al plano general; balance auditivo y aceptación artística humana siguen pendientes.
