# Hoja de grabación de voces — Intro_AncientMap

## Fuente y alcance

Fuente primaria: `Assets/_Project/Timelines/Intro/TL_Intro_AncientMap.playable`, pista `Cinematic Cues`, cues de tipo `Subtitle` (2). Auditoría del estado aprobado `55873c8` en `feature/intro-cinematic-final-polish`. Duración de la Intro: **87.60 s**.

SHA-256 de la Timeline auditada: `d38f6f3ac86990ec03cf6e00f4f2fa957940ef66a9c1c687351d1dd216c52cf4`.

Los textos y speakers se transcriben desde los subassets referenciados por los clips reales. Inicio = `m_Start`; fin = inicio + `m_Duration`; tiempos relativos al inicio de la Timeline, redondeados a dos decimales. Se conserva el orden de reproducción. `EXPLORADOR` corresponde a Dwight y `ANCIANO` a Elder; la tabla conserva los speakers actuales.

Se incluyen únicamente las intervenciones de estos personajes. Los dos cues `Narrative` sin speaker son rótulos narrativos, no líneas de voz de Dwight o Elder, y se excluyen. No se reconstruyó diálogo desde documentación anterior.

## A. Configuración y márgenes de grabación

- Un archivo **WAV por línea**, **48 kHz**, **mono**, **24-bit preferido / 16-bit aceptable**.
- Entregar voz limpia, sin música ni efectos y sin normalización agresiva. Evitar clipping.
- Reservar **0.25 s de aire inicial** y **0.35 s de aire final** en cada clip: **0.60 s en total** dentro de su ventana actual.
- La duración recomendada máxima corresponde solamente a la voz hablada: ventana menos 0.60 s. El WAV completo, incluidos los aires, debe caber en la ventana.
- No rellenar artificialmente hasta el máximo. Mantener una lectura natural; los máximos son límites de planificación, no duraciones obligatorias.
- Los rangos solicitados de aire (0.20–0.30 s inicial y 0.30–0.40 s final) permiten entre ventana menos 0.70 s y ventana menos 0.50 s de voz. La tabla fija un criterio uniforme intermedio.
- No cambiar texto, puntuación, Timeline ni subtítulos para acomodar una toma. Si no cabe naturalmente, señalarlo para una decisión posterior.

## B. Líneas en orden cronológico

Todos los tiempos están expresados en segundos. Los nombres de archivo son propuestas para futuras grabaciones; no se han creado ni importado WAV.

| # | Speaker | Texto | Inicio | Fin | Ventana | Voz recomendada máx. | Archivo |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| 1 | EXPLORADOR | ¿Qué hace aquí? | 33.90 | 36.20 | 2.30 | 1.70 | `VO_Intro_Dwight_01.wav` |
| 2 | ANCIANO | Podría preguntarte lo mismo. | 36.70 | 38.80 | 2.10 | 1.50 | `VO_Intro_Elder_01.wav` |
| 3 | ANCIANO | ¿A dónde te diriges? | 39.10 | 41.30 | 2.20 | 1.60 | `VO_Intro_Elder_02.wav` |
| 4 | EXPLORADOR | Busco la Fuente del Conocimiento. | 41.90 | 45.00 | 3.10 | 2.50 | `VO_Intro_Dwight_02.wav` |
| 5 | ANCIANO | He oído historias sobre ella. | 45.90 | 48.30 | 2.40 | 1.80 | `VO_Intro_Elder_03.wav` |
| 6 | ANCIANO | Pocos valientes se atreven a buscarla. | 48.60 | 51.10 | 2.50 | 1.90 | `VO_Intro_Elder_04.wav` |
| 7 | ANCIANO | Y pocos conocen el camino. | 51.50 | 53.30 | 1.80 | 1.20 | `VO_Intro_Elder_05.wav` |
| 8 | ANCIANO | Toma. | 54.50 | 55.90 | 1.40 | 0.80 | `VO_Intro_Elder_06.wav` |
| 9 | ANCIANO | Te ayudará a no perderte. | 57.50 | 59.80 | 2.30 | 1.70 | `VO_Intro_Elder_07.wav` |
| 10 | ANCIANO | Pero el camino que buscas no está a simple vista. | 60.50 | 63.90 | 3.40 | 2.80 | `VO_Intro_Elder_08.wav` |
| 11 | ANCIANO | Debes encontrar una entrada oculta. | 64.50 | 67.70 | 3.20 | 2.60 | `VO_Intro_Elder_09.wav` |
| 12 | EXPLORADOR | Gracias. | 69.30 | 70.80 | 1.50 | 0.90 | `VO_Intro_Dwight_03.wav` |
| 13 | ANCIANO | Y recuerda... | 75.90 | 78.00 | 2.10 | 1.50 | `VO_Intro_Elder_10.wav` |
| 14 | ANCIANO | No confíes en el sistema. | 78.70 | 82.50 | 3.80 | 3.20 | `VO_Intro_Elder_11.wav` |

