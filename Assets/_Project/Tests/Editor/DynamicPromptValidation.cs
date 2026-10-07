using System;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Player;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;

namespace Tikal.Tests.Editor
{
    public static class DynamicPromptValidation
    {
        private static readonly MethodInfo InteractionTick=typeof(PlayerInteraction).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
        private static readonly MethodInfo PresenterTick=typeof(InteractionPromptPresenter).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
        private static readonly MethodInfo DoorTick=typeof(Door).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic);
        [MenuItem("Tikal/Tests/Dynamic prompts and all regressions")]
        public static void Run()=>InteractionValidation.Run();

        internal static void ValidateScene(Component[] components)
        {
            var manager=components.OfType<InputDeviceManager>().Single();
            var presenter=components.OfType<InteractionPromptPresenter>().Single();
            var settings=new SerializedObject(presenter);
            Require(settings.FindProperty("_deviceManager").objectReferenceValue==manager,"Exactly one manager is wired to UI.");
            Require(settings.FindProperty("_interaction").objectReferenceValue==components.OfType<PlayerInteraction>().Single(),"UI uses interaction authority.");
            Require(settings.FindProperty("_inputReader").objectReferenceValue==components.OfType<PlayerInputReader>().Single(),"UI uses live input action source.");
            Require(settings.FindProperty("_promptRoot").objectReferenceValue!=null,"Visual container assigned.");
            var label=(TMP_Text)settings.FindProperty("_label").objectReferenceValue;
            Require(label!=null && label.font!=null && label.font.material.shader!=null,"TMP font, material and shader references valid.");
            Require(label.font.HasCharacter('□') && label.font.HasCharacter('E') && label.font.HasCharacter('X'),"All current Interact control glyphs available.");
            Require(components.OfType<Canvas>().Count()==1 && !label.raycastTarget && !label.richText,"One non-blocking contextual Canvas.");
            Require(PrefabUtility.GetCorrespondingObjectFromSource(presenter.gameObject)!=null,"Visual UI is a prefab instance.");
        }

