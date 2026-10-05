using Tikal.Persistence;
using UnityEditor;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    public static class LocalProgressTestingUtility
    {
        [MenuItem("Tikal/Testing/Reset Local Progress")]
        public static void ResetLocalProgress()
        {
            var service = new GamePersistenceService();
            if (!EditorUtility.DisplayDialog(
                    "Reset Local Progress",
                    $"Reset local development progress?\n\nOnly this file will be replaced:\n{service.ProgressPath}\n\nSettings will not be changed.",
                    "Reset Progress",
                    "Cancel"))
                return;

            service.ResetProgress();
            Debug.Log($"TIKAL_LOCAL_PROGRESS_RESET {service.ProgressPath}");
        }
    }
}