**Total: 14 líneas — 3 Dwight y 11 Elder.** Suma de ventanas: **34.10 s**; aire reservado: **8.40 s**; suma de máximos recomendados de voz hablada: **25.70 s**. Esta última cifra es un techo de planificación aproximado, no una medición de voces grabadas. La duración real de voz queda pendiente de grabación. Los huecos entre líneas no se cuentan como voz.

## C. Notas breves de interpretación

- Líneas 1 y 3: pregunta.
- Líneas 2 y 4: respuesta.
- Línea 8: entrega del mapa.
- Línea 12: agradecimiento.
- Líneas 13–14: advertencia final; respetar los puntos suspensivos de la línea 13.

## D. TIMING TIGHT

Alertas estimadas de planificación, no pruebas de interpretación. Para hacer explícito el criterio se usa una lectura de referencia de **2.5 palabras/s**; es una hipótesis de trabajo conservadora, no una norma ni una garantía. Se marca `TIMING TIGHT` cuando esa lectura excede el máximo de voz tras reservar 0.60 s de aire. La longitud de sílabas, pausas y actuación puede exigir más tiempo incluso en líneas no marcadas.

El margen disponible de voz de cada línea es real respecto a la ventana auditada; la lectura de referencia y su déficit son estimaciones. No se ha modificado ninguna ventana.

| Línea | Estado | Ventana real | Aire reservado | Voz disponible | Voz posible con aires de 0.50–0.70 s | Lectura de referencia | Déficit estimado |
| --- | --- | ---: | ---: | ---: | --- | ---: | ---: |
| 2 | TIMING TIGHT | 2.10 s | 0.60 s | 1.50 s | 1.40–1.60 s | 1.60 s | 0.10 s |
| 5 | TIMING TIGHT | 2.40 s | 0.60 s | 1.80 s | 1.70–1.90 s | 2.00 s | 0.20 s |
| 6 | TIMING TIGHT | 2.50 s | 0.60 s | 1.90 s | 1.80–2.00 s | 2.40 s | 0.50 s |
| 7 | TIMING TIGHT | 1.80 s | 0.60 s | 1.20 s | 1.10–1.30 s | 2.00 s | 0.80 s |
| 9 | TIMING TIGHT | 2.30 s | 0.60 s | 1.70 s | 1.60–1.80 s | 2.00 s | 0.30 s |
| 10 | TIMING TIGHT | 3.40 s | 0.60 s | 2.80 s | 2.70–2.90 s | 4.00 s | 1.20 s |

Prioridad de revisión en grabación: líneas 7, 10 y 6, por mayor presión temporal. No acelerar de forma artificial ni corregir frases automáticamente. Si una lectura natural no cabe, conservar la toma de referencia y comunicar su duración antes de autorizar un ajuste posterior.

## Comprobación documental

Inventario obtenido de 14 clips de subtítulo referenciados por la pista actual, sin solapamientos entre estas líneas, con 14 nombres WAV distintos. Textos, speakers e intervalos cotejados con la Timeline serializada. Esta tarea no incluye reproducción, pruebas de audio ni validación en Unity; no se han integrado voces ni alterado la Intro.

## FINAL VO INTEGRATION

Integración de 14 WAV finales (3 Dwight / 11 Elder; Anciano interpretado por Keynes). Esta sección añade resultados reales; la hoja original anterior queda conservada como referencia de planificación.

### Fuente e importación

Fuente externa de solo lectura: `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Materiales_Tikal_El_Explorador\AUDIO\VO\Intro`. Nombres corregidos por el usuario antes de importar: Elder_01 a Elder_11, sin Elder_12. Correspondencia por numeración de la hoja; no se renombraron ni editaron los fuentes.

