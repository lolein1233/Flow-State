using NUnit.Framework;
using UnityEngine;

namespace FlowState.Rendering.Tests
{
    public sealed class FlowPaletteBlendTests
    {
        [Test]
        public void Lerp_ClampsTheInterpolationFactor()
        {
            FlowPaletteState from = Solid(Color.black);
            FlowPaletteState to = Solid(Color.white);

            Assert.That(FlowPaletteState.Lerp(from, to, -1f).paper, Is.EqualTo(Color.black));
            Assert.That(FlowPaletteState.Lerp(from, to, 2f).paper, Is.EqualTo(Color.white));
        }

        [Test]
        public void Lerp_BlendsEveryPalettePlate()
        {
            FlowPaletteState result = FlowPaletteState.Lerp(Solid(Color.black), Solid(Color.white), 0.5f);

            Assert.That(result.paper.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(result.ink.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(result.shadow.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(result.mid.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(result.highlight.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(result.accent.r, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void EmotionEnergy_PutsDoubtBelowFlowAndAnger()
        {
            float doubt = FlowStatePaletteController.GetEmotionEnergy(FlowEmotion.Doubt);
            float flow = FlowStatePaletteController.GetEmotionEnergy(FlowEmotion.CreativeFlow);
            float anger = FlowStatePaletteController.GetEmotionEnergy(FlowEmotion.Anger);

            Assert.That(doubt, Is.LessThan(flow));
            Assert.That(flow, Is.LessThan(anger));
        }

        private static FlowPaletteState Solid(Color color)
        {
            return new FlowPaletteState
            {
                paper = color,
                ink = color,
                shadow = color,
                mid = color,
                highlight = color,
                accent = color
            };
        }
    }
}
