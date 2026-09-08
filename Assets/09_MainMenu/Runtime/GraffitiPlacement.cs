using System;
using UnityEngine;
namespace FlowState.Menu
{
    [Serializable]
    public struct MenuStroke
    {
        public int option;
        public Vector4 rect;
        public float angle, seed;
    }

    // Continuous candidate scoring: old coverage matters, without visible slots.
    public sealed class GraffitiPlacement
    {
        readonly float[] density=new float[18*10];
        readonly System.Random random;
        Vector2 previous=new Vector2(.31f,.5f);
        public GraffitiPlacement(int seed) { random=new System.Random(seed); }
        float Next(float min,float max) => Mathf.Lerp(min,max,(float)random.NextDouble());
        public MenuStroke Next(int option)
        {
            Vector2 best=previous; float bestScore=float.NegativeInfinity;
            for(int i=0;i<24;i++)
            {
                var p=new Vector2(Next(.235f,.385f),Next(.32f,.60f));
                int x=Mathf.Clamp((int)(p.x*18),0,17), y=Mathf.Clamp((int)(p.y*10),0,9);
                float score=-density[y*18+x]+Mathf.Min(Vector2.Distance(p,previous),.14f)*3+Next(0,.15f);
                if(score>bestScore){bestScore=score;best=p;}
            }
            for(int y=0;y<10;y++)for(int x=0;x<18;x++)
            {
                int n=y*18+x;
                float distance=Vector2.Distance(new Vector2((x+.5f)/18,(y+.5f)/10),best);
                density[n]=density[n]*.965f+Mathf.Max(0,1-distance/.23f);
            }
            previous=best;
            return new MenuStroke {option=option,rect=new Vector4(best.x,best.y,Next(.41f,.46f),Next(.21f,.27f)),angle=Next(-.095f,.095f),seed=Next(1,10000)};
        }
        public static int Wrap(int index,int count) => count<=0?0:((index%count)+count)%count;
    }
}
