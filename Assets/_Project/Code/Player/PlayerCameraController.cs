using Tikal.Input;
using Tikal.Persistence;
using UnityEngine;

namespace Tikal.Player
{
    // The camera pose must be ready before PlayerInteraction's default-order LateUpdate.
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController), typeof(PlayerLook))]
    public sealed class PlayerCameraController : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera = null;
        [SerializeField] private Transform _firstPersonTarget = null;
        [SerializeField] private Transform _thirdPersonPivot = null;
        [SerializeField] private Transform _lookTarget = null;
        [SerializeField] private GameObject _characterVisualRoot = null;
        [SerializeField] private GameObject _thirdPersonHumanProxy = null;
        [SerializeField] private ThirdPersonCameraProfile _profile = null;
        [SerializeField] private PersistentSettingsApplier _settingsApplier = null;

        private PlayerController _player;
        private PlayerLook _look;
        private PlayerInputReader _input;
        private Vector3 _fpsLocalPosition;
        private Quaternion _fpsLocalRotation;
        private float _baseFov;
        private float _yaw, _pitch, _smoothYaw, _smoothPitch;
        private float _yawVelocity, _pitchVelocity, _distanceVelocity;
        private float _distance;
        private Vector3 _positionVelocity;
        private bool _initialized, _snap;
        private SphereCollider _collisionProbe;

        public CameraMode Mode { get; private set; } = CameraMode.FirstPerson;
        public float Yaw => _yaw;
        public float Pitch => _pitch;
        public ThirdPersonCameraProfile Profile => _profile;
        public bool IsConfigured => _mainCamera != null && _firstPersonTarget != null &&
            _thirdPersonPivot != null && _lookTarget != null && _characterVisualRoot != null &&
            _thirdPersonHumanProxy != null && _settingsApplier != null && _profile != null && _profile.IsValid;
        public bool OwnsLook => isActiveAndEnabled && IsConfigured && Mode == CameraMode.ThirdPerson;
        public Transform MovementOrientation => _mainCamera != null ? _mainCamera.transform : null;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _look = GetComponent<PlayerLook>();
            _input = GetComponent<PlayerInputReader>();
            if (!IsConfigured)
            {
                Debug.LogError("Apply Third Person Camera Foundation authoring: camera references/profile are incomplete.", this);
                enabled = false;
                return;
            }
            _fpsLocalPosition = _mainCamera.transform.localPosition;
            _fpsLocalRotation = _mainCamera.transform.localRotation;
            _baseFov = _mainCamera.fieldOfView;
            // Disabled query shape: never participates in gameplay physics or self collision.
            var probe = new GameObject("CameraCollisionQuery");
            probe.transform.SetParent(transform, false);
            _collisionProbe = probe.AddComponent<SphereCollider>();
            _collisionProbe.enabled = false;
            _initialized = true;
        }

        private void OnEnable()
        {
            if (_settingsApplier == null) return;
            _settingsApplier.SettingsApplied += ApplySettings;
            if (_initialized && _settingsApplier.CurrentSettings != null)
                ApplySettings(_settingsApplier.CurrentSettings);
        }

        private void Start()
        {
            // All Awake calls (including persistence and PlayerLook) have finished.
            ApplySettings(_settingsApplier.CurrentSettings ?? GameSettingsData.CreateDefault());
        }

        private void OnDisable()
        {
            if (_settingsApplier != null) _settingsApplier.SettingsApplied -= ApplySettings;
            if (_initialized) ApplyCameraMode(CameraMode.FirstPerson);
        }

        private void OnDestroy()
        {
            if (_collisionProbe != null) Destroy(_collisionProbe.gameObject);
        }

        private void ApplySettings(GameSettingsData settings)
        {
            // Reuse PlayerLook's settings; neither controller creates another persistence source.
            _look.ApplySettings(settings.MouseSensitivity, settings.GamepadSensitivity, settings.InvertY);
            ApplyCameraMode(settings.CameraMode);
        }

        public void ApplyCameraMode(CameraMode mode)
        {
            var next = mode == CameraMode.ThirdPerson ? mode : CameraMode.FirstPerson;
            bool entering = Mode != next;
            Mode = next;
            bool thirdPerson = Mode == CameraMode.ThirdPerson;
            if (_characterVisualRoot != null) _characterVisualRoot.SetActive(thirdPerson);
            if (_thirdPersonHumanProxy != null) _thirdPersonHumanProxy.SetActive(thirdPerson);
            if (!_initialized) return;
            if (!thirdPerson)
            {
                // CameraRoot still owns yaw/pitch/crouch and PlayerLook still owns sprint FOV.
                _mainCamera.transform.SetParent(_firstPersonTarget, false);
                _mainCamera.transform.localPosition = _fpsLocalPosition;
                _mainCamera.transform.localRotation = _fpsLocalRotation;
                if (entering) _mainCamera.fieldOfView = _baseFov;
            }
            else if (entering)
            {
                _yaw = transform.eulerAngles.y;
                _pitch = Mathf.Clamp(Mathf.DeltaAngle(0, _look.CameraRoot.localEulerAngles.x), _profile.PitchMin, _profile.PitchMax);
                _smoothYaw = _yaw;
                _smoothPitch = _pitch;
                _yawVelocity = _pitchVelocity = _distanceVelocity = 0;
                _positionVelocity = Vector3.zero;
                _distance = _profile.Distance;
                _mainCamera.fieldOfView = _baseFov;
                _snap = true;
                // A deliberate mode change also gets a valid pose while a UI lock is active.
                // This does not consume Look or release the shared gameplay lock.
                UpdateThirdPersonPose(_profile.PositionSmoothTime);
            }
        }

        public void TickLook(Vector2 look, LookInputMode mode, float deltaTime)
        {
            if (!OwnsLook || _player == null || !_player.GameplayActive || _input == null ||
                !_input.GameplayEnabled || deltaTime <= 0) return;
            var rotation = mode == LookInputMode.Delta ? look * _look.MouseSensitivity :
                Vector2.ClampMagnitude(look, 1f) * (_look.GamepadSensitivity * deltaTime);
            _yaw = Mathf.Repeat(_yaw + rotation.x, 360f);
            _pitch = Mathf.Clamp(_pitch + rotation.y * (_look.InvertY ? 1f : -1f), _profile.PitchMin, _profile.PitchMax);
        }

        private void LateUpdate()
        {
            if (!OwnsLook || Time.deltaTime <= 0 || _player == null || !_player.GameplayActive ||
                _input == null || !_input.GameplayEnabled) return;
            UpdateThirdPersonPose(Time.deltaTime);
        }

        private void UpdateThirdPersonPose(float deltaTime)
        {
            _smoothYaw = Mathf.SmoothDampAngle(_smoothYaw, _yaw, ref _yawVelocity, _profile.RotationSmoothTime, Mathf.Infinity, deltaTime);
            _smoothPitch = Mathf.SmoothDampAngle(_smoothPitch, _pitch, ref _pitchVelocity, _profile.RotationSmoothTime, Mathf.Infinity, deltaTime);
            var rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0);
            var pivotLocal = _thirdPersonPivot.localPosition;
            // Crouch uses the existing eye-height safety without changing movement.
            pivotLocal.y = Mathf.Min(_profile.PivotHeight, _look.CameraRoot.localPosition.y);
            _thirdPersonPivot.localPosition = pivotLocal;
            var pivot = _thirdPersonPivot.position;
            var desiredOffset = rotation * new Vector3(_profile.ShoulderOffset, 0, -_profile.Distance);
            var direction = desiredOffset.normalized;
            var fullDistance = desiredOffset.magnitude;
            var safeDistance = SafeDistance(pivot, direction, fullDistance);
            var targetDistance = safeDistance;
            // Occlusion retracts immediately; the return uses the profile's smoothing.
            _distance = safeDistance < _distance ? safeDistance : Mathf.SmoothDamp(_distance, targetDistance,
                ref _distanceVelocity, _profile.PositionSmoothTime, Mathf.Infinity, deltaTime);
            if (safeDistance <= _distance) _distanceVelocity = 0;
            var desired = pivot + direction * _distance;
            var candidate = _snap ? desired : Vector3.SmoothDamp(_mainCamera.transform.position, desired,
                ref _positionVelocity, _profile.PositionSmoothTime, Mathf.Infinity, deltaTime);
            // Smoothing can cross a corner: cast again against the actual smoothed path.
            var offset = candidate - pivot;
            if (offset.sqrMagnitude > 0)
                candidate = pivot + offset.normalized * SafeDistance(pivot, offset.normalized, offset.magnitude);
            candidate = ResolveOverlaps(candidate);
            _mainCamera.transform.SetPositionAndRotation(candidate, rotation);
            _lookTarget.position = pivot + rotation * Vector3.forward * _profile.Distance;
            _snap = false;
        }

        private bool IsSelf(Collider collider) => collider == null || collider.transform.IsChildOf(transform);

        private float SafeDistance(Vector3 origin, Vector3 direction, float distance)
        {
            var safe = distance;
            // All hits avoids a fixed-size buffer silently discarding a legitimate wall.
            foreach (var hit in Physics.SphereCastAll(origin, _profile.CollisionRadius, direction, distance,
                _profile.CollisionMask, QueryTriggerInteraction.Ignore))
                if (!IsSelf(hit.collider)) safe = Mathf.Min(safe, Mathf.Max(0, hit.distance - _profile.CollisionPadding));
            // MinimumDistance is a framing preference, never a license to enter a wall.
            return safe;
        }

        private Vector3 ResolveOverlaps(Vector3 position)
        {
            // SphereCast does not report an initial overlap. Resolve it explicitly, including
            // low ceilings/pivot inside geometry, using a disabled sphere solely as query shape.
            _collisionProbe.radius = _profile.CollisionRadius;
            for (var pass = 0; pass < 8; pass++)
            {
                bool moved = false;
                foreach (var obstacle in Physics.OverlapSphere(position, _profile.CollisionRadius,
                    _profile.CollisionMask, QueryTriggerInteraction.Ignore))
                {
                    if (IsSelf(obstacle)) continue;
                    if (Physics.ComputePenetration(_collisionProbe, position, Quaternion.identity,
                        obstacle, obstacle.transform.position, obstacle.transform.rotation, out var direction, out var depth))
                    {
                        position += direction * (depth + _profile.CollisionPadding);
                        moved = true;
                    }
                }
                if (!moved) break;
            }
            return position;
        }
    }
}
