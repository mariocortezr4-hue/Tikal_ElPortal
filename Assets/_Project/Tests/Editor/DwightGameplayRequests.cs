using System.IO;
using UnityEditor;
using UnityEngine;
namespace Tikal.Tests.Editor
{
    [InitializeOnLoad] public static class DwightGameplayRequests
    {
        static DwightGameplayRequests(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            const string flag="Temp/DwightGameplayApply.request";
            if(!File.Exists(flag))return;
            File.Delete(flag);DwightGameplayAuthoring.Apply();
        }
    }
}
