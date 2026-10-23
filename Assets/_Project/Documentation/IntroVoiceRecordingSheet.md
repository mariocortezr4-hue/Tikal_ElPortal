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
