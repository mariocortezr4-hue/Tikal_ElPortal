using System.IO;
using UnityEditor;
namespace Tikal.Tests.Editor
{
    [InitializeOnLoad] public static class ElderCinematicRequests
    {
        static ElderCinematicRequests(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/ElderLegacyValidate.request"))
            {
                File.Delete("Temp/ElderLegacyValidate.request");
                try{AncientMapIntroValidation.Validate();File.WriteAllText("Temp/ElderLegacyValidation.txt","PASS");}
                catch(System.Exception e){File.WriteAllText("Temp/ElderLegacyValidation.txt",e.ToString());UnityEngine.Debug.LogException(e);}
                finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);ElderCinematicAuthoring.Validate();}
            }
            if(File.Exists("Temp/ElderApply.request")){File.Delete("Temp/ElderApply.request");ElderCinematicAuthoring.Apply();}
            if(File.Exists("Temp/ElderCapture.request")){File.Delete("Temp/ElderCapture.request");ElderCinematicPilot.Reaudition();}
            if(File.Exists("Temp/ElderPilot.request")){File.Delete("Temp/ElderPilot.request");ElderCinematicPilot.Import();}
        }
    }
}
