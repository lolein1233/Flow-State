using System;
using UnityEngine;

namespace FlowState.Rendering
{
    [Serializable]
    public struct FlowPaletteState
    {
        public Color paper;
        public Color ink;
        public Color shadow;
        public Color mid;
        public Color highlight;
        public Color accent;

        public static FlowPaletteState Lerp(in FlowPaletteState from, in FlowPaletteState to, float t)
        {
            t = Mathf.Clamp01(t);
            return new FlowPaletteState
            {
                paper = Color.Lerp(from.paper, to.paper, t),
                ink = Color.Lerp(from.ink, to.ink, t),
                shadow = Color.Lerp(from.shadow, to.shadow, t),
                mid = Color.Lerp(from.mid, to.mid, t),
                highlight = Color.Lerp(from.highlight, to.highlight, t),
                accent = Color.Lerp(from.accent, to.accent, t)
            };
        }
    }

    [CreateAssetMenu(fileName = "FP_NewPalette", menuName = "FLOW STATE/FSSRS/Palette Profile")]
    public sealed class FlowPaletteProfile : ScriptableObject
    {
        [SerializeField] private Color paper = new(0.91f, 0.88f, 0.81f, 1f);
        [SerializeField] private Color ink = new(0.04f, 0.04f, 0.06f, 1f);
        [SerializeField] private Color shadow = new(0.12f, 0.17f, 0.24f, 1f);
        [SerializeField] private Color mid = new(0.35f, 0.58f, 0.66f, 1f);
        [SerializeField] private Color highlight = new(0.96f, 0.88f, 0.52f, 1f);
        [SerializeField] private Color accent = new(0.91f, 0.25f, 0.18f, 1f);

        public FlowPaletteState State => new()
        {
            paper = paper,
            ink = ink,
            shadow = shadow,
            mid = mid,
            highlight = highlight,
            accent = accent
        };

        public void Configure(in FlowPaletteState state)
        {
            paper = state.paper;
            ink = state.ink;
            shadow = state.shadow;
            mid = state.mid;
            highlight = state.highlight;
            accent = state.accent;
        }
    }
}