Destino: `Assets/_Project/Audio/VO/Intro/`. WAV copiados byte a byte y cotejados mediante SHA-256. Todos son 48 kHz, mono y PCM de 24 bits. Unity: Decompress On Load, PCM, Preserve Sample Rate, Preload activado, Force To Mono desactivado; no hay conversión mono innecesaria ni compresión con pérdidas para VO.

### Auditoría previa a retiming

Diferencia = duración completa del WAV menos ventana original; positiva indica exceso. FITS: caben al menos 0.20 s extra; TIGHT: cabe con menos de 0.20 s; OVERFLOW: excede. Se compara el WAV completo, no una estimación del habla. Resultado: 13 OVERFLOW, 1 TIGHT, 0 FITS.

Pico y RMS expresados en dBFS, medidos sobre PCM completo, antes de gain. Silencios aproximados mediante bloques RMS de 10 ms y umbral -45 dBFS: ruido de fondo puede reducir esta estimación; no representan límites de palabras ni justifican recortes. No se midió LUFS.

| Clip | Personaje | WAV (s) | Ventana original (s) | Diferencia (s) | Estado | Pico (dBFS) | RMS (dBFS) | Silencio inicial (s) | Silencio final (s) |
| --- | --- | ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: |
| `VO_Intro_Dwight_01.wav` | EXPLORADOR | 4.713 | 2.30 | +2.414 | OVERFLOW | -0.00 | -21.90 | 0.040 | 0.143 |
| `VO_Intro_Elder_01.wav` | ANCIANO | 3.263 | 2.10 | +1.163 | OVERFLOW | -5.00 | -26.31 | 0.000 | 0.013 |
| `VO_Intro_Elder_02.wav` | ANCIANO | 3.263 | 2.20 | +1.063 | OVERFLOW | -5.88 | -26.67 | 0.000 | 0.023 |
| `VO_Intro_Dwight_02.wav` | EXPLORADOR | 4.673 | 3.10 | +1.573 | OVERFLOW | 0.00 | -21.46 | 0.030 | 0.983 |
| `VO_Intro_Elder_03.wav` | ANCIANO | 4.969 | 2.40 | +2.569 | OVERFLOW | -5.69 | -24.51 | 0.000 | 0.019 |
| `VO_Intro_Elder_04.wav` | ANCIANO | 5.481 | 2.50 | +2.981 | OVERFLOW | -6.37 | -24.82 | 0.000 | 0.021 |
| `VO_Intro_Elder_05.wav` | ANCIANO | 4.543 | 1.80 | +2.743 | OVERFLOW | -7.30 | -27.22 | 0.000 | 0.013 |
| `VO_Intro_Elder_06.wav` | ANCIANO | 2.324 | 1.40 | +0.924 | OVERFLOW | -7.06 | -25.32 | 0.000 | 0.024 |
| `VO_Intro_Elder_07.wav` | ANCIANO | 3.775 | 2.30 | +1.475 | OVERFLOW | -5.57 | -23.44 | 0.010 | 0.015 |
| `VO_Intro_Elder_08.wav` | ANCIANO | 5.055 | 3.40 | +1.655 | OVERFLOW | -6.65 | -23.18 | 0.000 | 0.015 |
| `VO_Intro_Elder_09.wav` | ANCIANO | 4.969 | 3.20 | +1.769 | OVERFLOW | -5.17 | -24.49 | 0.000 | 0.019 |
| `VO_Intro_Dwight_03.wav` | EXPLORADOR | 3.074 | 1.50 | +1.573 | OVERFLOW | -0.12 | -22.87 | 0.030 | 0.534 |
| `VO_Intro_Elder_10.wav` | ANCIANO | 4.969 | 2.10 | +2.869 | OVERFLOW | -8.35 | -28.93 | 0.000 | 0.019 |
| `VO_Intro_Elder_11.wav` | ANCIANO | 3.604 | 3.80 | -0.196 | TIGHT | -7.03 | -24.74 | 0.000 | 0.014 |

Suma de duración WAV real: **58.675 s**.

### Timing final y gains no destructivos

Tracks: `VO_Dwight` reutiliza el track Voice y su AudioSource original; `VO_Elder` añade un track y una fuente dedicada. Total: cinco AudioSources en Intro, frente a cuatro originales; no se añade AudioListener. `IntroVoiceSettings` respeta el volumen SFX existente y detiene la fuente al detener/desactivar la secuencia; master sigue la convención actual del AudioListener.

