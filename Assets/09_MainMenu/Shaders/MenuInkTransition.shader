Shader "FLOWSTATE/Menu/Ink Transition"
{
 Properties
 {
  _Progress("Paint invasion",Range(0,1))=0
  _Color("Deep pigment",Color)=(.03,.02,.05,1)
  _AccentColor("Wet edge",Color)=(1,.45,.04,1)
  _Seed("Spray seed",Float)=17
 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Overlay+100" "RenderType"="Transparent"} Pass {
 ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 CBUFFER_START(UnityPerMaterial)
 float _Progress;float4 _Color;float4 _AccentColor;float _Seed;
 CBUFFER_END
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise21(float2 p)
 {
  float2 id=floor(p),f=frac(p);f=f*f*(3-2*f);
  return lerp(lerp(hash21(id),hash21(id+float2(1,0)),f.x),lerp(hash21(id+float2(0,1)),hash21(id+1),f.x),f.y);
 }
 half4 frag(V i):SV_Target{
 float2 uv=i.uv;
 float coarse=noise21(uv*9+_Seed);
 float2 fromNozzle=(uv-float2(.84,.29))*float2(1.78,1);
 float distortedRadius=length(fromNozzle)+(coarse-.5)*.28;
 float front=_Progress*2.16-.09;
 float body=1-smoothstep(front-.08,front+.055,distortedRadius);
 float diagonal=_Progress*1.1-(1-uv.x)*.72-abs(uv.y-.29)*.24+(coarse-.5)*.15;
 float streak=smoothstep(-.035,.07,diagonal)*smoothstep(.02,.16,uv.x);
 float alpha=saturate(max(body,streak*.72));
 alpha*=step(.001,_Progress);
 float edge=saturate(1-abs(distortedRadius-front)/.07)*alpha;
 float wet=noise21(uv*18-_Seed)*.05+edge*.68;
 float3 pigment=lerp(_Color.rgb,_AccentColor.rgb,saturate(wet));
 return half4(pigment,alpha);
 }
 ENDHLSL
 } }
}
