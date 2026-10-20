using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FlowState.Menu.Tests
{
    public sealed class MainMenuBackdropPlayTests
    {
        const string Scene = "Assets/02_Escenas/MainMenu_FlowState.unity";

        [UnityTest]
        public IEnumerator HolographicMaskUsesProjectArtWithoutReplacingMenuAuthorities()
        {
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return new WaitForSecondsRealtime(0.7f);

            MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>();
            MenuBackdropMotion backdrop = Object.FindFirstObjectByType<MenuBackdropMotion>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(backdrop, Is.Not.Null);
            Assert.That(backdrop.wall, Is.SameAs(menu.painter.wall));
            Assert.That(backdrop.canMotion, Is.SameAs(menu.motion));
            Assert.That(backdrop.GetComponentsInChildren<Animator>(true), Is.Empty,
                "The simplified backdrop must not contain a physical Maikol character.");

            Material wall = menu.painter.wall.sharedMaterial;
            Assert.That(wall.GetTexture("_BackdropMask"), Is.Not.Null);
            Assert.That(wall.GetTexture("_BackdropLogo"), Is.Not.Null);
            Assert.That(wall.HasProperty("_BackdropEcho"), Is.False,
                "The removed Maikol image must no longer be part of the wall shader.");
            Assert.That(wall.HasProperty("_BackdropHoloIntensity"), Is.True);

            menu.Navigate(1);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(backdrop.CanImpulse, Is.GreaterThan(0f), "Can rotation must excite the backdrop animation.");
            Assert.That(menu.motion.IsTurning, Is.True, "Backdrop integration must not replace can rotation.");
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(menu.painter.IsPainting, Is.True, "Backdrop integration must not stop the spray painter after the can turn.");
        }

        [UnityTest]
        public IEnumerator PlayTransitionStillReachesGameplayWithLivingBackdrop()
        {
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return new WaitForSecondsRealtime(0.7f);

            MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.That(menu, Is.Not.Null);
            menu.Confirm();
            Assert.That(menu.transition.IsBusy, Is.True);

            float timeout = Time.realtimeSinceStartup + 15f;
            while (SceneManager.GetActiveScene().path != "Assets/02_Escenas/Gameplay.unity" &&
                   Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo("Assets/02_Escenas/Gameplay.unity"),
                "The existing paint transition must still reach Gameplay; the wider budget accounts for Editor scene-import variance.");
        }
    }
}