No se recortan WAV, palabras ni respiraciones. Todos los clips usan clipIn 0, speed 1, loop desactivado y duración samples/frequency. VO empieza 0.08 s después del subtítulo; el subtítulo termina al menos 0.20 s después del WAV. Las pausas de la grabación permanecen intactas.

| # | Archivo | Subtítulo inicio (s) | Subtítulo fin (s) | VO inicio (s) | VO fin (s) | Gain lineal | Gain (dB) |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | `VO_Intro_Dwight_01.wav` | 33.900 | 38.894 | 33.980 | 38.694 | 0.44133 | -7.10 |
| 2 | `VO_Intro_Elder_01.wav` | 39.394 | 42.936 | 39.474 | 42.736 | 0.73407 | -2.69 |
| 3 | `VO_Intro_Elder_02.wav` | 43.236 | 46.779 | 43.316 | 46.579 | 0.76515 | -2.33 |
| 4 | `VO_Intro_Dwight_02.wav` | 47.379 | 52.332 | 47.459 | 52.132 | 0.41976 | -7.54 |
| 5 | `VO_Intro_Elder_03.wav` | 53.232 | 58.482 | 53.312 | 58.282 | 0.59649 | -4.49 |
| 6 | `VO_Intro_Elder_04.wav` | 58.782 | 64.543 | 58.862 | 64.343 | 0.61813 | -4.18 |
| 7 | `VO_Intro_Elder_05.wav` | 64.943 | 69.766 | 65.023 | 69.566 | 0.81465 | -1.78 |
| 8 | `VO_Intro_Elder_06.wav` | 70.966 | 73.570 | 71.046 | 73.370 | 0.65427 | -3.68 |
| 9 | `VO_Intro_Elder_07.wav` | 75.170 | 79.224 | 75.250 | 79.024 | 0.52726 | -5.56 |
| 10 | `VO_Intro_Elder_08.wav` | 79.924 | 85.259 | 80.004 | 85.059 | 0.51147 | -5.82 |
| 11 | `VO_Intro_Elder_09.wav` | 85.859 | 91.108 | 85.939 | 90.908 | 0.59518 | -4.51 |
| 12 | `VO_Intro_Dwight_03.wav` | 92.708 | 96.062 | 92.788 | 95.862 | 0.49373 | -6.13 |
| 13 | `VO_Intro_Elder_10.wav` | 101.162 | 106.411 | 101.242 | 106.211 | 0.99148 | -0.07 |
| 14 | `VO_Intro_Elder_11.wav` | 107.111 | 110.995 | 107.191 | 110.795 | 0.61211 | -4.26 |

Gains de clip calculados para aproximar -29 dBFS RMS de archivo completo entre líneas, mediante atenuación únicamente. No es un objetivo LUFS ni una aprobación subjetiva de timbre/inteligibilidad. No se aplicaron EQ, compresor, reverb, normalización destructiva ni modificación de los WAV. Los picos originales de Dwight próximos a 0 dBFS quedan atenuados; el gain no repara una eventual distorsión ya grabada.

### Cambios mínimos de Timeline

Duración anterior **87.600 s**; duración final **116.095166 s**; incremento **28.495166 s**. El incremento refleja las grabaciones reales, no una compresión de escenas anteriores.

En cada ventana se añadió solo max(0, WAV + 0.28 s - ventana original), conservando los huecos existentes. Los clips, markers y keys internos posteriores se desplazaron de forma coherente. Los clips de animación externos que atraviesan un segmento extendido conservan su fragmento original ajustando su velocidad visual; no se editó ningún FBX ni se crearon animaciones. Las propiedades de cámaras, personajes, iluminación, entorno y mapa permanecen intactas.

Los 19 cues originales mantienen texto, speaker, tipo e identidad exactos. Branding conserva su tramo de 3.30 s, incluidos 2.70 s anteriores al fade final de 0.60 s; solo se desplaza junto al cierre. El flujo y código canónico de Intro -> Tutorial y Skip no se modifican.

### Música y mezcla

La música aprobada `MUS_Intro_Suspenso_Continuous.wav` se conserva intacta. Se deriva `Assets/_Project/Audio/Music/Cinematics/MUS_Intro_Suspenso_FinalVO.wav`, stereo 48 kHz / PCM 24-bit: original hasta 85.10 s, empalme complementario suave de 2.50 s con un tramo de la misma pista desde 28.00 s y continuación hasta el nuevo final. No introduce música externa, corte seco ni reinicio a tiempo cero. En Unity se conserva Streaming / Vorbis calidad 0.85 y sample rate original para esta pista musical.

