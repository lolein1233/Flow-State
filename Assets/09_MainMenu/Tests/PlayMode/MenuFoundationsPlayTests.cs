using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FlowState.Menu.Tests
{
    public sealed class MenuFoundationsPlayTests
    {
        const string Scene="Assets/02_Escenas/MainMenu_FlowState.unity";
        MainMenuController menu;
        FlowStateIntroDirector director;
        FlowStateMenuCameraController camera;
        Gamepad gamepad;
        Mouse mouse;
        Keyboard keyboard;
        string originalBindings;
        int readyEvents,entryEvents;
        InputSettings previousInputSettings, testInputSettings;
        bool hadMotionPreference;
        float previousMotionPreference;
        readonly List<InputDevice> suspendedDevices=new List<InputDevice>();

        IEnumerator Load(IntroMode mode=IntroMode.Disabled,bool debugComplete=false)
        {
            void Configure(Scene loaded,LoadSceneMode loadMode)
            {
                if(loaded.path!=Scene)return;
                menu=Object.FindFirstObjectByType<MainMenuController>();director=menu.introDirector;
                camera=director.cameraController;
                director.mode=mode;director.debugCompleteOnStart=debugComplete;
                director.MenuReady+=()=>readyEvents++;
                menu.MenuEntered+=()=>entryEvents++;
            }
            SceneManager.sceneLoaded+=Configure;
            try {yield return SceneManager.LoadSceneAsync(Scene);}
            finally {SceneManager.sceneLoaded-=Configure;}
            yield return new WaitForSecondsRealtime(.8f);
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            // Synthetic pointer/keyboard events must reach the player even while MCP owns focus.
            previousInputSettings=InputSystem.settings;
            testInputSettings=Object.Instantiate(previousInputSettings);
            testInputSettings.hideFlags=HideFlags.HideAndDontSave;
            testInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            #if UNITY_EDITOR
            testInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            #endif
            InputSystem.settings=testInputSettings;
            hadMotionPreference=PlayerPrefs.HasKey("FS.Menu.Motion");
            previousMotionPreference=PlayerPrefs.GetFloat("FS.Menu.Motion",1);
            PlayerPrefs.SetFloat("FS.Menu.Motion",1);
            // A real mouse moving over the editor must not replace the synthetic test source.
            suspendedDevices.Clear();
            foreach(var device in InputSystem.devices)
                if(device.enabled&&(device is Mouse||device is Keyboard||device is Gamepad))
                    suspendedDevices.Add(device);
            foreach(var device in suspendedDevices)InputSystem.DisableDevice(device);
            readyEvents=entryEvents=0;
            yield return Load();
            originalBindings=menu.input.actions.ToJson();
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(gamepad!=null)InputSystem.RemoveDevice(gamepad);
            if(mouse!=null)InputSystem.RemoveDevice(mouse);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            gamepad=null;mouse=null;keyboard=null;
            bool bindingsUnchanged=!menu||menu.input.actions.ToJson()==originalBindings;
            if(menu)menu.Back();
            InputSystem.settings=previousInputSettings;
            if(testInputSettings)Object.Destroy(testInputSettings);
            foreach(var device in suspendedDevices)if(device.added)InputSystem.EnableDevice(device);
            suspendedDevices.Clear();
            if(hadMotionPreference)PlayerPrefs.SetFloat("FS.Menu.Motion",previousMotionPreference);
            else PlayerPrefs.DeleteKey("FS.Menu.Motion");
            PlayerPrefs.Save();
            yield return null;
            Assert.That(bindingsUnchanged,Is.True,"Shared input asset was modified.");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DisabledAndRepeatedCompletionPaintExactlyOnce()
        {
            Assert.That(director.Phase,Is.EqualTo(IntroPhase.MenuReady));
            Assert.That(menu.IsReady,Is.True);Assert.That(menu.painter.StrokeCount,Is.EqualTo(1));
            Assert.That(camera.ReactiveEnabled,Is.True);
            menu.EnterMenu();menu.EnterMenu();director.CompleteIntroSafely();director.SkipIntro();director.BeginIntro();
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(menu.painter.StrokeCount,Is.EqualTo(1));
            Assert.That(readyEvents,Is.EqualTo(1));Assert.That(entryEvents,Is.EqualTo(1));
            Assert.That(menu.transition.IsBusy,Is.False);
        }
        [UnityTest] public IEnumerator FullLocksAndSouthSkipDoesNotSubmitPlay()
        {
            readyEvents=entryEvents=0;yield return Load(IntroMode.Full);
            Assert.That(director.Phase,Is.EqualTo(IntroPhase.Painting));
            Assert.That(menu.IsLocked,Is.True);Assert.That(menu.painter.StrokeCount,Is.Zero);
            menu.Navigate(1);menu.Confirm();
            Assert.That(menu.SelectedIndex,Is.Zero);Assert.That(menu.QueuedInputs,Is.Zero);
            gamepad=InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South));
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(director.WasSkipped,Is.True);Assert.That(menu.IsReady,Is.True);
            Assert.That(menu.transition.IsBusy,Is.False);Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(Scene));
            InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return null;yield return null;
            director.CompleteIntroSafely();
            Assert.That(readyEvents,Is.EqualTo(1));Assert.That(entryEvents,Is.EqualTo(1));
            Assert.That(menu.painter.StrokeCount,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator ShortAndDebugCompletionUseCommonFinalState()
        {
            readyEvents=entryEvents=0;yield return Load(IntroMode.Short);
            Assert.That(director.Phase,Is.EqualTo(IntroPhase.LogoReveal));
            director.CompleteIntroSafely();director.CompleteIntroSafely();
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(menu.IsReady,Is.True);Assert.That(menu.painter.StrokeCount,Is.EqualTo(1));
            Assert.That(readyEvents,Is.EqualTo(1));
            readyEvents=entryEvents=0;yield return Load(IntroMode.Full,true);
            Assert.That(menu.IsReady,Is.True);Assert.That(readyEvents,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator AllKeyboardSkipBindingsAreConsumed()
        {
            keyboard=InputSystem.AddDevice<Keyboard>();
            foreach(Key key in new[]{Key.Escape,Key.Enter,Key.Space})
            {
                yield return Load(IntroMode.Full);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
                yield return new WaitForSecondsRealtime(.7f);
                Assert.That(director.WasSkipped,Is.True,key+" phase="+director.Phase+" nav="+menu.input.NavigationEnabled+" key="+keyboard[key].isPressed+" enabled="+keyboard.enabled+" focus="+Application.isFocused+" actions="+InputDiagnostics());
                Assert.That(menu.transition.IsBusy,Is.False);
                Assert.That(menu.painter.StrokeCount,Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
            }
        }
        [UnityTest] public IEnumerator RightStickMovesCameraWithoutNavigationAndReturns()
        {
            gamepad=InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad,new GamepadState{rightStick=new Vector2(.9f,.5f)});
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(menu.SelectedIndex,Is.Zero);Assert.That(menu.painter.StrokeCount,Is.EqualTo(1));
            Assert.That(camera.Response.x,Is.GreaterThan(.3f));
            Assert.That(camera.responseRig.localPosition.x,Is.GreaterThan(.015f));
            Assert.That(camera.responseRig.localPosition.x,Is.LessThanOrEqualTo(camera.maxHorizontal+.001f));
            Assert.That(Quaternion.Angle(camera.responseRig.localRotation,Quaternion.identity),Is.GreaterThan(.1f));
            InputSystem.QueueStateEvent(gamepad,new GamepadState());
            yield return new WaitForSecondsRealtime(2);
            Assert.That(camera.responseRig.localPosition.magnitude,Is.LessThan(.001f));
            Assert.That(camera.Response.magnitude,Is.LessThan(.01f));
            Assert.That(Vector3.Distance(camera.menuCamera.transform.position,camera.heroAnchor.position),Is.LessThan(.001f));
        }
        [UnityTest] public IEnumerator LegacyZeroMotionPreferenceBecomesReducedInsteadOfDisabled()
        {
            PlayerPrefs.SetFloat("FS.Menu.Motion",0);
            yield return Load();
            Assert.That(menu.cameraFeedback.motionScale,Is.EqualTo(MenuSubmenu.ReducedMotionScale).Within(.001f));
            Assert.That(menu.motion.motionScale,Is.EqualTo(MenuSubmenu.ReducedMotionScale).Within(.001f));
            Assert.That(PlayerPrefs.GetFloat("FS.Menu.Motion"),Is.EqualTo(MenuSubmenu.ReducedMotionScale).Within(.001f));
            gamepad=InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad,new GamepadState{rightStick=Vector2.right});
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(camera.Response.x,Is.GreaterThan(.25f));
            Assert.That(camera.responseRig.localPosition.x,Is.GreaterThan(.015f));
        }
        [UnityTest] public IEnumerator LeftStickAndWasdStillNavigate()
        {
            gamepad=InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=Vector2.right});
            yield return null;yield return null;
            InputSystem.QueueStateEvent(gamepad,new GamepadState());
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.That(menu.SelectedIndex,Is.EqualTo(1));Assert.That(menu.motion.IsTurning,Is.False);
            keyboard=InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.That(menu.SelectedIndex,Is.Zero);Assert.That(menu.IsReady,Is.True);
        }
        [UnityTest] public IEnumerator MousePositionPersistsUntilCentreAndMotionZeroStops()
        {
            mouse=InputSystem.AddDevice<Mouse>();
            Rect rect=camera.menuCamera.pixelRect;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=rect.center});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(rect.x+rect.width*.9f,rect.y+rect.height*.75f)});
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(camera.Response.x,Is.GreaterThan(.1f),"ambient="+menu.input.AmbientInput+" mouse="+mouse.position.ReadValue()+" enabled="+mouse.enabled+" rect="+rect+" focus="+Application.isFocused+" actions="+InputDiagnostics());Assert.That(camera.Response.y,Is.GreaterThan(.05f));
            yield return new WaitForSecondsRealtime(menu.input.mouseIdleDelay+.4f);
            Assert.That(camera.Response.x,Is.GreaterThan(.1f),"An idle gamepad or timer replaced the absolute mouse position.");
            Assert.That(menu.input.ActiveLookSource,Is.EqualTo(AmbientLookSource.MousePosition));
            camera.CameraMotionIntensity=0;yield return null;yield return null;
            Assert.That(camera.responseRig.localPosition,Is.EqualTo(Vector3.zero));
            Assert.That(camera.responseRig.localRotation,Is.EqualTo(Quaternion.identity));
            camera.CameraMotionIntensity=1;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=rect.center});
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(camera.Response.magnitude,Is.LessThan(.01f));Assert.That(menu.SelectedIndex,Is.Zero);
        }
        [UnityTest] public IEnumerator CanEventsProduceBoundedFollowThroughAndCanBeDisabled()
        {
            int started=0,finished=0;
            menu.motion.RotationStarted+=(direction,duration)=>{started++;Assert.That(direction,Is.EqualTo(1));Assert.That(duration,Is.GreaterThan(0));};
            menu.motion.RotationCompleted+=()=>finished++;
            menu.Navigate(1);yield return new WaitForSecondsRealtime(.25f);
            Assert.That(camera.CanFollowOffset,Is.GreaterThan(.01f));
            yield return new WaitForSecondsRealtime(1.8f);
            Assert.That(started,Is.EqualTo(1));Assert.That(finished,Is.EqualTo(1));
            Assert.That(Mathf.Abs(camera.CanFollowOffset),Is.LessThan(.005f));
            camera.canFollowEnabled=false;menu.Navigate(1);yield return new WaitForSecondsRealtime(.25f);
            Assert.That(camera.CanFollowOffset,Is.Zero);
            yield return new WaitForSecondsRealtime(1.3f);Assert.That(menu.IsReady,Is.True);
        }
        [UnityTest] public IEnumerator SettingsStillOpenAndParallaxUsesRealDistances()
        {
            menu.Navigate(1);menu.Navigate(1);yield return new WaitForSecondsRealtime(2.7f);
            menu.Confirm();Assert.That(menu.submenu.IsOpen,Is.True);menu.Back();Assert.That(menu.submenu.IsOpen,Is.False);
            camera.ParallaxIntensity=0;
            gamepad=InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad,new GamepadState{rightStick=Vector2.right});yield return new WaitForSecondsRealtime(.6f);
            Assert.That(camera.responseRig.localPosition,Is.EqualTo(Vector3.zero));
            // World points share a ray but have different depths. Translation must separate them.
            camera.yaw=camera.pitch=camera.roll=0;camera.ResetToHero();
            Vector3 near=camera.heroAnchor.position+camera.heroAnchor.forward*3;
            Vector3 far=camera.heroAnchor.position+camera.heroAnchor.forward*12;
            Vector3 nearBefore=camera.menuCamera.WorldToViewportPoint(near),farBefore=camera.menuCamera.WorldToViewportPoint(far);
            camera.ParallaxIntensity=1;yield return new WaitForSecondsRealtime(.6f);
            float nearShift=Mathf.Abs(camera.menuCamera.WorldToViewportPoint(near).x-nearBefore.x);
            float farShift=Mathf.Abs(camera.menuCamera.WorldToViewportPoint(far).x-farBefore.x);
            Assert.That(nearShift,Is.GreaterThan(farShift*3.5f),
                "near="+nearShift+" far="+farShift+" ambient="+menu.input.AmbientInput+" response="+camera.Response+
                " rig="+camera.responseRig.localPosition+" reactive="+camera.ReactiveEnabled+" source="+menu.input.ActiveLookSource);
        }
        string InputDiagnostics()
        {
            string result="";
            foreach(var action in InputSystem.ListEnabledActions())
                if(action.name=="MenuSkipIntro"||action.name=="MenuPointerPosition")
                {
                    result+=action.name+"="+action.ReadValueAsObject()+" controls:";
                    foreach(var control in action.controls)result+=control.path+",";
                }
            return result;
        }
    }
}
