using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Tikal.Tests.Editor
{
    public static class JournalInputAuthoring
    {
        public static void Run()
        {
            try
            {
                string path="Assets/_Project/Input/TikalInputActions.inputactions";
                var asset=InputActionAsset.FromJson(File.ReadAllText(path));var map=asset.FindActionMap("Global",true);
                if(map.FindAction("Journal")!=null)throw new Exception("Journal already exists.");
                var action=map.AddAction("Journal",InputActionType.Button,expectedControlLayout:"Button");
                action.AddBinding("<Keyboard>/j").WithGroup("KeyboardMouse");action.AddBinding("<Gamepad>/select").WithGroup("Gamepad");
                File.WriteAllText(path,asset.ToJson(),new System.Text.UTF8Encoding(false));UnityEngine.Object.DestroyImmediate(asset);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                EditorApplication.delayCall+=Complete;
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void Complete()
        {
            if(!File.ReadAllText("Assets/_Project/Input/TikalInputActions.cs").Contains("m_Global_Journal")){Debug.LogError("Official importer has not generated Journal.");if(Application.isBatchMode)EditorApplication.Exit(1);return;}
            Debug.Log("TIKAL_JOURNAL_INPUT_GENERATED_BY_OFFICIAL_IMPORTER");if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}
