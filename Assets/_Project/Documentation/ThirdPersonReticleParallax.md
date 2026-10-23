# Retículo TPS y aim de cámara

La posición autorada ya tenía anchors y pivot `(0.5, 0.5)` y `anchoredPosition=(0,0)`.
La causa era dinámica: el presenter proyectaba `PlayerInteraction.ResolvedAimPoint`,
que puede ser reemplazado por el primer impacto físico desde el pecho. Con shoulder
offset y una obstrucción, ese impacto se aparta del rayo central. La auditoría nativa
reprodujo un offset de Canvas `(-33.43,-18.98)`, aunque el aim de cámara seguía centrado.

La intención canónica es `Camera.ViewportPointToRay(new Vector3(.5f,.5f,0))`.
Su AimPoint es el impacto visible de ese rayo, o cualquier punto sobre él cuando no
hay impacto. Todos esos puntos proyectan al centro del viewport. En TPS,
`ReticlePresenter.TryGetViewportCenterOffset` convierte
`Camera.ViewportToScreenPoint((.5,.5,0))` al plano del padre mediante
`RectTransformUtility.ScreenPointToLocalPointInRectangle`, restando `parent.rect.center`.
Respeta CanvasScaler, Canvas camera y el rectángulo real de cámara. No sigue el
personaje, el hombro, Player.forward ni un impacto físico fuera del rayo central.

FPS mantiene exactamente `anchoredPosition=(0,0)`. Dot, Cross y None comparten la
posición del mismo contenedor; solo cambia su visibilidad. Pause/Journal mantienen
su ocultación y Settings/persistencia mantienen los modos existentes. LateUpdate
100 consume la pose final de cámara, sin suavizado ni jitter añadido.

`PlayerInteraction` no cambia: CameraRay selecciona intención y `InteractionOrigin`
verifica alcance físico de 3 m, orientación y obstrucciones. La distancia del boom
no reduce ese alcance. `ResolvedAimPoint` sigue siendo diagnóstico del resultado
físico, no la autoridad visual del retículo. No se modifican parámetros de cámara,
shoulder, boom, colisión ni environment.

`Tikal/Debug/Reticle Camera Aim Gizmos` activa gizmos exclusivos del Editor:
cian CameraRay, amarillo AimPoint, magenta InteractionRay y blanco la posición
del retículo con sus coordenadas de viewport. El alcance de dibujo de 30 m es
solo diagnóstico; no cambia el alcance de interacción. No añade UI a la build.

La validación local `Temp/ReticleCameraAim.request` acepta `audit` (reproducción
anterior) o `validate` (FPS/TPS, None/Dot/Cross, blancos a 2/5/10 m de cámara,
oclusor físico, interacción simple/Door y retracción real del boom). Usa Level01
en Play Mode, fixtures temporales y guardado aislado. No resuelve puzzles ni
modifica la escena guardada. Evidencia local: `_ReticleCameraAim_20261023`.

Validación nativa en Unity 6.6: 46 comprobaciones locales aprobadas, error de posición medido de 0 píxeles en las muestras FPS/TPS, blancos a 2/5/10 m, oclusor, interacción simple/Door y cámara retraída durante ocho frames. Cero Missing Scripts. Guardados personales sin cambios; escenas, environment y perfil de cámara sin modificaciones.
