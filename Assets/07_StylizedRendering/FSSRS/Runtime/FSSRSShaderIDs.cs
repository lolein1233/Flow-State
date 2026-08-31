using UnityEngine;

namespace FlowState.Rendering
{
    public static class FSSRSShaderIDs
    {
        public static readonly int PaperColor = Shader.PropertyToID("_FSSRS_PaperColor");
        public static readonly int InkColor = Shader.PropertyToID("_FSSRS_InkColor");
        public static readonly int ShadowColor = Shader.PropertyToID("_FSSRS_ShadowColor");
        public static readonly int MidColor = Shader.PropertyToID("_FSSRS_MidColor");
        public static readonly int HighlightColor = Shader.PropertyToID("_FSSRS_HighlightColor");
        public static readonly int AccentColor = Shader.PropertyToID("_FSSRS_AccentColor");
        public static readonly int PaletteInfluence = Shader.PropertyToID("_FSSRS_PaletteInfluence");

        public static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        public static readonly int OutlineIntensity = Shader.PropertyToID("_OutlineIntensity");
        public static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");
        public static readonly int DepthThreshold = Shader.PropertyToID("_DepthThreshold");
        public static readonly int NormalThreshold = Shader.PropertyToID("_NormalThreshold");
        public static readonly int LumaThreshold = Shader.PropertyToID("_LumaThreshold");
        public static readonly int PosterizeSteps = Shader.PropertyToID("_PosterizeSteps");
        public static readonly int GrainIntensity = Shader.PropertyToID("_GrainIntensity");
        public static readonly int HalftoneIntensity = Shader.PropertyToID("_HalftoneIntensity");
        public static readonly int HalftoneScale = Shader.PropertyToID("_HalftoneScale");
        public static readonly int HatchIntensity = Shader.PropertyToID("_HatchIntensity");
        public static readonly int DebugMode = Shader.PropertyToID("_DebugMode");
    }
}
