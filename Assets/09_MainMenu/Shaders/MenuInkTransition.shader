Shader "FLOWSTATE/Menu/Ink Transition"
{
 Properties { _Progress("Paint invasion",Range(0,1))=0 _Color("Pigment",Color)=(.03,.02,.04,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Overlay+100" "RenderType"="Transparent"} Pass {
 ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 CBUFFER_START(UnityPerMaterial)
 float _Progress;float4 _Color;
 CBUFFER_END
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 float h(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 half4 frag(V i):SV_Target{
 float r=length((i.uv-float2(.79,.38))*float2(1.65,1));
 float n=h(floor(i.uv*110))*.11+h(floor(i.uv*390))*.035;
 float a=(1-smoothstep(_Progress*2-.07,_Progress*2+.02,r+n))*step(.001,_Progress);
 float dots=step(.77,h(floor(i.uv*240)));
 return half4(_Color.rgb*(.86+dots*.14),a);
 }
 ENDHLSL
 } }
}
