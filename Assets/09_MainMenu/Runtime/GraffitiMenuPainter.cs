using UnityEngine;
namespace FlowState.Menu
{
    public sealed class GraffitiMenuPainter : MonoBehaviour
    {
        public Renderer wall;
        public Material compositeTemplate;
        public int textureWidth=2048,textureHeight=1024;
        [Range(.88f,1)] public float historyRetention=.985f;
        public int seed=173;
        public WallPersistence persistence=WallPersistence.ResetOnEntry;
        public MenuOptionData[] options;
        RenderTexture history,working;
        Material composite,display;
        GraffitiPlacement placement;
        MenuStroke stroke;
        MenuOptionData active;
        float progress;
        float wetness;
        int lastSavedStroke=-1;
        bool painting;
        static byte[] sessionImage;
        public int StrokeCount {get;private set;}
        public bool IsPainting=>painting;
        public int TextureCount=>(history?1:0)+(working?1:0);
        public MenuStroke CurrentStroke=>stroke;
        public RenderTexture Surface=>painting?working:history;
        string SavePath=>System.IO.Path.Combine(Application.persistentDataPath,"flowstate-menu-wall-v1.png");
        void Awake()
        {
            textureWidth=Mathf.Clamp(textureWidth,512,4096); textureHeight=Mathf.Clamp(textureHeight,256,2048);
            history=CreateRT("Menu wall history"); working=CreateRT("Menu wall live paint");
            composite=new Material(compositeTemplate);
            display=new Material(wall.sharedMaterial); wall.sharedMaterial=display;
            placement=new GraffitiPlacement(seed==0?System.Environment.TickCount:seed);
            Clear(history); Clear(working);
            Restore(); display.SetTexture("_PaintTex",history);
        }
        RenderTexture CreateRT(string label)
        {
            return PaintRenderSurface.Create(textureWidth,textureHeight,label);
        }
        static void Clear(RenderTexture rt)
        {
            PaintRenderSurface.Clear(rt);
        }
        public void Begin(MenuOptionData option,int index)
        {
            if(painting) Commit();
            active=option; stroke=placement.Next(index); progress=0; painting=true;
            composite.SetTexture("_StampTex",option.wordMask);
            composite.SetVector("_Rect",stroke.rect);
            composite.SetFloat("_Angle",stroke.angle); composite.SetFloat("_Seed",stroke.seed);
            composite.SetColor("_PaintColor",option.paintColor); composite.SetColor("_Accent",option.accentColor);
            composite.SetFloat("_Grain",option.grain); composite.SetFloat("_Drips",option.drips);
            composite.SetFloat("_Retention",historyRetention);
            display.SetTexture("_FreshMask",option.wordMask);
            display.SetVector("_FreshRect",stroke.rect);
            display.SetFloat("_FreshAngle",stroke.angle);
            display.SetColor("_FreshColor",option.paintColor);
            display.SetColor("_FreshAccent",option.accentColor);
            display.SetFloat("_FreshGrain",option.grain);
            wetness=1;
            display.SetTexture("_PaintTex",working);
            Draw(0);
        }
        public Vector3 SprayTarget
        {
            get
            {
                var r=stroke.rect;
                float u=r.x-r.z*.5f+r.z*Mathf.Clamp01(progress);
                return wall.transform.TransformPoint(new Vector3(u-.5f,r.y-.5f,-.008f));
            }
        }
        void Update()
        {
            if(wetness>0){wetness=Mathf.MoveTowards(wetness,0,Time.unscaledDeltaTime*.35f);display.SetFloat("_Wetness",wetness);}
            if(!painting)return;
            progress=Mathf.Min(1,progress+Time.unscaledDeltaTime/Mathf.Max(.1f,active.sprayDuration));
            Draw(progress);
            if(progress>=1)Commit();
        }
        void Draw(float value)
        {
            composite.SetFloat("_Reveal",value);display.SetFloat("_FreshReveal",value); Graphics.Blit(history,working,composite);
        }
        void Commit()
        {
            Draw(1); var swap=history;history=working;working=swap;
            display.SetTexture("_PaintTex",history); painting=false;StrokeCount++;
        }
        public byte[] Snapshot()
        {
            var old=RenderTexture.active; RenderTexture.active=Surface;
            var tex=new Texture2D(textureWidth,textureHeight,TextureFormat.RGBA32,false,true);
            tex.ReadPixels(new Rect(0,0,textureWidth,textureHeight),0,0);tex.Apply();
            RenderTexture.active=old; byte[] png=tex.EncodeToPNG();Destroy(tex);return png;
        }
        public void Save()
        {
            if(persistence==WallPersistence.ResetOnEntry||!history||lastSavedStroke==StrokeCount)return;
            try
            {
                byte[] png=Snapshot();
                if(persistence==WallPersistence.Session)sessionImage=png;
                else System.IO.File.WriteAllBytes(SavePath,png);
                lastSavedStroke=StrokeCount;
            }
            catch(System.Exception e){Debug.LogWarning("Could not save menu wall: "+e.Message,this);}
        }
        void Restore()
        {
            Texture2D tex=null;
            try
            {
                byte[] png=persistence==WallPersistence.Session?sessionImage:null;
                if(persistence==WallPersistence.BetweenSessions&&System.IO.File.Exists(SavePath))
                {
                    if(new System.IO.FileInfo(SavePath).Length>24*1024*1024) return;
                    png=System.IO.File.ReadAllBytes(SavePath);
                }
                if(png==null)return;
                tex=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                if(tex.LoadImage(png)&&tex.width<=4096&&tex.height<=2048)Graphics.Blit(tex,history);
            }
            catch(System.Exception e){Debug.LogWarning("Menu wall reset: "+e.Message,this);}
            finally {if(tex)Destroy(tex);}
        }
        void OnApplicationQuit(){Save();}
        void OnDisable(){Save();}
        void OnDestroy()
        {
            if(history){history.Release();Destroy(history);} if(working){working.Release();Destroy(working);}
            if(composite)Destroy(composite);if(display)Destroy(display);
        }
    }
}