Duck durante VO: **0.018**, frente a 0.006 previo. Dado el gain de voz aplicado, la medición PCM del archivo sitúa la música aproximadamente 18–25 dB debajo del RMS de VO con ajustes por defecto (Music 0.8 / SFX 1 / Master 1), sin confundirlo con LUFS. Se conservan las rampas existentes de 0.65 s antes del bloque y recuperación ambiental de aproximadamente 1.10 s después. Ambient = 0.10; fade musical final de 0.60 s acompaña el fade visual. La narración inicial sin VO mantiene su duck original de 0.006.

### Handoff y advertencia final

`Toma.` y `Te ayudará a no perderte.` permanecen dentro del segmento/cámara de entrega. El evento AncientMapReceived y el constraint existente se desplazan con los mismos tiempos narrativos; no se rehace la animación ni se cambia el mapa.

Advertencia final: `Y recuerda...` termina como WAV en 106.211 s; `No confíes en el sistema.` empieza en 107.191 s. Pausa entre archivos **0.980 s**, además de los aires internos originales.

### Validación

Validación completada en Unity 6000.6.3f1, Play Mode interactivo a tiempo real y velocidad 1x, con progreso de prueba aislado; sin escribir partidas del usuario.

- Reproducción completa: 14 voces con PCM no nulo y subtítulos exactos observados; ningún WAV truncado, acelerado o solapado accidentalmente.
- Dos tracks con fuentes distintas; cinco AudioSources totales y un único AudioListener activo en la comprobación inicial; ningún listener añadido.
- Cero Missing AudioClips, Missing Scripts o Missing Materials; bindings de todos los tracks poblados válidos.
- Música con señal durante voces y después del antiguo límite de 87.6 s; duck final 0.018 observado durante las líneas; recuperación ambiental y fade comprobados.
- Mapa visible en CAM_MapHandoff durante Elder_07; AncientMapReceived exactamente una vez; constraint mantiene el mapa con Dwight.
- Advertencia final, branding legible antes del fade y fade final observados en reproducción.
- Final natural carga TUT_ExplorerBasics y marca IntroSeen en la partida aislada.
- Skip mediante UI.Cancel mantenido: primera partida llega a Tutorial; replay llega a MainMenu; fuentes/componentes de VO de Intro retirados en ambos casos.
- Comparación de datos con HEAD previo: los 19 textos, speakers y tipos de cue se conservan; propiedades originales de environment, cámaras, iluminación, personajes, mapa y materiales intactas.
- Fuentes WAV externos y copias importadas conservan sus hashes SHA-256 de auditoría. La música original también se conserva.

Unity emitió al cargar un aviso de 8 AudioListeners presentes antes de que Timeline activase su cámara; esos ocho listeners ya existían en la escena original y no se añadió ninguno. La comprobación tras inicializar Timeline encontró uno activo. No se modificó la arquitectura de cámaras para ocultar ese aviso previo.

Límite de validación: la medición PCM confirma presencia, niveles y sincronización técnica; no sustituye aprobación humana de inteligibilidad, actuación, ruido de grabación ni sutileza perceptual del empalme. No se declara una escucha humana aprobada ni medición LUFS. La aprobación auditiva final queda para el usuario.

Herramientas de esta pasada: IntroFinalVoiceIntegration (authoring de una sola ejecución desde auditoría temporal; protección frente a retiming duplicado) e IntroFinalVoiceValidation (reproducción local con la metadata de esta pasada). Evidencia de ejecución actual en Temp/IntroFinalVO/PlayValidation.json; Temp puede limpiarse al reiniciar Unity.

## REEMPLAZO POR VOCES LIMPIAS — revisión posterior a 2de110d

Esta revisión sustituye los 14 WAV de la integración anterior por las versiones limpiadas entregadas por el usuario. Los datos de FINAL VO INTEGRATION anteriores permanecen como historial; las especificaciones y gains vigentes de VO son los de esta revisión.

