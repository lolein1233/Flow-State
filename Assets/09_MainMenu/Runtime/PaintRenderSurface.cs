using UnityEngine;
namespace FlowState.Menu
{
    // Allocation and lifetime only; menu strokes and intro masks retain separate ownership.
    public static class PaintRenderSurface
    {
        public static RenderTexture Create(int width,int height,string label,int depth=0)
        {
            var rt=new RenderTexture(width,height,depth,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
            {name=label,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,useMipMap=false,autoGenerateMips=false};
            rt.Create();Clear(rt);return rt;
        }
        public static void Clear(RenderTexture rt)
        {var old=RenderTexture.active;RenderTexture.active=rt;GL.Clear(true,true,Color.clear);RenderTexture.active=old;}
        public static void Release(ref RenderTexture rt)
        {if(!rt)return;if(RenderTexture.active==rt)RenderTexture.active=null;rt.Release();Object.Destroy(rt);rt=null;}
        public static Texture2D Freeze(RenderTexture rt)
        {
            var old=RenderTexture.active;RenderTexture.active=rt;
            var copy=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false,true)
            {name=rt.name+" review snapshot",wrapMode=TextureWrapMode.Clamp};
            copy.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);copy.Apply(false,true);
            RenderTexture.active=old;return copy;
        }
    }
}
