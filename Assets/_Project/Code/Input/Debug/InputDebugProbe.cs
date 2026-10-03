using UnityEngine;

namespace Tikal.Input
{
    [DisallowMultipleComponent]
    public sealed class InputDebugProbe : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _reader;

#if UNITY_EDITOR || DEBUG
        private PlayerInputReader _subscribedReader;
        private int _jumpCount;
        private int _interactCount;
        private int _secretCount;
        private int _pauseCount;
        private string _lastEvent = "None";

        private void OnEnable()
        {
            if (_reader == null)
            {
                UnityEngine.Debug.LogWarning("InputDebugProbe requires a PlayerInputReader reference.", this);
                return;
            }

            _subscribedReader = _reader;
            _subscribedReader.JumpPressed += OnJumpPressed;
            _subscribedReader.InteractPressed += OnInteractPressed;
            _subscribedReader.SecretCommandPressed += OnSecretCommandPressed;
            _subscribedReader.PausePressed += OnPausePressed;
        }

        private void OnDisable()
        {
            if (_subscribedReader == null)
                return;

            _subscribedReader.JumpPressed -= OnJumpPressed;
            _subscribedReader.InteractPressed -= OnInteractPressed;
            _subscribedReader.SecretCommandPressed -= OnSecretCommandPressed;
            _subscribedReader.PausePressed -= OnPausePressed;
            _subscribedReader = null;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 390, 310), "Input Debug (development only)", GUI.skin.window);
            if (_reader == null)
            {
                GUILayout.Label("Assign a PlayerInputReader reference.");
            }
            else
            {
                IPlayerInputReader input = _reader;
                GUILayout.Label($"Move: {input.Move:F2}");
                GUILayout.Label($"Look: {input.Look:F2}");
                GUILayout.Label($"SprintHeld: {input.SprintHeld}");
                GUILayout.Label($"CrouchHeld: {input.CrouchHeld}");
                GUILayout.Space(8);
                GUILayout.Label($"JumpPressed: {_jumpCount}");
                GUILayout.Label($"InteractPressed: {_interactCount}");
                GUILayout.Label($"SecretCommandPressed: {_secretCount}");
                GUILayout.Label($"PausePressed: {_pauseCount}");
                GUILayout.Label($"Last event: {_lastEvent}");
            }
            GUILayout.EndArea();
        }

        private void OnJumpPressed() { _jumpCount++; RecordEvent(nameof(IPlayerInputReader.JumpPressed)); }
        private void OnInteractPressed() { _interactCount++; RecordEvent(nameof(IPlayerInputReader.InteractPressed)); }
        private void OnSecretCommandPressed() { _secretCount++; RecordEvent(nameof(IPlayerInputReader.SecretCommandPressed)); }
        private void OnPausePressed() { _pauseCount++; RecordEvent(nameof(IPlayerInputReader.PausePressed)); }

        private void RecordEvent(string eventName)
        {
            _lastEvent = eventName;
            UnityEngine.Debug.Log($"[InputDebugProbe] {eventName}", this);
        }
#endif
    }
}
