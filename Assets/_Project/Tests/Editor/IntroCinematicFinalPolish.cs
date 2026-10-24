using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Tikal.Cinematics;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class IntroCinematicFinalPolish
    {
        public const string Evidence="../_IntroFinalPolish_20261023";
        public const string MusicPath="Assets/_Project/Audio/Music/Cinematics/MUS_Intro_Suspenso_Continuous.wav";
        public const string MapPath="Assets/Tikal/NonCharacterKit/Prologue/Prefabs/PFB_PRP_MapaAntiguo_01.prefab";
        public const string Title="TIKAL EL EXPLORADOR";
        static IntroCinematicFinalPolish(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/IntroFinalPolishApply.request"))return;
            File.Delete("Temp/IntroFinalPolishApply.request");Directory.CreateDirectory(Evidence);
            try{Apply();var paths=new[]{AncientMapIntroAuthoring.ScenePath,AncientMapIntroAuthoring.TimelinePath};var bytes=paths.Select(File.ReadAllBytes).ToArray();Apply();if(!paths.Select((p,i)=>bytes[i].SequenceEqual(File.ReadAllBytes(p))).All(x=>x))throw new Exception("Final polish is not byte-idempotent");File.WriteAllText(Evidence+"/Authoring.txt","PASS: applied twice byte-identical");}
            catch(Exception e){File.WriteAllText(Evidence+"/AuthoringError.txt",e.ToString());Debug.LogException(e);}
        }
        static void Set(UnityEngine.Object o,string name,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        [MenuItem("Tikal/Authoring/Apply Intro Cinematic Final Polish")]
        public static void Apply()
        {
            if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Use Edit Mode with saved scene.");
            var scene=EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);var director=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();var timeline=(TimelineAsset)director.playableAsset;
            if(Math.Abs(timeline.duration-85.5)>.01)throw new Exception("Unexpected Intro duration: preserve before editing.");
            var tracks=ElderCinematicAuthoring.Tracks(timeline).ToArray();var cues=tracks.OfType<CinematicCueTrack>().SelectMany(t=>t.GetClips()).ToArray();
            foreach(var cue in cues.Where(c=>((CinematicCueAsset)c.asset).Type==CinematicCueType.Title))
            {var s=new SerializedObject(cue.asset);s.FindProperty("_text").stringValue=Title;s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(cue.asset);}
            var presenter=UnityEngine.Object.FindAnyObjectByType<CinematicSubtitlePresenter>();var data=new SerializedObject(presenter);var card=((GameObject)data.FindProperty("_titleCard").objectReferenceValue).GetComponent<RectTransform>();var text=(TMP_Text)data.FindProperty("_titleText").objectReferenceValue;text.text=Title;text.enabled=false;Stretch(card,new Vector2(.05f,.08f),new Vector2(.95f,.92f));
            // Reuse the final menu brand artwork without modifying that asset or the Menu scene.
            var backing=Image("Intro brand stone",card);Stretch(backing.rectTransform,new Vector2(.15f,.23f),new Vector2(.85f,.77f));backing.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/MainMenu/UI_Menu_Stone.png");backing.type=UnityEngine.UI.Image.Type.Tiled;backing.color=new Color(.045f,.057f,.046f,.88f);backing.transform.SetAsFirstSibling();
            var logo=Image("Intro final brand",card);Stretch(logo.rectTransform,new Vector2(.23f,.30f),new Vector2(.77f,.70f));logo.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/MainMenu/UI_Tikal_StoneGold_Logo.png");logo.color=Color.white;logo.preserveAspect=true;
            var outline=backing.GetComponent<Outline>()??backing.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.62f,.48f,.25f,.7f);outline.effectDistance=new Vector2(1,-1);
            var importer=(AudioImporter)AssetImporter.GetAtPath(MusicPath);if(importer==null)throw new Exception("Import continuous music first");var settings=importer.defaultSampleSettings;
            if(settings.loadType!=AudioClipLoadType.Streaming||settings.compressionFormat!=AudioCompressionFormat.Vorbis||Mathf.Abs(settings.quality-.85f)>.001f){settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.85f;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
            var audio=AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);var track=tracks.OfType<AudioTrack>().Single(t=>t.name=="Music");var clip=track.GetClips().Single();((AudioPlayableAsset)clip.asset).clip=audio;((AudioPlayableAsset)clip.asset).loop=false;clip.duration=timeline.duration;clip.easeInDuration=clip.easeOutDuration=0;
            var mix=UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>();var serialized=new SerializedObject(mix);serialized.FindProperty("_envelope").animationCurveValue=BuildEnvelope(cues,timeline.duration);serialized.ApplyModifiedPropertiesWithoutUndo();
            var map=ElderCinematicAuthoring.Find("AncientMap_Proxy");var constraint=map.GetComponent<ParentConstraint>();if(constraint.sourceCount!=2)throw new Exception("Keep original two-hand map constraint");
            var visual=map.Find("Ancient map final visual");if(visual==null){visual=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MapPath),scene)).transform;visual.name="Ancient map final visual";visual.SetParent(map,false);}
            // Parent is the original scaled cube carrier; inverse scale gives the authored 62 x 42 cm sheet.
            visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;visual.localScale=new Vector3(1/map.localScale.x,1/map.localScale.y,1/map.localScale.z);
            var proxy=map.GetComponent<MeshRenderer>();if(proxy!=null)proxy.enabled=false;var collider=map.GetComponent<Collider>();if(collider!=null)collider.enabled=false;
            foreach(var field in new[]{"_dateCard","_narrativePanel","_subtitlePanel","_titleCard"})((GameObject)data.FindProperty(field).objectReferenceValue).SetActive(false);
            foreach(var field in new[]{"_dateText","_narrativeText","_speakerText","_dialogueText"})((TMP_Text)data.FindProperty(field).objectReferenceValue).text=string.Empty;
            EditorUtility.SetDirty(track);EditorUtility.SetDirty(clip.asset);EditorUtility.SetDirty(timeline);EditorUtility.SetDirty(mix);EditorUtility.SetDirty(text);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("INTRO_FINAL_POLISH_APPLIED");
        }
        public static AnimationCurve BuildEnvelope(TimelineClip[] cues,double duration)
        {
            var windows=cues.Where(c=>{var type=((CinematicCueAsset)c.asset).Type;return type==CinematicCueType.Subtitle||type==CinematicCueType.Narrative;}).OrderBy(c=>c.start).ToArray();
            var merged=new List<Vector2>();foreach(var c in windows){if(merged.Count>0&&c.start-merged[merged.Count-1].y<1.6){var last=merged[merged.Count-1];last.y=(float)c.end;merged[merged.Count-1]=last;}else merged.Add(new Vector2((float)c.start,(float)c.end));}
            var keys=new List<Keyframe>{new Keyframe(0,0),new Keyframe(.75f,.20f)};float previous=.20f;
            foreach(var w in merged){keys.Add(new Keyframe(w.x-.65f,previous));keys.Add(new Keyframe(w.x,.006f));keys.Add(new Keyframe(w.y,.006f));keys.Add(new Keyframe(w.y+1.1f,.10f));previous=.10f;}
            keys.Add(new Keyframe((float)duration-.6f,.10f));keys.Add(new Keyframe((float)duration,0));return new AnimationCurve(keys.OrderBy(k=>k.time).ToArray());
        }
        static UnityEngine.UI.Image Image(string name,Transform parent){var child=parent.Find(name);if(child==null){child=new GameObject(name,typeof(RectTransform)).transform;child.SetParent(parent,false);}var image=child.GetComponent<UnityEngine.UI.Image>()??child.gameObject.AddComponent<UnityEngine.UI.Image>();image.raycastTarget=false;return image;}
        static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
