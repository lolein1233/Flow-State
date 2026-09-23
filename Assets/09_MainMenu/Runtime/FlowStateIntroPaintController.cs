using UnityEngine;
namespace FlowState.Menu
{
    public sealed class FlowStateIntroPaintController : MonoBehaviour
    {
        public Renderer surface;
        public Shader brushShader,compositeShader;
        public Texture2D officialLogo;
        public IntroFragmentDefinition[] fragments=new IntroFragmentDefinition[3];
        public IntroPaintDebug debugMode;
        public const int Width=1024,Height=576,MaxPoints=24,MaxShapePoints=8;
        public bool IsLive=>maskA;
        public bool IsFrozen {get;private set;}
        public int TextureCount=>(maskA?1:0)+(maskB?1:0);
        public Texture Mask=>maskA?(Texture)maskA:frozenMask;
        public Texture Content=>display?display.GetTexture("_ContentTex"):null;
        RenderTexture maskA,maskB;
        Texture2D frozenMask,frozenContent;
        Material brush,display,previousMaterial;
        readonly Vector4[] points=new Vector4[MaxPoints];
        readonly Vector4[] shapePoints=new Vector4[MaxShapePoints];
        readonly float[,] progress=new float[3,3];
        static readonly int Path=Shader.PropertyToID("_Path");
        static readonly int Shape=Shader.PropertyToID("_Shape");
        public void Begin(Texture content)
        {
            if(IsLive||IsFrozen)return;
            if(fragments==null||fragments.Length!=3||!surface||!officialLogo||!brushShader||!compositeShader)
                throw new System.InvalidOperationException("Three intro fragments, surface, official logo and shaders are required.");
            maskA=PaintRenderSurface.Create(Width,Height,"Intro shared masks A");
            maskB=PaintRenderSurface.Create(Width,Height,"Intro shared masks B");
            brush=new Material(brushShader);display=new Material(compositeShader);
            previousMaterial=surface.sharedMaterial;surface.sharedMaterial=display;
            display.SetTexture("_ContentTex",content);display.SetTexture("_LogoTex",officialLogo);
            ResetProgress();RenderMasks();
        }
        public void ResetProgress(){System.Array.Clear(progress,0,progress.Length);}
        public void SetProgress(int index,IntroPaintStage stage,float value)
        {if(index>=0&&index<3&&stage<=IntroPaintStage.Deposition)progress[index,(int)stage]=Mathf.Clamp01(value);}
        public void RenderMasks()
        {
            if(!IsLive)return;
            var previousTarget=RenderTexture.active;
            // Rebuild coverage from absolute path distance. Never accumulate animated RGB.
            PaintRenderSurface.Clear(maskA);
            for(int i=0;i<3;i++)
            {
                var f=fragments[i];
                Stamp(f,f.path,progress[i,0],i,false,1,f.silhouette);
                float secondary=Mathf.InverseLerp(f.secondaryDelay,1,progress[i,0]);
                Stamp(f,f.secondaryPath,secondary,i,false,1,f.secondarySilhouette);
                Stamp(f,f.extensionPath,progress[i,1],i,false,f.extensionWidth,null);
                Stamp(f,f.depositionPath,progress[i,2],i,true,f.depositionWidth,null);
                display.SetVector("_Crop"+i,RectVector(f.contentCrop));
                display.SetVector("_Window"+i,RectVector(f.contentWindow));
                display.SetFloat("_Desaturate"+i,f.treatment==IntroTreatment.ClarityNeutral?.65f:.25f);
            }
            display.SetTexture("_MaskTex",maskA);display.SetInt("_DebugMode",(int)debugMode);
            RenderTexture.active=previousTarget;
        }
        static Vector4 RectVector(Rect r)=>new Vector4(r.x,r.y,Mathf.Max(.001f,r.width),Mathf.Max(.001f,r.height));
        void Stamp(IntroFragmentDefinition f,Vector2[] path,float value,int channel,bool deposit,float widthScale,Vector2[] silhouette)
        {
            if(value<=0||path==null||path.Length<2)return;
            int count=Mathf.Min(MaxPoints,path.Length);float length=0;
            for(int i=0;i<count;i++)
            {
                Vector2 p=f.TransformPoint(path[i]);p.x*=16f/9;
                if(i>0)length+=Vector2.Distance(p,new Vector2(points[i-1].x,points[i-1].y));
                points[i]=new Vector4(p.x,p.y,length,0);
            }
            brush.SetVectorArray(Path,points);brush.SetInt("_Count",count);
            int shapeCount=silhouette!=null?Mathf.Min(MaxShapePoints,silhouette.Length):0;
            for(int i=0;i<shapeCount;i++)
            {Vector2 p=f.TransformPoint(silhouette[i]);shapePoints[i]=new Vector4(p.x*16f/9,p.y,0,0);}
            brush.SetVectorArray(Shape,shapePoints);brush.SetInt("_ShapeCount",shapeCount);
            brush.SetFloat("_Structured",shapeCount>=3?1:0);brush.SetFloat("_EdgeErosion",f.edgeErosion);
            brush.SetFloat("_Distance",length*Mathf.Clamp01(f.revealCurve.Evaluate(value)));
            brush.SetFloat("_Width",f.width*widthScale);brush.SetFloat("_Overspray",f.overspray);
            brush.SetFloat("_Seed",f.seed);brush.SetFloat("_Rough",f.treatment==IntroTreatment.Anger?1:0);
            var channels=Vector4.zero;channels[channel]=1;channels.w=deposit?1:0;
            brush.SetVector("_Channels",channels);brush.SetVector("_Region",RectVector(f.logoContribution));
            brush.SetFloat("_Deposit",deposit?1:0);
            Graphics.Blit(maskA,maskB,brush);var swap=maskA;maskA=maskB;maskB=swap;
        }
        public void Freeze()
        {
            if(!IsLive)return;
            frozenMask=PaintRenderSurface.Freeze(maskA);
            var content=Content as RenderTexture;if(content)frozenContent=PaintRenderSurface.Freeze(content);
            display.SetTexture("_MaskTex",frozenMask);display.SetTexture("_ContentTex",frozenContent);
            PaintRenderSurface.Release(ref maskA);PaintRenderSurface.Release(ref maskB);
            if(brush)Destroy(brush);brush=null;IsFrozen=true;
        }
        void Update(){if(display)display.SetInt("_DebugMode",(int)debugMode);}
        public void End()
        {
            if(surface&&display&&surface.sharedMaterial==display)surface.sharedMaterial=previousMaterial;
            PaintRenderSurface.Release(ref maskA);PaintRenderSurface.Release(ref maskB);
            if(brush)Destroy(brush);if(display)Destroy(display);
            if(frozenMask)Destroy(frozenMask);if(frozenContent)Destroy(frozenContent);
            brush=display=null;frozenMask=frozenContent=null;IsFrozen=false;
        }
        void OnDestroy(){End();}
    }
}
