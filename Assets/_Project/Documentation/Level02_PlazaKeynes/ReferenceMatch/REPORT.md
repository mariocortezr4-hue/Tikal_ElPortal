# Plaza Keynes: cierre de segunda pasada artística

Rama: `feature/level2-plaza-keynes-art-pass`. Base publicada verificada: `0e221c3`, que conserva el entorno de `4d42e68`, la transición de `4a98609` y Pause compartido. No se reconstruyó la plaza.

## Comparación con referencias

Se analizaron las copias locales `../References/Imagen_Reference1.png` y `../References/Imagen_Reference2.png`. Se tomó de la primera la plaza amplia con estatua central, ruinas próximas y templos periféricos; de la segunda, las capas arquitectónicas, alturas distintas, fondo profundo y luz cálida lateral. Se conserva el centro abierto y una distribución asimétrica.

La escala percibida mejora con cuatro templos visuales compuestos de 26 piezas y tres landmarks distantes ampliados en sus emplazamientos. Las dos estructuras cercanas y sus escaleras caminables originales siguen intactas. Keynes conserva posición `(0,0.7200005,0)` y escala `(10,10,10)`; dos bloques sobre la segunda terraza refuerzan su base sin crear una pirámide. Su visibilidad se comprobó desde tres rutas.

Se variaron moderadamente 48 secciones de pavimento existentes, sin cambiar sus rutas ni el terreno físico. Cuatro sectores nuevos incluyen 12 muros, cuatro portales, ocho columnas y cuatro columnas caídas. Se añadieron 20 rocas y 64 plantas bajas; 32 de esas plantas se recolocaron hacia los bordes. No hay árboles grandes nuevos en el centro.

La periferia incorpora ocho árboles midground y 36 de fondo, más 158 masas de canopy en tres mallas que reutilizan el atlas aprobado. Tres mallas económicas de horizonte y siete apoyos bajo templos integran las pendientes. El cierre corto final corrigió solamente las huellas de esos apoyos: **cero placements nuevos** después de la instrucción de cierre.

Una sola Directional Light cálida, rotación `(24,-48,0)`, intensidad `2`; fog exponencial cuadrático de densidad `0.00185`. El nuevo cielo es exclusivo de Level02 porque el anterior también se usa en la transición. No se rehízo la iluminación durante el cierre, no hay volumétricos ni luces locales nuevas. Piedra, suelo y vegetación reutilizan materiales existentes. No se importaron assets externos.

La comparación final conserva limitaciones visibles respecto a las referencias: detalle modular de piedra, pavimento repetitivo, uniones de arquitectura sobre pendientes, cielo procedural sencillo y relieve lejano poco detallado. No se afirma equivalencia visual ni aprobación artística humana.

## Inventario y coste

| Observación del entorno | Antes | Después |
|---|---:|---:|
| Placements de prefab | 723 | 905 |
| LODGroups | 723 | 905 |
| Renderer en jerarquías activas | 1615 | 2071 |
| Colliders habilitados del entorno | 299 | 327 |
| Renderer potenciales con sombras | 1017 | 1004 |
| Luces realtime | 1 | 1 |
| Suma de primeros LODs y mallas independientes | 5,260,388 | 6,378,390 |
| Suma de últimos LODs y mallas independientes | 2,412,184 | 2,822,026 |

Trabajo acumulado: 182 instancias de assets existentes y 13 mallas auxiliares: tres relieves, siete apoyos y tres sectores de canopy. El cierre final no añadió instancias. Los 28 colliders añadidos pertenecen a 12 muros, ocho columnas, cuatro columnas caídas y cuatro rocas accesibles. No se modificaron colliders originales; vegetación, arquitectura distante, canopy y relieve carecen de colisión.

Foreground conserva LODs detallados; fondo usa LODs reducidos y descarte lejano. Los renderer excluidos se desactivan. Sombras desactivadas en fondo y reducidas en árboles midground; la estatua y arquitectura próxima mantienen sombras.

Las cifras de renderer y sombras incluyen niveles LOD alternativos, no objetos renderizados simultáneamente. Las sumas de geometría son inventario completo sin frustum culling. En las capturas finales UnityStats registró 4,516,894–4,591,074 triángulos y 1100–1138 draw calls globales del Editor. No son mediciones aisladas por cámara ni certificación de rendimiento en GPU integrada. No se hizo profiling profundo ni benchmark de build.

## Validación y alcance

`FinalValidation.json`: **151 comprobaciones aprobadas, cero errores**. Carga/spawn, recorrido FPS/TPS por el camino principal, visibilidad de Keynes, LODs válidos, apoyo del dressing accesible, ausencia de Missing Scripts/Materials y unicidad de Player/Input/Camera/AudioListener/EventSystem. Los recorridos completos anteriores por ambos lados, monumento y templo se conservan en `PlayValidation.json`: 174 comprobaciones aprobadas. No se ejecutaron regresiones globales.

Entre los archivos preexistentes solo se modifica la escena de Level02, además de la caché TMP que ya estaba fuera de scope. No se alteraron Player, Input, cámaras, Pause, selección, progreso, Main Menu, Intro, Tutorial, Level01 ni transición. Unity actualizó la caché TMP al reabrirse; no se editó ni se revirtió directamente y todo su cambio se excluye del commit.

## Nueve capturas finales de Unity

Capturas a 1600×900, sin retoque ni generación de imágenes. También se conservan las diez capturas anteriores como evidencia del trabajo acumulado.

1. [PlazaKeynes_Final_Entrance.png](Captures/PlazaKeynes_Final_Entrance.png)
2. [PlazaKeynes_Final_Center.png](Captures/PlazaKeynes_Final_Center.png)
3. [PlazaKeynes_Final_Keynes.png](Captures/PlazaKeynes_Final_Keynes.png)
4. [PlazaKeynes_Final_Left.png](Captures/PlazaKeynes_Final_Left.png)
5. [PlazaKeynes_Final_Right.png](Captures/PlazaKeynes_Final_Right.png)
6. [PlazaKeynes_Final_Skyline.png](Captures/PlazaKeynes_Final_Skyline.png)
7. [PlazaKeynes_Final_Background.png](Captures/PlazaKeynes_Final_Background.png)
8. [PlazaKeynes_Final_Wide.png](Captures/PlazaKeynes_Final_Wide.png)
9. [PlazaKeynes_Final_TPS.png](Captures/PlazaKeynes_Final_TPS.png)

El commit y la publicación se informan en el chat. No se hace merge ni se continúa otra pasada después del cierre.
