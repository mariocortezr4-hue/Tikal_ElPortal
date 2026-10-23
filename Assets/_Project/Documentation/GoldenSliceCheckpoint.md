# Golden Slice — checkpoint de revisión artística

> REGISTRO HISTÓRICO: describe el estado del 21 de octubre anterior al Entrance Hero y a su expansión. Sus cantidades, materiales y capturas no representan el estado actual de la entrada. Se conserva como evidencia del proceso. El checkpoint de entrada del 22 de octubre contiene 125 placements base y 130 de expansión; 62 anteriores permanecen desactivados. Validación dirigida: 208 colliders originales y ninguno artístico, sin cambios de lógica runtime. Las limitaciones y el estado sin commit descritos más abajo corresponden exclusivamente a aquella revisión.

Fecha: 21 de octubre de 2026. Rama `feature/level1-environment-art-pass`, base `a81b486`. Tres pasadas internas; placement detenido. **Candidato pendiente de aprobación visual humana**, no referencia aprobada del nivel.

## Alcance y recuperación

Solo Entrada/Main Door y Primera Plaza. WIP global respaldado fuera de Unity en `D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/_Level01Environment_20261021/Level01_Environment_GlobalPass_WIP_Backup.unity`; SHA-256 `62bc52c40c3739af8cb36bdd77fa11d3e8e3db4af19d7f0079d1bcd7f4a6bcdd`. Después se restauró únicamente la escena desde HEAD para retirar las 267 instancias globales. No se eliminó ningún archivo importado ni fuente.

Salvage por archivo en `D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/_Level01Environment_20261021/GoldenSliceSalvageAudit.csv`. De los 465 archivos nuevos iniciales (incluyendo metadatos/carpetas): A runtime/estructura 109; B prefabs 25; C materiales/texturas 86; D herramientas 8; F diagnósticos 6; G UV 231. E corresponde al placement global retirado tras backup; H: ninguno eliminado. Se conservan 12 prefabs nuevos, sus dependencias y 115 mallas UV; las no usadas quedan sin referencias de escena. No se generaron nuevos modelos ni texturas en esta fase.

`URPProjectSettings.asset` restaurado desde HEAD: la ruta `URPDefaultResources` era incidental. Importación, render y Play Mode funcionaron sin esa entrada. No se cambió la configuración del pipeline.

## Composición y colocación

177 instancias: 12 árboles de canopy, 22 árboles de fondo, 78 plantas/hojas, 42 rocas y 23 piezas arquitectónicas. Cuatro capas exactas: `1_GROUND`, `2_MID`, `3_CANOPY`, `4_BACKGROUND`, bajo `Environment/ENV_GoldenSlice`. Todas dentro del sector Entrada/Plaza; ninguna nueva en Observation, Puzzle05, False Path o Final Path.

Ground snap en `GoldenSliceArtPass.Ground/Put`: raycast vertical desde Y=50, ignora triggers y usa exclusivamente MeshCollider originales hijos de Terrain/NaturalBoundaries con normal Y>0.25. Prioriza suelo Terrain visible sobre la envolvente lateral oculta. Sin hit no instancia. Coloca el mínimo Y del bounds visual sobre hit.point, con margen de 5mm para vegetación. Árboles verticales. No se usa Y aproximada.

Rocas enterradas 10–18% de altura visual (dentro de 5–20%); algunas masas periféricas tienen escala diferenciada para cubrir bordes sin colisión nueva. La geometría/cota del suelo jugable permanece intacta. Las cuñas originales del sector se ocultan solo visualmente; sus colliders siguen siendo autoridad. No se reconstruyó Terrain.

Se corrigieron las bases aparentemente flotantes de la pasada 1, apoyadas en laderas visualmente ocultas. Última validación: **0 separaciones de base >3cm**, 0 posiciones sin hit; se comprueba por bounds central y raycast, no contacto de cada raíz/vértice. La revisión visual A–F complementa esta aproximación.

Sendero y plaza: hojas/piedras hundidas y pavimento roto en bordes y transiciones, centro despejado. Main Door: arco y columnas flanquean la puerta lógica, bloques con helechos/plantas en bases; no se invade su acceso funcional de 3m. La barrera previa conserva collider y se presenta como roca caída. Plaza: apertura central rodeada de ruinas, plantas y masa forestal; puzzle/mural y recompensas originales conservados. El mural sigue siendo la visual provisional, sin integrar kit final.

Canopy en varios planos; el objetivo perceptual 60–75% de selva no se considera una medición aprobada. Fondo con LOD1/LOD2 (Ceiba de dos niveles usa solo LOD1), sin sombras ni colliders. Sombras selectivas en árboles hero y arquitectura. Ambiente Trilight, sol cálido intensidad 1.3 y fog lineal 35–105m, solo en esta escena; sin paquetes, bloom ni volumetría nuevos. Se conservó orientación del sol.

## Assets instanciados