Fuente de solo lectura: `D:\ARCHIVOS\TREVIX\Proyectos\Videojuegos\Materiales_Tikal_El_Explorador\AUDIO\VO\Intro\Sin Ruido`. Destino conservado: `Assets/_Project/Audio/VO/Intro/`. Se reemplaza exclusivamente el contenido WAV con los mismos nombres; los 14 .meta, GUIDs, referencias, fuentes, tracks y bindings se conservan.

Formato de los 14 nuevos WAV: **48 kHz, mono, PCM 16-bit**. El formato anterior era 24-bit; los archivos limpios ya fueron entregados a 16-bit. No se reconvierten artificialmente a 24-bit, no se aplica otro denoise, normalización, EQ, compresión ni procesamiento destructivo. Unity mantiene PCM / Decompress On Load / Preload / Preserve Sample Rate y Force To Mono desactivado.

Cada WAV nuevo contiene exactamente **dos muestras adicionales** respecto al sustituido: **0.000041667 s** a 48 kHz. Se amplía solo su duración de AudioPlayable en esas dos muestras para reproducirlo íntegro. Se conservan todos los inicios VO, tiempos de subtítulos, cámaras, animaciones, handoff, branding, fade, música y ducking. Duración global sin cambios: **116.095166 s**.

### Auditoría de archivos limpios

Pico y RMS en dBFS de archivo completo antes del gain; silencio aproximado con bloques RMS de 10 ms y umbral -45 dBFS. Estos umbrales no determinan límites de palabras ni acreditan por sí solos calidad perceptual del denoise. No se midió LUFS.

| Archivo | WAV (s) | Pico (dBFS) | RMS (dBFS) | Silencio inicial (s) | Silencio final (s) | Ventana actual (s) | WAV − ventana (s) | Estado |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| `VO_Intro_Dwight_01.wav` | 4.713542 | -8.69 | -29.88 | 2.320 | 1.584 | 4.993500 | -0.279958 | FITS |
| `VO_Intro_Dwight_02.wav` | 4.673542 | -7.60 | -29.35 | 2.130 | 1.014 | 4.953500 | -0.279958 | FITS |
| `VO_Intro_Dwight_03.wav` | 3.073542 | -8.29 | -31.39 | 1.560 | 1.044 | 3.353500 | -0.279958 | FITS |
| `VO_Intro_Elder_01.wav` | 3.262708 | -9.34 | -27.72 | 1.070 | 0.503 | 3.542667 | -0.279958 | FITS |
| `VO_Intro_Elder_02.wav` | 3.262708 | -9.26 | -28.68 | 1.080 | 0.903 | 3.542667 | -0.279958 | FITS |
| `VO_Intro_Elder_03.wav` | 4.969375 | -9.89 | -28.52 | 1.680 | 0.909 | 5.249333 | -0.279958 | FITS |
| `VO_Intro_Elder_04.wav` | 5.481375 | -11.40 | -28.90 | 1.780 | 1.171 | 5.761333 | -0.279958 | FITS |
| `VO_Intro_Elder_05.wav` | 4.542708 | -12.68 | -29.74 | 1.660 | 1.433 | 4.822667 | -0.279958 | FITS |
| `VO_Intro_Elder_06.wav` | 2.324042 | -11.16 | -28.57 | 1.020 | 0.684 | 2.604000 | -0.279958 | FITS |
| `VO_Intro_Elder_07.wav` | 3.774708 | -12.25 | -28.03 | 0.840 | 0.995 | 4.054667 | -0.279958 | FITS |
| `VO_Intro_Elder_08.wav` | 5.054708 | -8.76 | -26.89 | 0.740 | 0.455 | 5.334667 | -0.279958 | FITS |
| `VO_Intro_Elder_09.wav` | 4.969375 | -12.06 | -27.84 | 1.480 | 0.949 | 5.249333 | -0.279958 | FITS |
| `VO_Intro_Elder_10.wav` | 4.969375 | -13.56 | -30.55 | 2.040 | 1.509 | 5.249333 | -0.279958 | FITS |
| `VO_Intro_Elder_11.wav` | 3.604042 | -9.09 | -26.31 | 0.760 | 0.564 | 3.884000 | -0.279958 | FITS |

Total: **14 WAV (3 Dwight y 11 Elder/Keynes), 58.675750 s** de archivos. Contra las ventanas actuales ya extendidas: **14 FITS, 0 TIGHT, 0 OVERFLOW**. Las clasificaciones OVERFLOW de la primera integración se referían a las ventanas históricas de la Intro de 87.6 s.

