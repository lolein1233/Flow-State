using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace FlowState.Menu.Tests
{
    public sealed class IntroPrototypePlayTests
    {
        const string ScenePath="Assets/02_Escenas/MainMenu_FlowState.unity";
        FlowStateIntroDirector intro;
        float previousVolume;
        [UnitySetUp] public IEnumerator Setup()
        {
            previousVolume=AudioListener.volume;AudioListener.volume=0;
            void Configure(Scene scene,LoadSceneMode mode)
            {if(scene.path==ScenePath){intro=Object.FindFirstObjectByType<FlowStateIntroDirector>();intro.mode=IntroMode.Full;}}
            SceneManager.sceneLoaded+=Configure;
            try{yield return SceneManager.LoadSceneAsync(ScenePath);}
            finally{SceneManager.sceneLoaded-=Configure;}
            yield return new WaitForSecondsRealtime(.15f);intro.PreviewPrototypeAt(0);yield return null;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(intro)intro.SkipIntro();AudioListener.volume=previousVolume;
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        static Color32[] Read(Texture source)
        {
            var rt=(RenderTexture)source;var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false,true);
            tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();
            var pixels=tex.GetPixels32();RenderTexture.active=old;Object.Destroy(tex);return pixels;
        }
        static int Count(Color32[] pixels,int channel)
        {return pixels.Count(p=>(channel==0?p.r:channel==1?p.g:channel==2?p.b:p.a)>128);}
        Color32[] At(double time)
        {
            intro.PreviewPrototypeAt(time);var pixels=Read(intro.prototypePaint.Mask);
            TestContext.WriteLine("t="+time+" coverage RGBA="+Count(pixels,0)+","+Count(pixels,1)+","+Count(pixels,2)+","+Count(pixels,3));
            return pixels;
        }
        [UnityTest] public IEnumerator ThreeChannelsGrowFromTrueBlackAndKeepNegativeSpace()
        {
            Assert.That(At(.2).All(p=>p.r==0&&p.g==0&&p.b==0&&p.a==0),Is.True,"Unbound blit input must not inject a grey background.");
            var early=At(.55);var a=At(.85);var b=At(1.25);var c=At(1.70);var all=At(3.00);
            Assert.That(Count(early,0),Is.GreaterThan(50));Assert.That(Count(a,0),Is.GreaterThan(Count(early,0)*2));
            Assert.That(Count(a,1)+Count(a,2),Is.Zero);
            Assert.That(Count(b,1),Is.GreaterThan(100));Assert.That(Count(all,1),Is.GreaterThan(Count(b,1)));
            Assert.That(Count(c,2),Is.GreaterThan(50));Assert.That(Count(all,2),Is.GreaterThan(Count(c,2)));
            Assert.That(all.Count(p=>p.r>128||p.g>128||p.b>128)/(float)all.Length,Is.LessThan(.38));
            Assert.That(intro.prototypePaint.fragments.All(f=>f.secondarySilhouette!=null&&f.secondarySilhouette.Length>=4),Is.True);
            Assert.That(Count(all,3),Is.Zero);yield return null;
        }
        [UnityTest] public IEnumerator SeekingIsDeterministicWhileSharedWorldKeepsMoving()
        {
            var direct=At(3.40);
            Assert.That(Count(direct,0),Is.GreaterThan(1000),"A deterministic but empty render is not a valid pass.");
            for(int step=0;step<12;step++)intro.PreviewPrototypeAt(step*.31);
            CollectionAssert.AreEqual(direct,At(3.40),"Mask depends on evaluation history/frame partition.");
            var mask=At(3.05);var world=Read(intro.contentProvider.Surface);
            CollectionAssert.AreEqual(mask,At(3.55),"Completed vertical paths should stand still.");
            var moved=Read(intro.contentProvider.Surface);
            Assert.That(world.Where((p,n)=>!p.Equals(moved[n])).Count(),Is.GreaterThan(1000),"World must continue moving inside the stationary masks.");
            Assert.That(intro.prototypePaint.Content,Is.SameAs(intro.contentProvider.Surface));
            Assert.That(intro.contentProvider.diorama.GetComponentsInChildren<Camera>(true).Length,Is.EqualTo(1));
            var f=intro.prototypePaint.fragments;
            var slopeA=f[0].contentCrop.width/f[0].contentWindow.width;
            var slopeB=f[1].contentCrop.width/f[1].contentWindow.width;
            Assert.That(slopeA,Is.EqualTo(slopeB).Within(.0001));
            Assert.That(f[0].contentCrop.x-f[0].contentWindow.x*slopeA,Is.EqualTo(f[1].contentCrop.x-f[1].contentWindow.x*slopeB).Within(.0001));
            Assert.That(f[2].contentCrop,Is.Not.EqualTo(f[0].contentCrop));yield return null;
        }
        [UnityTest] public IEnumerator SixPanelsRemainDioramaOnlyWithoutLogoDeposition()
        {
            var final=At(4.84);
            Assert.That(Count(final,0),Is.GreaterThan(1000));
            Assert.That(Count(final,1),Is.GreaterThan(1000));
            Assert.That(Count(final,2),Is.GreaterThan(1000));
            Assert.That(Count(final,3),Is.Zero,"The official logo must not be deposited in this revision.");
            Assert.That(intro.prototypePaint.surface.sharedMaterial.GetTexture("_LogoTex"),Is.SameAs(intro.prototypePaint.officialLogo));
            Assert.That(intro.prototypePaint.surface.sharedMaterial.HasProperty("_LogoOpacity"),Is.False);
            Assert.That(intro.prototypeTimeline.playableAsset.outputs.Count(),Is.EqualTo(1));
            Assert.That(intro.prototypeTimeline.playableAsset.duration,Is.EqualTo(5.10).Within(.01));
            Assert.That(intro.cameraController.ReactiveEnabled,Is.False);Assert.That(intro.menu.IsLocked,Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator NaturalEndReleasesLiveResourcesAndSkipRestoresMenuOnce()
        {
            intro.PreviewPrototypeAt(4.84);yield return null;
            Assert.That(intro.Phase,Is.EqualTo(IntroPhase.CameraPush));
            Assert.That(intro.cameraController.IntroPoseProgress,Is.GreaterThan(.95f));
            Assert.That(intro.cameraController.responseRig.localPosition.z,Is.GreaterThan(.15f));
            Assert.That(intro.cameraController.menuCamera.fieldOfView,Is.LessThan(intro.cameraController.baseFieldOfView));
            intro.PreviewPrototypeAt(4.90);intro.prototypeTimeline.Play();yield return new WaitForSecondsRealtime(.35f);
            Assert.That(intro.PrototypeComplete,Is.True);Assert.That(intro.prototypePaint.IsFrozen,Is.True);
            Assert.That(intro.prototypePaint.TextureCount,Is.Zero);Assert.That(intro.contentProvider.Surface,Is.Null);
            Assert.That(intro.contentProvider.contentCamera.enabled,Is.False);Assert.That(intro.contentProvider.contentCamera.targetTexture,Is.Null);
            Assert.That(intro.menu.IsLocked,Is.True);
            intro.SkipIntro();intro.SkipIntro();yield return new WaitForSecondsRealtime(.8f);
            Assert.That(intro.prototypePaint.IsFrozen,Is.False);Assert.That(intro.prototypePaint.Content,Is.Null);
            Assert.That(intro.menu.IsReady,Is.True);Assert.That(intro.menu.painter.StrokeCount,Is.EqualTo(1));
            Assert.That(intro.cameraController.ReactiveEnabled,Is.True);
        }
    }
}
