using System;
using System.IO;
using System.Linq;
using Tikal.Cinematics;
using Tikal.Core;
using Tikal.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class PlazaTransitionAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/Cinematics/Transition_Level01_PlazaKeynes.unity";
        public const string Level01 = "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        public const string Level02 = "Assets/_Project/Scenes/Levels/Level02_PlazaKeynes.unity";
        public const string Art = "Assets/_Project/Timelines/PlazaTransition";
        public const string Evidence = "D:/ARCHIVOS/TREVIX/Proyectos/Videojuegos/Tikal_ElPortal/Para_Nivel_2/Transition_20261024";
        const float Duration = 10.5f;
        static PlayableDirector director;
        static TimelineAsset timeline;

        public static void RepairAndValidate()
        {
            if (!Application.dataPath.Replace('\\','/').Contains("/_PlazaKeynes_20261024/")) throw new Exception("Isolated authoring only.");
            EditorSceneManager.OpenScene(ScenePath);
            var d = UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();
            foreach(var track in ((TimelineAsset)d.playableAsset).GetOutputTracks().OfType<AnimationTrack>())
            {
                track.trackOffset = TrackOffset.ApplyTransformOffsets;
                EditorUtility.SetDirty(track);
            }
            EditorUtility.SetDirty(d.playableAsset);
            var camera = Camera.main;
            camera.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level02/MAT_PlazaKeynes_Sky.mat");
            RenderSettings.ambientLight = new Color(.07f,.08f,.07f);
            if (GameObject.Find("Gate_PocketFacade") == null)
                Fit(FindPrefab("ARQ_BloqueMaya_02"),GameObject.Find("Passage_VisualSet").transform,"Gate_PocketFacade",new Vector3(0,3.45f,-.85f),new Vector3(4.5f,3.6f,.45f));
            ExteriorFraming();
            SealTunnelEnd();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
            AssetDatabase.SaveAssets();
            PlazaTransitionValidation.Begin();
        }

        public static void Diagnose()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var d = UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();
            var output = new System.Text.StringBuilder();
            foreach (var track in ((TimelineAsset)d.playableAsset).GetOutputTracks())
                output.AppendLine(track.name + " -> " + d.GetGenericBinding(track));
            foreach (var time in new[]{0d,.75d,1.8d,3.6d,9.4d,10.4d})
            {
                d.time = time; d.Evaluate();
                output.AppendLine(time + " gate=" + GameObject.Find("GateLift_AnimationRoot").transform.position.ToString("F3") + " camera=" + Camera.main.transform.position.ToString("F3") + " actor=" + GameObject.Find("SelectedCharacter_CinematicProxy").transform.position.ToString("F3") + " fade=" + GameObject.Find("LocalFade").GetComponent<CanvasGroup>().alpha);
            }
            File.WriteAllText(Evidence+"/Timeline_Diagnostic.txt",output.ToString());
            EditorApplication.Exit(0);
        }

        public static void Run()
        {
            try
            {
                if (!Application.dataPath.Replace('\\','/').Contains("/_PlazaKeynes_20261024/")) throw new Exception("Isolated authoring only.");
                Directory.CreateDirectory(Evidence + "/Captures");
                Directory.CreateDirectory(Art); AssetDatabase.Refresh();
                if (File.Exists(ScenePath)) throw new Exception("Transition already exists. Refusing to overwrite.");
                var source = EditorSceneManager.OpenScene(Level01);
                var door = UnityEngine.Object.FindAnyObjectByType<Door>();
                var receiver = UnityEngine.Object.FindAnyObjectByType<SecretEntranceCommandReceiver>();
                if (!door || !receiver || receiver.MainDoor != door) throw new Exception("Real source Door/SecretCommand not verified.");
                var pivot = (Transform)new SerializedObject(door).FindProperty("_pivot").objectReferenceValue;
                var clone = UnityEngine.Object.Instantiate(pivot.gameObject);
                clone.name = "Door_VisualProxy";
                clone.transform.SetParent(null, true);
                var sourceBounds = BoundsOf(clone);
                var audit = new System.Text.StringBuilder();
                audit.AppendLine("Source scene: " + Level01);
                audit.AppendLine("Door: " + PathOf(door.transform));
                audit.AppendLine("Pivot: " + PathOf(pivot));
                audit.AppendLine("Door world bounds: " + sourceBounds.ToString("F4"));
                audit.AppendLine("Receiver range: " + receiver.Range);
                foreach (var definition in CharacterCatalog.All.Where(c => !c.IsLocked))
                {
                    var a = definition.VisualPrefab.GetComponentInChildren<Animator>(true);
                    audit.AppendLine(definition.DisplayName + " | " + AssetDatabase.GetAssetPath(definition.VisualPrefab) + " | avatar=" + (a.avatar != null && a.avatar.isHuman));
                    foreach (var c in a.runtimeAnimatorController.animationClips.Where(c => c.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0))
                        audit.AppendLine("  " + c.name + " | " + AssetDatabase.GetAssetPath(c));
                }
                File.WriteAllText(Evidence + "/Source_Audit.txt", audit.ToString());
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.MoveGameObjectToScene(clone, scene);
                EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(scene);
                foreach (var c in clone.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(c);
                foreach (var c in clone.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
                foreach (var c in clone.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(c);
                foreach (var c in clone.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(c);
                // A separate visual proxy retains the actual leaf's mesh/material and world orientation.
                float scale = 3.35f / sourceBounds.size.y;
                clone.transform.localScale *= scale;
                var b = BoundsOf(clone);
                clone.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
                var gate = new GameObject("GateLift_AnimationRoot");
                clone.transform.SetParent(gate.transform, true);
                var gateAnimator = gate.AddComponent<Animator>();
                var width = BoundsOf(clone).size.x;
                if (width < 1.8f) throw new Exception("Door is too narrow for this cinematic proxy.");
                var environment = new GameObject("Passage_VisualSet").transform;
                var stonePrefab = FindPrefab("ARQ_MuroMaya_01");
                var floorPrefab = FindPrefab("ARQ_PisoPiedraMaya_01");
                var blockPrefab = FindPrefab("ARQ_BloqueMaya_02");
                float side = width * .5f + .45f;
                Fit(stonePrefab, environment, "Facade_Left", new Vector3(-side, 0, 0), new Vector3(.9f, 7.2f, 1.3f));
                Fit(stonePrefab, environment, "Facade_Right", new Vector3(side, 0, 0), new Vector3(.9f, 7.2f, 1.3f));
                Fit(blockPrefab, environment, "Stone_Lintel", new Vector3(0, 3.45f, .1f), new Vector3(width + 1.8f, 1, 1.5f));
                Fit(blockPrefab, environment, "Lintel_Upper", new Vector3(0, 6.95f, .1f), new Vector3(width + 2, .65f, 1.5f));
                for (int i = 0; i < 6; i++)
                {
                    float z = 1.6f + i * 2.5f;
                    Fit(stonePrefab, environment, "Tunnel_Left_" + i, new Vector3(-side, 0, z), new Vector3(.9f, 3.4f, 2.55f));
                    Fit(stonePrefab, environment, "Tunnel_Right_" + i, new Vector3(side, 0, z), new Vector3(.9f, 3.4f, 2.55f));
                    Fit(floorPrefab, environment, "Tunnel_Roof_" + i, new Vector3(0, 3.38f, z), new Vector3(width + 1.8f, .35f, 2.55f));
                    Fit(floorPrefab, environment, "Tunnel_Floor_" + i, new Vector3(0, -.18f, z), new Vector3(width + 1.8f, .18f, 2.55f));
                }
                Fit(blockPrefab, environment, "Dark_End", new Vector3(0, 0, 14.5f), new Vector3(width + 1.8f, 3.7f, .7f));
                Fit(floorPrefab, environment, "Entrance_Court", new Vector3(0, -.18f, -5), new Vector3(13, .18f, 10));
                Fit(FindPrefab("VEG_ArbustoTropical_02"), environment, "Exterior_Growth_Left", new Vector3(-4, 0, -2), new Vector3(3, 2, 2));
                Fit(FindPrefab("VEG_HelechoTropical_02"), environment, "Exterior_Growth_Right", new Vector3(4, 0, -1.5f), new Vector3(3, 1.8f, 2.5f));
                var sun = new GameObject("Entrance_Daylight").AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.1f; sun.color = new Color(1, .88f, .7f);
                sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(48, -155, 0);
                var fill = new GameObject("Entrance_Bounce").AddComponent<Light>();
                fill.type = LightType.Point; fill.range = 6.5f; fill.intensity = 1.2f; fill.color = new Color(1, .78f, .53f);
                fill.transform.position = new Vector3(0, 2.5f, 1); fill.shadows = LightShadows.None;
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.035f, .04f, .035f);
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 8; RenderSettings.fogEndDistance = 19; RenderSettings.fogColor = new Color(.012f, .015f, .012f);
                var cam = new GameObject("CAM_Passage", typeof(Camera), typeof(AudioListener), typeof(Animator));
                cam.tag = "MainCamera"; cam.GetComponent<Camera>().fieldOfView = 58;
                cam.GetComponent<Camera>().nearClipPlane = .08f;
                cam.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
                RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level02/MAT_PlazaKeynes_Sky.mat");
                cam.GetComponent<Camera>().backgroundColor = new Color(.012f, .015f, .012f);
                var proxy = new GameObject("SelectedCharacter_CinematicProxy", typeof(Animator));
                var visualRoot = new GameObject("VisualRoot").transform; visualRoot.SetParent(proxy.transform, false);
                var canvas = new GameObject("LocalFade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(Animator));
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; canvas.GetComponent<Canvas>().sortingOrder = 31000;
                var panel = new GameObject("Black", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
                var rect = (RectTransform)panel.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                panel.GetComponent<Image>().color = Color.black;
                var systems = new GameObject("Passage_Timeline", typeof(PlayableDirector), typeof(PlazaPassageSequence));
                director = systems.GetComponent<PlayableDirector>(); director.playOnAwake = false; director.extrapolationMode = DirectorWrapMode.Hold;
                director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                timeline = ScriptableObject.CreateInstance<TimelineAsset>(); timeline.name = "TL_Level01_PlazaKeynes";
                AssetDatabase.CreateAsset(timeline, Art + "/TL_Level01_PlazaKeynes.playable");
                var doorClip = Clip("Door_Lift", gateAnimator);
                Curve(doorClip, "", typeof(Transform), "m_LocalPosition.y", new[]{0f, .8f, 2.8f, Duration}, new[]{0f,0f,3.65f,3.65f});
                var walk = Clip("Character_Passage", proxy.GetComponent<Animator>());
                Curve(walk, "", typeof(Transform), "m_LocalPosition.z", new[]{0f,2.6f,9.8f,Duration}, new[]{-3.2f,-3.2f,10.5f,10.5f}, true);
                var camera = Clip("Camera_Passage", cam.GetComponent<Animator>());
                var times = new[]{0f,2.6f,4.2f,7.0f,9.8f,Duration};
                var positions = new[]{new Vector3(-3.4f,2.4f,-7),new Vector3(-3.4f,2.4f,-7),new Vector3(-1,2.25f,-4),new Vector3(-.85f,2.2f,1.7f),new Vector3(-.7f,2.15f,6.6f),new Vector3(-.7f,2.15f,6.6f)};
                var targets = new[]{new Vector3(0,1.6f,0),new Vector3(0,1.6f,0),new Vector3(0,1.2f,0),new Vector3(0,1.2f,5),new Vector3(0,1.2f,10.5f),new Vector3(0,1.2f,10.5f)};
                for(int axis=0;axis<3;axis++) Curve(camera,"",typeof(Transform),"m_LocalPosition."+"xyz"[axis],times,positions.Select(p=>p[axis]).ToArray(),true);
                var rotations=positions.Select((p,i)=>Quaternion.LookRotation(targets[i]-p)).ToArray();
                for(int axis=0;axis<4;axis++) Curve(camera,"",typeof(Transform),"m_LocalRotation."+"xyzw"[axis],times,rotations.Select(q=>q[axis]).ToArray());
                camera.EnsureQuaternionContinuity();
                var fade = Clip("Passage_Fade", canvas.GetComponent<Animator>());
                Curve(fade,"",typeof(CanvasGroup),"m_Alpha",new[]{0f,.7f,9.6f,Duration},new[]{1f,0f,0f,1f});
                director.playableAsset = timeline;
                ExteriorFraming();
                SealTunnelEnd();
                Set(systems.GetComponent<PlazaPassageSequence>(),"_director",director);
                Set(systems.GetComponent<PlazaPassageSequence>(),"_visualRoot",visualRoot);
                Set(systems.GetComponent<PlazaPassageSequence>(),"_localFade",canvas.GetComponent<CanvasGroup>());
                var configured = EditorBuildSettings.scenes.ToList();
                foreach (var path in new[]{ScenePath,Level02}) if (!configured.Any(s=>s.path==path)) configured.Add(new EditorBuildSettingsScene(path,true));
                EditorSceneManager.SaveScene(scene,ScenePath);
                EditorBuildSettings.scenes = configured.ToArray();
                AssetDatabase.SaveAssets();
                File.WriteAllText(Evidence+"/Authoring_OK.txt","Created dedicated 10.5s Timeline, real Door visual proxy, selected-character visual, one camera, stone passage. Original scenes untouched.");
                PlazaTransitionValidation.Begin();
            }
            catch(Exception e){File.WriteAllText(Evidence+"/Authoring_Error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(2);}
        }

        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        static void SealTunnelEnd()
        {
            var parent=GameObject.Find("Passage_VisualSet").transform;
            float width=BoundsOf(GameObject.Find("Door_VisualProxy")).size.x;
            float side=width*.5f+.45f;
            if(GameObject.Find("Tunnel_Floor_5")==null)
            {
                Fit(FindPrefab("ARQ_PisoPiedraMaya_01"),parent,"Tunnel_Floor_5",new Vector3(0,-.18f,14.1f),new Vector3(width+1.8f,.18f,2.55f));
                Fit(FindPrefab("ARQ_PisoPiedraMaya_01"),parent,"Tunnel_Roof_5",new Vector3(0,3.38f,14.1f),new Vector3(width+1.8f,.35f,2.55f));
                Fit(FindPrefab("ARQ_MuroMaya_01"),parent,"Tunnel_Left_5",new Vector3(-side,0,14.1f),new Vector3(.9f,3.4f,2.55f));
                Fit(FindPrefab("ARQ_MuroMaya_01"),parent,"Tunnel_Right_5",new Vector3(side,0,14.1f),new Vector3(.9f,3.4f,2.55f));
            }
            if(GameObject.Find("Tunnel_EndBacking")==null)
            {
                var cap=GameObject.CreatePrimitive(PrimitiveType.Cube);cap.name="Tunnel_EndBacking";cap.transform.SetParent(parent,false);
                cap.transform.localPosition=new Vector3(0,1.8f,14.9f);cap.transform.localScale=new Vector3(width+1.8f,4,.3f);
                cap.GetComponent<Renderer>().sharedMaterial=GameObject.Find("Door_VisualProxy").GetComponentInChildren<Renderer>().sharedMaterial;
                UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
            }
        }
        static void ExteriorFraming()
        {
            if (GameObject.Find("ExteriorFraming") != null) return;
            var parent = new GameObject("ExteriorFraming").transform;
            parent.SetParent(GameObject.Find("Passage_VisualSet").transform,false);
            Fit(FindPrefab("ARQ_PisoPiedraMaya_01"),parent,"ExteriorGround",new Vector3(0,-.22f,-4),new Vector3(30,.18f,28));
            foreach(float side in new[]{-1f,1f})
            {
                Fit(FindPrefab("ARQ_MuroMaya_01"),parent,"FacadeWing_"+side,new Vector3(side*3.6f,0,.1f),new Vector3(4.5f,4.7f,1.2f));
                Fit(FindPrefab("VEG_ArbolTropical_02"),parent,"ExteriorCanopy_"+side,new Vector3(side*6.5f,0,2),new Vector3(10,12,9));
                Fit(FindPrefab("VEG_ArbustoTropical_02"),parent,"ExteriorUnderstorey_"+side,new Vector3(side*6,0,-.5f),new Vector3(7,3.2f,5));
            }
        }
        static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
        static GameObject FindPrefab(string name){var guids=AssetDatabase.FindAssets("PFB_"+name+" t:Prefab",new[]{"Assets/_Project/Prefabs/Environment"});return guids.Select(g=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).First(p=>p.name=="PFB_"+name);}
        static void Fit(GameObject prefab,Transform parent,string name,Vector3 foot,Vector3 size){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent,false);var b=BoundsOf(go);go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));b=BoundsOf(go);go.transform.position+=foot-new Vector3(b.center.x,b.min.y,b.center.z);foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);}
        static void Set(UnityEngine.Object target,string name,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static AnimationClip Clip(string name,Animator binding){var c=new AnimationClip{name=name,frameRate=30};AssetDatabase.CreateAsset(c,Art+"/"+name+".anim");var t=timeline.CreateTrack<AnimationTrack>(null,name);t.trackOffset=TrackOffset.ApplyTransformOffsets;var playable=t.CreateClip<AnimationPlayableAsset>();playable.start=0;playable.duration=Duration;((AnimationPlayableAsset)playable.asset).clip=c;((AnimationPlayableAsset)playable.asset).removeStartOffset=false;director.SetGenericBinding(t,binding);return c;}
        static void Curve(AnimationClip c,string path,Type type,string property,float[] times,float[] values,bool linear=false){var curve=new AnimationCurve(times.Select((t,i)=>new Keyframe(t,values[i])).ToArray());for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,linear?AnimationUtility.TangentMode.Linear:AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,linear?AnimationUtility.TangentMode.Linear:AnimationUtility.TangentMode.ClampedAuto);}AnimationUtility.SetEditorCurve(c,EditorCurveBinding.FloatCurve(path,type,property),curve);}
    }
}