### Gains vigentes y sincronización

Se recalculan las atenuaciones de clip a partir del PCM limpio, en vez de reutilizar la reducción de nivel de la grabación ruidosa. Criterio: aproximar -29 dBFS RMS de archivo completo mediante atenuación únicamente; si un archivo ya queda por debajo, gain 1 (0 dB), sin amplificación artificial. La voz hablada y las pausas internas siguen completas.

| Archivo | Gain anterior (dB) | Gain nuevo (dB) | Gain lineal nuevo | VO inicio (s) | VO fin (s) |
| --- | ---: | ---: | ---: | ---: | ---: |
| `VO_Intro_Dwight_01.wav` | -7.10 | 0.00 | 1.000000 | 33.980000 | 38.693542 |
| `VO_Intro_Dwight_02.wav` | -7.54 | 0.00 | 1.000000 | 47.458833 | 52.132375 |
| `VO_Intro_Dwight_03.wav` | -6.13 | 0.00 | 1.000000 | 92.788333 | 95.861875 |
| `VO_Intro_Elder_01.wav` | -2.69 | -1.28 | 0.863348 | 39.473500 | 42.736208 |
| `VO_Intro_Elder_02.wav` | -2.33 | -0.32 | 0.963590 | 43.316167 | 46.578875 |
| `VO_Intro_Elder_03.wav` | -4.49 | -0.48 | 0.946081 | 53.312333 | 58.281708 |
| `VO_Intro_Elder_04.wav` | -4.18 | -0.10 | 0.989034 | 58.861667 | 64.343042 |
| `VO_Intro_Elder_05.wav` | -1.78 | 0.00 | 1.000000 | 65.023000 | 69.565708 |
| `VO_Intro_Elder_06.wav` | -3.68 | -0.43 | 0.951636 | 71.045667 | 73.369708 |
| `VO_Intro_Elder_07.wav` | -5.56 | -0.97 | 0.894367 | 75.249667 | 79.024375 |
| `VO_Intro_Elder_08.wav` | -5.82 | -2.11 | 0.784710 | 80.004333 | 85.059042 |
| `VO_Intro_Elder_09.wav` | -4.51 | -1.16 | 0.875456 | 85.939000 | 90.908375 |
| `VO_Intro_Elder_10.wav` | -0.07 | 0.00 | 1.000000 | 101.241833 | 106.211208 |
| `VO_Intro_Elder_11.wav` | -4.26 | -2.69 | 0.733628 | 107.191166 | 110.795208 |

El subtítulo sigue empezando 0.08 s antes del WAV y finaliza aproximadamente 0.199958 s después. Las pausas/silencios internos del archivo se conservan; no se hace trimming ni se adelantan frases al primer bloque detectado. Se mantiene la interpretación y el montaje existente.

Música y mezcla musical sin cambios: duck VO **0.018**, recuperación ambiental **0.10**, rampas existentes, empalme de la pista extendida y fade final **0.60 s**. No se vuelve a generar ni modificar la música. No se modifica código runtime, escena, texto ni flujo canónico de Intro/Tutorial/Skip.

### Validación de la sustitución

Validación técnica repetida en Play Mode interactivo, a tiempo real 1x y con partida aislada: reproducción completa; señal de los 14 WAV limpios y música; subtítulos exactos; ningún WAV cortado ni solapado; bindings válidos y sin nuevos Missing AudioClips/Scripts/Materials. Handoff visible y evento AncientMapReceived una sola vez. Branding, hold, fade y recuperación musical comprobados. Final natural carga Tutorial. Skip de primera partida llega a Tutorial y replay a MainMenu, sin VO residual. No se añadieron fuentes ni listeners.

Comprobaciones de archivos: las 14 copias coinciden por SHA-256 con Sin Ruido; GUIDs y metadatos VO originales intactos; escena y WAV musicales idénticos a antes de la sustitución. Timeline difiere únicamente en 14 gains y 14 duraciones (+2 muestras por WAV), con todos los demás datos conservados.

Se conserva el aviso previo de listeners durante la carga, documentado en la integración anterior. La validación no acredita escucha humana ni calidad subjetiva de inteligibilidad/denoise; esa aprobación queda al usuario. No se midió LUFS. Evidencia temporal: Temp/IntroFinalVO/PlayValidation.json y Temp/IntroCleanVO/Audit.json.