        internal static void ValidateCurrentPrompts()
        {
            var interaction=UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
            var manager=interaction.GetComponent<InputDeviceManager>();
            var reader=interaction.GetComponent<PlayerInputReader>();
            var coordinator=interaction.GetComponent<PlayerController>();
            var camera=interaction.GetComponentInChildren<Camera>();
            var presenter=UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();
            var settings=new SerializedObject(presenter);
            var panel=(GameObject)settings.FindProperty("_promptRoot").objectReferenceValue;
            var label=(TMP_Text)settings.FindProperty("_label").objectReferenceValue;
            var door=UnityEngine.Object.FindAnyObjectByType<Door>();
            coordinator.enabled=false; reader.EnableGameplay();
            var actions=(TikalInputActions)typeof(PlayerInputReader).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(reader);
            var inputSettings=InputSystem.settings;
            var oldBackground=inputSettings.backgroundBehavior; var oldEditor=inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>(); var mouse=InputSystem.AddDevice<Mouse>();
            var xbox=InputSystem.AddDevice<XInputController>(); var playStation=InputSystem.AddDevice<DualShockGamepad>(); var generic=InputSystem.AddDevice<Gamepad>();
            var oldDevices=actions.devices;
            actions.devices=new InputDevice[]{keyboard,mouse,xbox,playStation,generic};
            var changes=0; var focusChanges=0;
            Action<InputDeviceFamily> deviceObserver=_=>changes++;
            Action<IInteractable> focusObserver=_=>focusChanges++;
            manager.ActiveDeviceChanged+=deviceObserver; interaction.FocusChanged+=focusObserver;
            try
            {
                Pad(generic); Key(keyboard); Require(manager.ActiveDevice==InputDeviceFamily.KeyboardMouse,"Keyboard selects KeyboardMouse.");
                Pad(xbox); Require(manager.ActiveDevice==InputDeviceFamily.Xbox,"Real XInput layout classifies Xbox.");
                var before=changes; Pad(xbox); Require(changes==before,"Repeated same-family activity does not notify.");
                InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(0.05f,0)}); InputSystem.Update();
                Require(manager.ActiveDevice==InputDeviceFamily.Xbox,"Mouse noise ignored.");
                InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(3,0)}); InputSystem.Update();
                Require(manager.ActiveDevice==InputDeviceFamily.KeyboardMouse,"Significant mouse motion selects KeyboardMouse.");
                before=changes; Pad(xbox); Require(changes==before+1,"Meaningful family transition notifies exactly once.");
                Pad(playStation); Require(manager.ActiveDevice==InputDeviceFamily.PlayStation,"Real DualShock layout classifies PlayStation.");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(600,400)}); InputSystem.Update();
                Require(manager.ActiveDevice==InputDeviceFamily.PlayStation,"Mouse position/warp is not activity.");
                Pad(generic); Require(manager.ActiveDevice==InputDeviceFamily.GenericGamepad,"Unknown Gamepad has neutral family.");
                Key(keyboard); before=changes;
                InputSystem.QueueStateEvent(xbox,new GamepadState{leftStick=new Vector2(0.05f,0.05f),leftTrigger=0.05f}); InputSystem.Update();
                Require(manager.ActiveDevice==InputDeviceFamily.KeyboardMouse && changes==before,"Stick drift and trigger noise do not switch family.");
                InputSystem.QueueStateEvent(xbox,new GamepadState{leftStick=new Vector2(0.65f,0)}); InputSystem.Update();
                Require(manager.ActiveDevice==InputDeviceFamily.Xbox,"Meaningful stick motion detected.");
                Key(keyboard);
                InputSystem.QueueStateEvent(xbox,new GamepadState{leftTrigger=0.6f}); InputSystem.Update();
                Require(manager.ActiveDevice==InputDeviceFamily.Xbox,"Significant trigger detected.");
                manager.enabled=false; before=changes; Key(keyboard);
                Require(changes==before && manager.ActiveDevice==InputDeviceFamily.Xbox,"Disabled manager unsubscribes.");
                for(var index=0;index<4;index++)
                {
                    manager.enabled=true; before=changes; Key(keyboard);
                    Require(changes==before+1,"Re-enable does not duplicate callbacks.");
                    Pad(xbox); manager.enabled=false;
                }
                manager.enabled=true;
                Pad(playStation); InputSystem.RemoveDevice(playStation);
                Require(manager.ActiveDevice==InputDeviceFamily.KeyboardMouse,"Disconnect uses keyboard/mouse fallback.");
                playStation=InputSystem.AddDevice<DualShockGamepad>();
                actions.devices=new InputDevice[]{keyboard,mouse,xbox,playStation,generic};

                var interact=actions.Player.Interact;
                var secret=actions.Player.SecretCommand;
                Prompt(interact,InputDeviceFamily.KeyboardMouse,"E"); Prompt(interact,InputDeviceFamily.Xbox,"X"); Prompt(interact,InputDeviceFamily.PlayStation,"□");
                Prompt(interact,InputDeviceFamily.GenericGamepad,"Botón oeste");
                Prompt(secret,InputDeviceFamily.KeyboardMouse,"G"); Prompt(secret,InputDeviceFamily.Xbox,"Y"); Prompt(secret,InputDeviceFamily.PlayStation,"△");
                Prompt(secret,InputDeviceFamily.GenericGamepad,"Botón norte");
                foreach (var family in new[] { InputDeviceFamily.KeyboardMouse, InputDeviceFamily.Xbox, InputDeviceFamily.PlayStation, InputDeviceFamily.GenericGamepad })
                {
                    Require(InputPromptResolver.Resolve(interact, family).ControlPath ==
                        (family == InputDeviceFamily.KeyboardMouse ? "<Keyboard>/e" : "<Gamepad>/buttonWest"), "Interact effective binding: " + family);
                    Require(InputPromptResolver.Resolve(secret, family).ControlPath ==
                        (family == InputDeviceFamily.KeyboardMouse ? "<Keyboard>/g" : "<Gamepad>/buttonNorth"), "SecretCommand effective binding: " + family);
                }
                var binding=interact.GetBindingIndex(InputBinding.MaskByGroup("KeyboardMouse"));
                interact.ApplyBindingOverride(binding,"<Keyboard>/f");
                Require(InputPromptResolver.Resolve(interact,InputDeviceFamily.KeyboardMouse).DisplayText=="F" && InputPromptResolver.Resolve(interact,InputDeviceFamily.KeyboardMouse).ControlPath=="<Keyboard>/f","Resolver respects effective binding overrides.");
                interact.RemoveBindingOverride(binding);

                View(camera,Vector3.back); Refresh(interaction,presenter);
                Require(!panel.activeSelf,"No focus hides prompt.");
                Key(keyboard); View(camera,Vector3.forward); Refresh(interaction,presenter);
                Require(panel.activeSelf && label.text=="[ E ]  Abrir","Door focus presents keyboard prompt.");
                var focused=interaction.CurrentInteractable; before=focusChanges;
                Pad(xbox); Require(label.text=="[ X ]  Abrir" && ReferenceEquals(focused,interaction.CurrentInteractable) && focusChanges==before,"Xbox prompt updates instantly without focus change.");
                Pad(playStation); Require(label.text=="[ □ ]  Abrir" && focusChanges==before,"PlayStation prompt updates without focus change.");
                InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(4,0)}); InputSystem.Update();
                Require(label.text=="[ E ]  Abrir" && focusChanges==before,"Mouse restores keyboard prompt without focus change.");
                Pad(generic); Require(label.text=="[ Botón oeste ]  Abrir","Generic prompt is neutral.");
                Key(keyboard);
                interact.ApplyBindingOverride(binding,"<Keyboard>/f"); InputSystem.Update(); PresenterTick.Invoke(presenter,null);
                Require(label.text=="[ F ]  Abrir","Presenter receives live rebinding changes.");
                interact.RemoveBindingOverride(binding); InputSystem.Update(); PresenterTick.Invoke(presenter,null);
                Require(label.text=="[ E ]  Abrir","Restored binding updates UI.");
                // Keep focus on the door frame while the leaf rotates out of the central ray.
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.E)); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update(); Refresh(interaction,presenter);
                Animate(door); Refresh(interaction,presenter);
                Require(door.State==Door.DoorState.Open && panel.activeSelf && label.text=="[ E ]  Cerrar","E opens Door and state updates contextual UI.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.G)); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update(); Refresh(interaction,presenter);
                Require(door.State==Door.DoorState.Open && label.text=="[ E ]  Cerrar","SecretCommand never appears in contextual UI or activates Door.");
                door.SetInteractionAvailable(false); PresenterTick.Invoke(presenter,null);
                Require(!panel.activeSelf,"Unavailable interactable hides active prompt.");
                door.SetInteractionAvailable(true); Refresh(interaction,presenter);
                presenter.enabled=false; Require(!panel.activeSelf,"Presenter disable hides prompt.");
                presenter.enabled=true; Require(panel.activeSelf && label.text=="[ E ]  Cerrar","Presenter re-enable restores correct prompt.");
                View(camera,Vector3.back); Refresh(interaction,presenter); Require(!panel.activeSelf,"Looking away hides prompt.");
                camera.transform.position=new Vector3(-0.75f,1.3f,-4); camera.transform.rotation=Quaternion.identity; Refresh(interaction,presenter);
                Require(!panel.activeSelf,"Leaving range hides prompt.");
                Debug.Log("TIKAL_DYNAMIC_PROMPTS_PASSED: all device, resolver, presenter and rebinding checks; keyboard/XInput/DualShock/generic simulation.");
            }
            finally
            {
                actions.Player.Interact.RemoveAllBindingOverrides();
                door.SetInteractionAvailable(true);
                if(door.InteractionText=="Cerrar") door.Interact(); Animate(door);
                manager.enabled=true;
                manager.ActiveDeviceChanged-=deviceObserver; interaction.FocusChanged-=focusObserver;
                actions.devices=oldDevices;
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(xbox); InputSystem.RemoveDevice(playStation); InputSystem.RemoveDevice(generic);
                inputSettings.backgroundBehavior=oldBackground; inputSettings.editorInputBehaviorInPlayMode=oldEditor;
            }
        }
        private static void Key(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.H)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
        }
        private static void Pad(Gamepad gamepad)
        {
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.East)); InputSystem.Update();
            InputSystem.QueueStateEvent(gamepad,new GamepadState()); InputSystem.Update();
        }
        private static void Prompt(InputAction action,InputDeviceFamily family,string expected)=>Require(InputPromptResolver.Resolve(action,family).DisplayText==expected,"Resolver: "+action.name+" / "+family+" = "+expected);
        private static void View(Camera camera,Vector3 direction)
        {
            camera.transform.position=new Vector3(-0.75f,1.3f,0.4f); camera.transform.rotation=Quaternion.LookRotation(direction);
        }
        private static void Refresh(PlayerInteraction interaction,InteractionPromptPresenter presenter)
        {
            Physics.SyncTransforms(); InteractionTick.Invoke(interaction,null); PresenterTick.Invoke(presenter,null);
        }
        private static void Animate(Door door)
        {
            for(var index=0;index<100;index++) DoorTick.Invoke(door,new object[]{0.02f}); Physics.SyncTransforms();
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
