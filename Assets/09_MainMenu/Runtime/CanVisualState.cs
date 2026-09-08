using UnityEngine;
using FlowState.Rendering;
namespace FlowState.Menu
{
    public sealed class CanVisualState : MonoBehaviour
    {
        public FlowStatePaletteController paletteController;
        public Renderer canRenderer;
        public Light accentLight;
        MaterialPropertyBlock block;
        Color sourceAccent, targetAccent;
        float sourceEmotion, targetEmotion, fromEnergy, toEnergy, fromDots, toDots;
        float time,duration;
        bool blending;
        void Awake() { block=new MaterialPropertyBlock(); }
        public void Apply(MenuOptionData option,float seconds)
        {
            sourceAccent=accentLight.color; targetAccent=option.accentColor;
            sourceEmotion=Shader.GetGlobalFloat(FSSRSShaderIDs.EmotionIndex); targetEmotion=(float)option.visualState;
            fromEnergy=Shader.GetGlobalFloat(FSSRSShaderIDs.EmotionEnergy); toEnergy=FlowStatePaletteController.GetEmotionEnergy(option.visualState);
            fromDots=block.GetFloat("_HalftoneStrength"); toDots=option.halftone;
            paletteController.SetPalette(option.palette,seconds);
            time=0; duration=Mathf.Max(.01f,seconds); blending=true;
        }
        void Update()
        {
            if(!blending)return;
            time+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/duration));
            Shader.SetGlobalFloat(FSSRSShaderIDs.EmotionIndex,Mathf.Lerp(sourceEmotion,targetEmotion,t));
            Shader.SetGlobalFloat(FSSRSShaderIDs.EmotionEnergy,Mathf.Lerp(fromEnergy,toEnergy,t));
            Color accent=Color.Lerp(sourceAccent,targetAccent,t);
            accentLight.color=accent;
            block.SetColor("_RimColor",accent);
            block.SetFloat("_HalftoneStrength",Mathf.Lerp(fromDots,toDots,t));
            block.SetFloat("_InkBreakup",.3f+Mathf.Sin(t*Mathf.PI)*.65f);
            block.SetFloat("_HatchStrength",.32f+Mathf.Sin(t*Mathf.PI)*.24f);
            canRenderer.SetPropertyBlock(block);
            blending=time<duration;
        }
    }
}
