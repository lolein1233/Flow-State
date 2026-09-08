using NUnit.Framework;
using UnityEngine;
namespace FlowState.Menu.Tests
{
    public sealed class GraffitiPlacementTests
    {
        [Test] public void CarouselWrapsInBothDirections()
        {
            Assert.That(GraffitiPlacement.Wrap(-1,4),Is.EqualTo(3));
            Assert.That(GraffitiPlacement.Wrap(4,4),Is.Zero);
            Assert.That(GraffitiPlacement.Wrap(-101,4),Is.EqualTo(3));
        }
        [Test] public void LongSessionsRespectLogoAndCanRegions()
        {
            var layout=new GraffitiPlacement(192);
            for(int i=0;i<10000;i++)
            {
                var s=layout.Next(i%4);
                Assert.That(s.rect.x-s.rect.z*.5f,Is.GreaterThan(0));
                Assert.That(s.rect.x+s.rect.z*.5f,Is.LessThan(.63f));
                Assert.That(s.rect.y+s.rect.w*.5f,Is.LessThan(.75f));
                Assert.That(s.rect.y-s.rect.w*.5f,Is.GreaterThan(.15f));
                Assert.That(float.IsNaN(s.angle),Is.False);
            }
        }
        [Test] public void SeedReproducesCompositionAndRepeatedWordsVary()
        {
            var a=new GraffitiPlacement(51);var b=new GraffitiPlacement(51);
            var first=a.Next(0);Assert.That(first.rect,Is.EqualTo(b.Next(0).rect));
            float distance=0;
            for(int i=0;i<100;i++){var x=a.Next(0);var y=b.Next(0);Assert.That(x.rect,Is.EqualTo(y.rect));distance+=Vector4.Distance(first.rect,x.rect);}
            Assert.That(distance,Is.GreaterThan(1));
        }
    }
}
