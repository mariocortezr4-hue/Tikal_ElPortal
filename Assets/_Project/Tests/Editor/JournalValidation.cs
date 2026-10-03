using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Player;
using Tikal.Puzzles;
using Tikal.Puzzles.Clues;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.UI;
namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class JournalValidation
    {
        const string Active="Tikal.Journal.Active",Result="Tikal.Journal.Result",Count="Tikal.Journal.Count";
        static int checks,journalEvents;
        static double deadline;
        static ClueJournalPresenter journal;
        static ClueTracker tracker;
        static PlayerController player;
        static PlayerInputReader reader;
        static PlayerInteraction interaction;
        static InteractionPromptPresenter prompt;
        static GameObject promptPanel,discoveryPanel;
        static TMP_Text promptText,body,hint;
        static Keyboard keyboard;
        static XInputController xbox;
        static DualShockGamepad sony;
        static TikalInputActions actions;
        static UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>? oldDevices;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        static MethodInfo Method(Type t,string name)=>t.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance);
        static void Require(bool value,string message){if(!value)throw new Exception("Journal: "+message);checks++;Debug.Log("JOURNAL_CHECK: "+message);}
        static JournalValidation(){EditorApplication.playModeStateChanged+=State;}
        static ClueDefinition[] Definitions()=>new[]{"mural_fragments","path_guardians","echo_chamber"}.Select(n=>AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01."+n+".asset")).ToArray();
        public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                checks=0;ValidateTracker();
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                Require(all.All(c=>c!=null),"No Missing Scripts");DynamicPromptValidation.ValidateScene(all);
                var presenter=all.OfType<ClueJournalPresenter>().Single();var js=new SerializedObject(presenter);
                foreach(string field in new[]{"_tracker","_inputReader","_player","_devices","_panel","_content","_closeHint"})Require(js.FindProperty(field).objectReferenceValue!=null,"Journal reference "+field);
                Require(PrefabUtility.GetCorrespondingObjectFromSource(presenter.gameObject)!=null,"Independent journal prefab reused");
                Require(all.OfType<Canvas>().Count()==1,"Existing Canvas only");
                var asset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/TikalInputActions.inputactions");var action=asset.FindAction("Global/Journal",true);
                Require(action.type==InputActionType.Button&&action.bindings.Count==2&&action.bindings[0].path=="<Keyboard>/j"&&action.bindings[1].path=="<Gamepad>/select","Native semantic Journal bindings J and select");
                Require(asset.FindAction("Player/Interact").bindings.Any(b=>b.path=="<Keyboard>/e")&&asset.FindAction("Player/SecretCommand").bindings.Any(b=>b.path=="<Keyboard>/g"),"Existing Interact and SecretCommand preserved");
                var ps=new SerializedObject(all.OfType<InteractionPromptPresenter>().Single());var rect=(RectTransform)((GameObject)ps.FindProperty("_promptRoot").objectReferenceValue).transform;
                Require(rect.anchorMin==Vector2.zero&&rect.anchorMax==Vector2.zero&&rect.pivot==Vector2.zero&&rect.anchoredPosition==new Vector2(50,50),"Interaction prompt permanently bottom left at 50 reference pixels");
                var ds=new SerializedObject(all.OfType<ClueDiscoveryPresenter>().Single());var discovery=(RectTransform)((GameObject)ds.FindProperty("_panel").objectReferenceValue).transform;
                Require(discovery.anchorMin==new Vector2(.5f,1)&&discovery.anchorMax==new Vector2(.5f,1)&&discovery.anchoredPosition==new Vector2(0,-50),"Discovery top centre away from centre of vision");
                ValidateResponsive(rect,discovery,all.OfType<CanvasScaler>().Single());
                SessionState.SetInt(Count,checks);SessionState.SetBool(Active,true);SessionState.SetInt(Result,1);EditorApplication.EnterPlaymode();
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void ValidateTracker()
        {
            var go=new GameObject("Temporary tracker validation");var t=go.AddComponent<ClueTracker>();var other=new GameObject("Temporary restored tracker");var restored=other.AddComponent<ClueTracker>();var clues=Definitions();var tieA=ScriptableObject.CreateInstance<ClueDefinition>();var tieB=ScriptableObject.CreateInstance<ClueDefinition>();
            try
            {
                int found=0,loads=0;t.ClueDiscovered+=_=>found++;restored.ProgressRestored+=()=>loads++;int restoredDiscoveries=0;restored.ClueDiscovered+=_=>restoredDiscoveries++;
                for(int i=0;i<3;i++){Require(t.Discover(clues[i])&&t.Count==i+1,"Clue accumulation retains previous discoveries "+i);for(int n=0;n<=i;n++)Require(t.HasClue(clues[n].Id),"Retained clue "+n);}
                Require(!t.Discover(clues[1])&&t.Count==3&&found==3,"Duplicate discovery emits no event");
                Require(t.TryGetClue(clues[0].Id,out var first)&&first==clues[0]&&!t.TryGetClue("missing",out _),"Safe lookup by stable ID");
                Require(t.DiscoveredClues.SequenceEqual(clues),"Discovery order preserved");
                bool immutable=false;try{((IList<ClueDefinition>)t.DiscoveredClues).Clear();}catch(NotSupportedException){immutable=true;}Require(immutable&&t.Count==3,"Collection cannot be mutated by consumers");
                var snapshot=t.CaptureSnapshot();Require(snapshot.DiscoveredClueIds.SequenceEqual(clues.Select(c=>c.Id)),"Snapshot contains IDs in discovery order");
                var ids=new[]{clues[0].Id};var isolated=new ClueProgressSnapshot(ids);ids[0]="changed";Require(isolated.DiscoveredClueIds[0]==clues[0].Id,"Snapshot owns copied IDs");
                immutable=false;try{((IList<string>)snapshot.DiscoveredClueIds).Clear();}catch(NotSupportedException){immutable=true;}Require(immutable,"Snapshot ID view is immutable");
                string json=JsonUtility.ToJson(snapshot);var roundtrip=JsonUtility.FromJson<ClueProgressSnapshot>(json);Require(roundtrip.DiscoveredClueIds.SequenceEqual(snapshot.DiscoveredClueIds)&&!json.Contains(clues[0].Title),"Serializable ID-only in-memory save contract");
                Require(restored.TryRestoreSnapshot(roundtrip,clues,out _)&&restored.Count==3&&loads==1&&restoredDiscoveries==0,"Restore recovers exact progress without new discovery notifications");
                Require(restored.DiscoveredClues.SequenceEqual(clues),"Restored discovery order intact");
                Require(!restored.TryRestoreSnapshot(new ClueProgressSnapshot(new[]{"unknown.id"}),clues,out var error)&&error!=null&&restored.Count==3&&loads==1,"Unknown ID rejects restore atomically");
                Require(restored.TryRestoreSnapshot(new ClueProgressSnapshot(new[]{clues[0].Id,clues[0].Id}),clues,out _)&&restored.Count==1,"Duplicate snapshot IDs are deduplicated");
                Require(restored.TryRestoreSnapshot(new ClueProgressSnapshot(),clues,out _)&&restored.Count==0&&snapshot.DiscoveredClueIds.Count==3,"Empty snapshot replaces state without changing older snapshots");
                SetClue(tieA,"test.tieA");SetClue(tieB,"test.tieB");var catalogue=clues.Concat(new[]{tieA,tieB}).ToArray();
                Require(restored.TryRestoreSnapshot(new ClueProgressSnapshot(new[]{clues[2].Id,tieB.Id,tieA.Id,clues[0].Id,clues[1].Id}),catalogue,out _),"Out-of-order fixture restored");
                var ordered=restored.DiscoveredClues.OrderBy(c=>c.Order).ToArray();Require(ordered.SequenceEqual(new[]{clues[0],tieB,tieA,clues[1],clues[2]}),"Logical order stable with discovery order as deterministic tie-break");
                Require(!restored.TryRestoreSnapshot(snapshot,clues.Concat(new[]{clues[0]}),out _)&&restored.Count==5,"Ambiguous catalogue rejects restore without loss");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(other);UnityEngine.Object.DestroyImmediate(tieA);UnityEngine.Object.DestroyImmediate(tieB);}
        }
        static void SetClue(ClueDefinition clue,string id){var s=new SerializedObject(clue);s.FindProperty("_id").stringValue=id;s.FindProperty("_order").intValue=2;s.ApplyModifiedPropertiesWithoutUndo();}
        static Rect Bounds(RectTransform r){var c=new Vector3[4];r.GetWorldCorners(c);return new Rect(c[0].x,c[0].y,c[2].x-c[0].x,c[2].y-c[0].y);}
        static void ValidateResponsive(RectTransform prompt,RectTransform discovery,CanvasScaler scaler)
        {
            Require(scaler.uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize,"Responsive Canvas Scaler retained");
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1280,720),new Vector2(1280,960),new Vector2(2560,1080),new Vector2(3840,1080)})
            {
                var g=new GameObject("Temporary responsive canvas",typeof(RectTransform),typeof(Canvas));g.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
                float scale=Mathf.Pow(size.x/scaler.referenceResolution.x,1-scaler.matchWidthOrHeight)*Mathf.Pow(size.y/scaler.referenceResolution.y,scaler.matchWidthOrHeight);var parent=(RectTransform)g.transform;parent.sizeDelta=size/scale;
                var a=UnityEngine.Object.Instantiate(prompt.gameObject,parent,false);var b=UnityEngine.Object.Instantiate(discovery.gameObject,parent,false);
                try{Canvas.ForceUpdateCanvases();Require(!Bounds((RectTransform)a.transform).Overlaps(Bounds((RectTransform)b.transform)),"No notification overlap at "+size);}
                finally{UnityEngine.Object.DestroyImmediate(g);}
            }
        }
        static void State(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Active,false))return;
            if(change==PlayModeStateChange.EnteredPlayMode)
            {
                try{checks=SessionState.GetInt(Count,0);ValidateRuntime();deadline=EditorApplication.timeSinceStartup+6.6;EditorApplication.update+=Tick;}
                catch(Exception e){Fail(e);}
            }
            else if(change==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;SessionState.SetBool(Active,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Result,1));}
        }
        static void Refresh(){Physics.SyncTransforms();Method(typeof(PlayerInteraction),"LateUpdate").Invoke(interaction,null);Method(typeof(InteractionPromptPresenter),"LateUpdate").Invoke(prompt,null);}
        static void Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();Refresh();}
        static void Pad(Gamepad pad,GamepadButton button){InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));InputSystem.Update();InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();Refresh();}
        static void ObserveJournal()=>journalEvents++;
        static void ValidateRuntime()
        {
            journal=UnityEngine.Object.FindAnyObjectByType<ClueJournalPresenter>();tracker=UnityEngine.Object.FindAnyObjectByType<ClueTracker>();player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();reader=player.GetComponent<PlayerInputReader>();interaction=player.GetComponent<PlayerInteraction>();prompt=UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();
            var js=new SerializedObject(journal);body=(TMP_Text)js.FindProperty("_content").objectReferenceValue;hint=(TMP_Text)js.FindProperty("_closeHint").objectReferenceValue;var ps=new SerializedObject(prompt);promptPanel=(GameObject)ps.FindProperty("_promptRoot").objectReferenceValue;promptText=(TMP_Text)ps.FindProperty("_label").objectReferenceValue;discoveryPanel=(GameObject)new SerializedObject(UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>()).FindProperty("_panel").objectReferenceValue;
            oldBackground=InputSystem.settings.backgroundBehavior;oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();xbox=InputSystem.AddDevice<XInputController>();sony=InputSystem.AddDevice<DualShockGamepad>();actions=(TikalInputActions)typeof(PlayerInputReader).GetField("_actions",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(reader);oldDevices=actions.devices;actions.devices=new InputDevice[]{keyboard,xbox,sony};reader.JournalPressed+=ObserveJournal;journalEvents=0;
            var view=player.GetComponentInChildren<Camera>();view.transform.SetPositionAndRotation(new Vector3(0,1.62f,-2),Quaternion.identity);Press(Key.H);
            Require(!journal.IsOpen&&player.GameplayActive&&reader.GameplayEnabled,"Initially closed with gameplay active");Require(promptPanel.activeSelf&&promptText.text=="[ E ]  Abrir","World prompt visible at lower left");
            var initialCursor=Cursor.lockState;bool initialVisible=Cursor.visible;float scale=Time.timeScale;var position=player.transform.position;var rotation=view.transform.rotation;var door=UnityEngine.Object.FindAnyObjectByType<Door>();var doorState=door.State;
            Press(Key.J);Require(journal.IsOpen&&journalEvents==1&&hint.text.Contains("J"),"Native keyboard Journal opens and resolves close hint");Require(body.text=="Todavía no has descubierto pistas."&&!body.text.Contains("Clue04")&&!body.text.Contains("????"),"Empty journal shows no spoilers or total count");
            Require(!player.GameplayActive&&!reader.GameplayEnabled&&actions.Global.enabled&&actions.Global.Journal.enabled,"Player gated while Global Journal remains active");Require(!promptPanel.activeSelf&&interaction.CurrentInteractable==null,"Journal hides world interaction immediately");Require(Cursor.lockState==CursorLockMode.None&&Cursor.visible&&Time.timeScale==scale,"Cursor released without timeScale changes");
            Press(Key.W);Press(Key.E);Press(Key.G);Method(typeof(PlayerController),"Update").Invoke(player,null);
            Require(reader.Move==Vector2.zero&&reader.Look==Vector2.zero&&player.transform.position==position&&view.transform.rotation==rotation&&door.State==doorState,"Journal blocks movement look world interaction and secret input");
            Press(Key.J);Require(!journal.IsOpen&&player.GameplayActive&&reader.GameplayEnabled&&journalEvents==2,"Native J closes and restores gameplay");Require(Cursor.lockState==initialCursor&&Cursor.visible==initialVisible,"Previous cursor state restored");
            var clues=Definitions();Require(tracker.Discover(clues[0]),"Clue01 discovered");Canvas.ForceUpdateCanvases();Require(promptPanel.activeSelf&&discoveryPanel.activeSelf&&!Bounds((RectTransform)promptPanel.transform).Overlaps(Bounds((RectTransform)discoveryPanel.transform)),"World prompt and discovery coexist without overlap");
            Press(Key.J);Require(body.text.Contains(clues[0].Description)&&!body.text.Contains(clues[1].Title.ToUpperInvariant())&&!body.text.Contains(clues[2].Description),"Journal initially contains only Clue01");
            Require(tracker.Discover(clues[1])&&body.text.Contains(clues[1].Description)&&body.text.IndexOf(clues[0].Title.ToUpperInvariant(),StringComparison.Ordinal)<body.text.IndexOf(clues[1].Title.ToUpperInvariant(),StringComparison.Ordinal),"Clue02 appears live with Clue01 retained and ordered");
            string before=body.text;Require(!tracker.Discover(clues[1])&&body.text==before&&tracker.Count==2,"Duplicate reward cannot duplicate displayed entry");
            Pad(xbox,GamepadButton.Select);Require(!journal.IsOpen&&reader.GameplayEnabled,"Native Xbox select closes journal");Pad(xbox,GamepadButton.Select);Require(journal.IsOpen&&hint.text.Contains("View"),"Native Xbox select opens journal with View hint");
            Require(tracker.Discover(clues[2])&&body.text.Contains(clues[2].Description)&&tracker.Count==3,"Clue03 appears live retaining all three");Require(body.text.IndexOf(clues[1].Title.ToUpperInvariant(),StringComparison.Ordinal)<body.text.IndexOf(clues[2].Title.ToUpperInvariant(),StringComparison.Ordinal)&&!body.text.Contains("PLACEHOLDER"),"All discovered entries in logical order without future placeholders");
            Canvas.ForceUpdateCanvases();body.ForceMeshUpdate();Require(!body.isTextOverflowing&&!body.richText,"Current three clues fit and cannot inject rich text");
            Pad(sony,GamepadButton.Select);Require(!journal.IsOpen,"Native PlayStation Share closes journal");Pad(sony,GamepadButton.Select);Require(journal.IsOpen&&hint.text.Contains("Share"),"Native PlayStation Share opens journal");
            var saved=tracker.CaptureSnapshot();Require(tracker.TryRestoreSnapshot(new ClueProgressSnapshot(new[]{clues[0].Id}),clues,out _)&&!body.text.Contains(clues[2].Description),"Snapshot replacement updates open journal");Require(tracker.TryRestoreSnapshot(saved,clues,out _)&&body.text.Contains(clues[2].Description),"Snapshot restoration recovers journal content");
            Press(Key.J);for(int i=0;i<3;i++){journal.enabled=false;int events=journalEvents;Press(Key.J);Require(!journal.IsOpen&&journalEvents==events+1,"Disabled presenter unsubscribed");journal.enabled=true;Press(Key.J);Require(journal.IsOpen,"Re-enabled presenter opens exactly once");Press(Key.J);Require(!journal.IsOpen&&reader.GameplayEnabled,"Re-enabled presenter closes exactly once");}
            Press(Key.J);journal.enabled=false;Require(!journal.IsOpen&&reader.GameplayEnabled,"Disabling open journal safely restores gameplay");journal.enabled=true;
            for(int i=0;i<3;i++){reader.enabled=false;reader.enabled=true;int beforeEvents=journalEvents;Press(Key.J);Require(journal.IsOpen&&journalEvents==beforeEvents+1,"Reader enable-disable does not duplicate semantic listeners");Press(Key.J);Require(!journal.IsOpen,"Reader re-enabled journal closes");}
            foreach(var name in new[]{"ClueDiscovered","ProgressRestored"}){var listeners=(Delegate)typeof(ClueTracker).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(tracker);Require(listeners.GetInvocationList().Count(d=>ReferenceEquals(d.Target,journal))==1,"Exactly one journal tracker subscription after lifecycle cycles "+name);}
            reader.DisableGameplay();Press(Key.J);Press(Key.J);Require(player.GameplayActive&&!reader.GameplayEnabled,"Closing preserves independently disabled Player map");reader.EnableGameplay();
            player.SetGameplayActive(false);Press(Key.J);Require(journal.IsOpen,"Journal can open with gameplay already disabled");Press(Key.J);Require(!journal.IsOpen&&!reader.GameplayEnabled&&!player.GameplayActive,"Closing preserves previously disabled gameplay");player.SetGameplayActive(true);
            Pad(xbox,GamepadButton.East);Require(promptText.text=="[ X ]  Abrir","Device change updates existing Xbox interaction prompt");Pad(sony,GamepadButton.East);Require(promptText.text=="[ □ ]  Abrir","Existing PlayStation interaction prompt preserved");Press(Key.H);Require(promptText.text=="[ E ]  Abrir","Existing keyboard interaction prompt preserved");
            foreach(var p in UnityEngine.Object.FindObjectsByType<PuzzleBase>())Require(p.State==PuzzleState.Available,"Journal and SecretCommand do not alter puzzle state "+p.PuzzleId);
        }
        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.timeSinceStartup<deadline)return;
            try{Refresh();Require(!discoveryPanel.activeSelf&&promptPanel.activeSelf&&tracker.Count==3,"Discovery disappears but world prompt and registered clues remain");Press(Key.J);Require(journal.IsOpen&&Definitions().All(c=>body.text.Contains(c.Description)),"Journal still consults all clues after notification expiry");Press(Key.J);Require(reader.GameplayEnabled,"Final gameplay restored");Debug.Log("TIKAL_JOURNAL_VALIDATION_PASSED checks="+checks);SessionState.SetInt(Result,0);DisposeInput();EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
            catch(Exception e){Fail(e);}
        }
        static void DisposeInput(){if(reader!=null)reader.JournalPressed-=ObserveJournal;if(actions!=null)actions.devices=oldDevices;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(xbox!=null)InputSystem.RemoveDevice(xbox);if(sony!=null)InputSystem.RemoveDevice(sony);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;keyboard=null;xbox=null;sony=null;actions=null;}
        static void Fail(Exception e){Debug.LogException(e);DisposeInput();EditorApplication.update-=Tick;SessionState.SetInt(Result,1);EditorApplication.ExitPlaymode();}
    }
}
