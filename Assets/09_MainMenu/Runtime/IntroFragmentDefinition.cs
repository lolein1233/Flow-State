using System;
using UnityEngine;
namespace FlowState.Menu
{
    public enum IntroTreatment { CreativeFlow, Anger, ClarityNeutral }
    public enum IntroPaintStage { Reveal, Extension, Deposition, CameraPush }
    public enum IntroPaintDebug { Composite, MaskA, MaskB, MaskC, CombinedPaint, LogoDeposition, Content }
    [Serializable]
    public sealed class IntroFragmentDefinition
    {
        public string label="Fragment A";
        public Vector2 position;
        public Vector2 scale=Vector2.one;
        public float orientation;
        [Tooltip("Local UV points, bottom-left origin. Up to 24 points per path.")]
        public Vector2[] path,extensionPath,depositionPath;
        [Tooltip("Designed silhouette in local UV space. Points must be counter-clockwise.")]
        public Vector2[] silhouette;
        [Tooltip("Optional second designed panel revealed by the same Timeline cue.")]
        public Vector2[] secondarySilhouette,secondaryPath;
        [Range(0,.8f)] public float secondaryDelay=.38f;
        [Range(.01f,.4f)] public float width=.13f;
        [Range(.05f,1)] public float extensionWidth=.25f;
        [Range(0,1)] public float edgeErosion=.2f;
        [Min(.05f),Tooltip("Default clip duration used when building the prototype Timeline.")]
        public float duration=.9f;
        public AnimationCurve revealCurve=AnimationCurve.EaseInOut(0,0,1,1);
        [Tooltip("Source UV rectangle sampled across the destination window.")]
        public Rect contentCrop=new Rect(0,0,1,1);
        public Rect contentWindow=new Rect(0,0,1,1);
        [Range(0,1)] public float overspray=.2f;
        public IntroTreatment treatment;
        public Rect logoContribution=new Rect(0,0,1,1);
        [Range(.05f,1.5f)] public float depositionWidth=.4f;
        public int seed=41;
        public Vector2 TransformPoint(Vector2 p)
        {
            p=Vector2.Scale(p,scale);float a=orientation*Mathf.Deg2Rad;
            // Rotate in physical 16:9 composition space, not stretched UV space.
            float x=p.x*16f/9;
            return position+new Vector2((x*Mathf.Cos(a)-p.y*Mathf.Sin(a))*9f/16,x*Mathf.Sin(a)+p.y*Mathf.Cos(a));
        }
    }
}
