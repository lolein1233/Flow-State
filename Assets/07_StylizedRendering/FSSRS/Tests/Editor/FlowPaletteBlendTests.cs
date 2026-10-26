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
        public void EmotionEnergy_PutsNeutralBelowFlowAndAnger()
        {
            float neutral = FlowStatePaletteController.GetEmotionEnergy(FlowEmotion.Neutral);
            float flow = FlowStatePaletteController.GetEmotionEnergy(FlowEmotion.CreativeFlow);
            float anger = FlowStatePaletteController.GetEmotionEnergy(FlowEmotion.Anger);

            Assert.That(neutral, Is.LessThan(flow));
            Assert.That(flow, Is.LessThan(anger));
        }

        [Test]
        public void AvailableLooksPreserveShaderIndicesAndExcludeClarity()
        {
            Assert.That((int)FlowEmotion.Comic, Is.EqualTo(0));
            Assert.That((int)FlowEmotion.Neutral, Is.EqualTo(1));
            Assert.That(System.Enum.GetNames(typeof(FlowEmotion)), Is.EqualTo(
                new[] { "Comic", "Neutral", "Anger", "CreativeFlow" }));
        }

        [Test]
        public void PaletteMappingKeepsComicAndNeutralDistinct()
        {
            var owner = new GameObject("Palette mapping test");
            owner.SetActive(false);
            var comic = ScriptableObject.CreateInstance<FlowPaletteProfile>();
            var neutral = ScriptableObject.CreateInstance<FlowPaletteProfile>();
            try
            {
                var controller = owner.AddComponent<FlowStatePaletteController>();
                controller.ConfigureEmotionPalettes(comic, neutral, null, null, FlowEmotion.Neutral);
                Assert.That(controller.CurrentEmotion, Is.EqualTo(FlowEmotion.Neutral));
                Assert.That(controller.PaletteFor(FlowEmotion.Comic), Is.SameAs(comic));
                Assert.That(controller.PaletteFor(FlowEmotion.Neutral), Is.SameAs(neutral));
                Assert.That(controller.PaletteFor((FlowEmotion)4), Is.SameAs(neutral));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(comic);
                Object.DestroyImmediate(neutral);
            }
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