| Asset | Instancias |
|---|---:|
| `ARQ_ArcoMaya_01` | 1 |
| `ARQ_BloqueMaya_02` | 15 |
| `ARQ_ColumnaDerrumbada_01` | 1 |
| `ARQ_ColumnaMaya_01` | 2 |
| `ARQ_EstelaFragmentada_01` | 1 |
| `ARQ_PisoPiedraMaya_01` | 3 |
| `ROC_RocaFragmentada_01` | 24 |
| `ROC_RocaMusgo_02` | 7 |
| `ROC_RocaPlana_01` | 7 |
| `ROC_RocaTropical_01` | 4 |
| `VEG_ArbolTropical_02` | 9 |
| `VEG_ArbolTropical_03` | 8 |
| `VEG_ArbolTropical_04` | 8 |
| `VEG_ArbustoTropical_02` | 5 |
| `VEG_ArbustoTropical_03` | 35 |
| `VEG_CeibaJoven_01` | 8 |
| `VEG_HelechoTropical_02` | 19 |
| `VEG_HojasSecasMonton_01` | 11 |
| `VEG_PalmeraTropical_02` | 1 |
| `VEG_PlantaTropical_02` | 8 |

Materiales/mapas reutilizados: ForestFloor del Tutorial y piedra de MuroMaya; materiales de los prefabs. DoorMesh y feedback funcional conservan material original. El material de bancos verdes de la pasada global no se utiliza en la escena actual. Solo se reutilizan las copias UV de superficies del sector; no se regeneraron las 115.

## Evidencia y validación dirigida

Unity 6.6 interactivo, sin batchmode. 35 checks funcionales aprobados, sin errores: recorrido físico spawn→desvío→puerta→plaza en FPS y TPS, Interact abre la puerta, órbita TPS, checkpoint runtime y matriz SecretCommand (Closed/Opening/Open/Closing, dentro/fuera, FPS/TPS, teclado/gamepad, victoria con cero pistas). No se ejecutó la regresión completa de los cinco puzzles en esta fase.

Validación Editor: escena sin Missing Scripts, cuatro capas, colliders y componentes gameplay originales idénticos, transforms/activación originales iguales, referencias de puerta/secret/checkpoint válidas y materiales soportados. **208 colliders originales, 0 artísticos, 0 scripts runtime nuevos.** Auditoría de 1343 archivos originales: solo Level01 cambió; 0 bloques originales eliminados. Tutorial, demás escenas y código gameplay permanecen intactos respecto a HEAD.

Triángulos estimados por frustum y LOD seleccionado. La medición corregida cuenta solo el rango de submallas de cada renderer estático combinado. **Las cifras previas de Pass1/Pass2 y el primer PlayValidation de Pass3 sobrecontaban static batching; se sustituyen por MetricsValidation.json.** No incluye occlusion ni pases de sombras y no es rendimiento de build.

| Shot | Cámara | Golden Slice | Escena visible estimada |
|---|---|---:|---:|
| A | ThirdPerson | 717,059 | 734,489 |
| B | ThirdPerson | 704,098 | 722,776 |
| C | ThirdPerson | 570,002 | 585,208 |
| D | ThirdPerson | 435,342 | 450,554 |
| E | ThirdPerson | 449,805 | 452,263 |
| F | FirstPerson | 707,110 | 723,700 |

Capturas A–F, 1920×1080, cámaras de gameplay originales:

- [A — A_Spawn_MainDoor](<D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/_Level01Environment_20261021/GoldenSlice/Pass3/A_Spawn_MainDoor.png>)
- [B — B_Approach](<D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/_Level01Environment_20261021/GoldenSlice/Pass3/B_Approach.png>)
- [C — C_PlazaArrival](<D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/_Level01Environment_20261021/GoldenSlice/Pass3/C_PlazaArrival.png>)
- [D — D_PlazaCenter](<D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/_Level01Environment_20261021/GoldenSlice/Pass3/D_PlazaCenter.png>)
- [E — E_TPS_Lateral](<D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/_Level01Environment_20261021/GoldenSlice/Pass3/E_TPS_Lateral.png>)
- [F — F_FPS_Spawn](<D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/_Level01Environment_20261021/GoldenSlice/Pass3/F_FPS_Spawn.png>)

## Límites conocidos y siguiente decisión humana

- Persisten vistas del blockout de sectores posteriores y algunos huecos de horizonte lateral. Se reforzó el cierre dentro del sector sin modificar esas zonas. Por ello el resultado todavía no se presenta como Golden Slice aprobado.
- La barrera original de aproximación oculta parte baja de Main Door desde spawn, especialmente FPS. No se cambió su collider ni la ruta gameplay para mejorar una captura.
- La puerta conserva su mesh/material provisional; el arco y las ruinas mejoran su contexto, pero falta aprobación de su lectura visual. Hay piezas de ruina con voladizos propios del prefab.
- Se retiró el tronco que ocupaba la cámara B. Aún hace falta playtest humano de aproximación y órbita en todos los bordes, y revisar contacto visual de raíces sobre terreno irregular.
- La densidad, siluetas de hojas, repetición de piedra y equilibrio de cielo requieren dirección artística humana. No se certifica FPS de build ni porcentaje exacto de canopy.
- `git diff --check` detectó espacios finales en campos YAML `value:` escritos por Unity. No es un fallo de Play Mode; quedan pendientes para una futura preparación de commit, sin reformatear la escena en esta revisión.

Sin staging, commit, push, merge ni nueva rama. Escena guardada. Se detiene tras este checkpoint; no continuar otras zonas sin aprobación.
