using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace FlowState.Menu.Tests
{
    public sealed class MainMenuPlayTests
    {
        const string Scene="Assets/02_Escenas/MainMenu_FlowState.unity";
        MainMenuController menu;
        Gamepad gamepad;
        Keyboard keyboard;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return new WaitForSecondsRealtime(.65f);
            menu=Object.FindFirstObjectByType<MainMenuController>();Assert.That(menu,Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(gamepad!=null)InputSystem.RemoveDevice(gamepad);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            gamepad=null;keyboard=null;
            if(menu)menu.Back();
            yield return null;
        }
        [UnityTest] public IEnumerator KeyboardAndGamepadNavigateTheProjectActionMap()
        {
            keyboard=InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.RightArrow));
            yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.That(menu.SelectedIndex,Is.EqualTo(1));Assert.That(menu.IsReady,Is.True);
            gamepad=InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.DpadLeft));
            yield return null;yield return null;
            InputSystem.QueueStateEvent(gamepad,new GamepadState());
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.That(menu.SelectedIndex,Is.Zero);Assert.That(menu.painter.StrokeCount,Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator SpamStaysBoundedAndFinishesCoherently()
        {
            for(int i=0;i<1000;i++)menu.Navigate(i%3==0?-1:1);
            Assert.That(menu.QueuedInputs,Is.LessThanOrEqualTo(4));
            yield return new WaitForSecondsRealtime(6.5f);
            Assert.That(menu.IsReady,Is.True);Assert.That(menu.QueuedInputs,Is.Zero);
            Assert.That(menu.painter.StrokeCount,Is.InRange(2,6));
            Assert.That(menu.motion.IsTurning,Is.False);
        }
        [UnityTest] public IEnumerator HundredsOfPaintingsKeepFixedTexturesAndObjects()
        {
            int objects=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;
            for(int i=0;i<300;i++)menu.painter.Begin(menu.options[i%4],i%4);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(menu.painter.StrokeCount,Is.EqualTo(301));
            Assert.That(menu.painter.TextureCount,Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length,Is.EqualTo(objects));
            var rts=Resources.FindObjectsOfTypeAll<RenderTexture>().Where(t=>t.name.StartsWith("Menu wall ")).ToArray();
            Assert.That(rts.Length,Is.EqualTo(2));
            Assert.That(rts.All(t=>t.width==2048&&t.height==1024),Is.True);
        }
        [UnityTest] public IEnumerator CancelClosesSubmenuAndReloadResetsWall()
        {
            menu.Navigate(1);yield return new WaitForSecondsRealtime(1.3f);menu.Confirm();
            Assert.That(menu.submenu.IsOpen,Is.True);menu.Back();Assert.That(menu.submenu.IsOpen,Is.False);
            yield return SceneManager.LoadSceneAsync(Scene);yield return new WaitForSecondsRealtime(.7f);
            menu=Object.FindFirstObjectByType<MainMenuController>();Assert.That(menu.SelectedIndex,Is.Zero);Assert.That(menu.painter.StrokeCount,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator PlayUsesPaintTransitionAndLoadsGameplay()
        {
            menu.Confirm();Assert.That(menu.transition.IsBusy,Is.True);
            yield return new WaitForSecondsRealtime(5);
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo("Assets/02_Escenas/Game.unity"));
            Assert.That(Object.FindFirstObjectByType<MenuTransition>(),Is.Null);
        }
    }
}
