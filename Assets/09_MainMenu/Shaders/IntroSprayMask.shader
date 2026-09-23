Shader "Hidden/FlowState/IntroSprayMask"
{
 Properties { _MainTex("Previous coverage",2D)="black"{} }
 SubShader { Cull Off ZWrite Off ZTest Always
 Pass { HLSLPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 float4 _Path[24],_Shape[8],_Channels,_Region;
 int _Count,_ShapeCount;
 float _Distance,_Width,_Overspray,_Seed,_Rough,_Deposit,_Structured,_EdgeErosion;
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7))+_Seed)*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float4 frag(v2f_img i):SV_Target
 {
   float2 p=i.uv*float2(1.77777778,1);float d=100;
   [loop] for(int n=1;n<_Count;n++)
   {
     float4 a=_Path[n-1],b=_Path[n];if(a.z>_Distance)break;
     float f=saturate((_Distance-a.z)/max(.00001,b.z-a.z));
     float2 v=(b.xy-a.xy)*f;
     float h=saturate(dot(p-a.xy,v)/max(.0000001,dot(v,v)));
     d=min(d,length(p-a.xy-v*h));
   }
   float grain=hash(floor(p*float2(1800,1800)));
   float radius=_Width*.5*(.77+.30*noise(p*39)+.10*noise(p*110));
   float edge=.0025+.006*_Overspray;
   float brushCoverage=1-smoothstep(radius-edge,radius+edge,d);
   float inside=1,shapeEdge=100;
   [loop] for(int s=0;s<_ShapeCount;s++)
   {
     float2 a=_Shape[s].xy,b=_Shape[(s+1)%_ShapeCount].xy,v=b-a,w=p-a;
     inside*=step(0,v.x*w.y-v.y*w.x);
     float h=saturate(dot(w,v)/max(.0000001,dot(v,v)));
     shapeEdge=min(shapeEdge,length(w-v*h));
   }
   float erosion=(noise(p*135)-.5)*(.003+.006*_EdgeErosion);
   float designed=inside*smoothstep(-.001,.003,shapeEdge+erosion);
   float coverage=lerp(brushCoverage,designed*brushCoverage,_Structured);
   coverage*=lerp(1,.92+.08*grain,smoothstep(radius*.6,radius,d));
   coverage*=1-_Rough*.70*step(.965,noise(p*float2(28,260)));
   float pathMist=1-smoothstep(radius,radius+.010+.020*_Overspray,d);
   float shapeMist=1-smoothstep(.002,.012+.016*_Overspray,shapeEdge);
   float mist=lerp(pathMist,pathMist*shapeMist,_Structured)*.34;
   coverage=max(coverage,mist*step(.88,grain)*_Overspray);
   float2 regionDistance=min(i.uv-_Region.xy,_Region.xy+_Region.zw-i.uv);
   coverage*=lerp(1,smoothstep(0,.012,min(regionDistance.x,regionDistance.y)),_Deposit);
   return max(tex2D(_MainTex,i.uv),coverage*_Channels);
 }
 ENDHLSL }
 }
}
