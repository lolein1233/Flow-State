Shader "FlowState/IntroWorldPigment"
{
 Properties { _MaskTex("Masks",2D)="black"{} _ContentTex("Shared world",2D)="black"{} _LogoTex("Official logo",2D)="black"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Overlay+100" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest Always
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MaskTex);SAMPLER(sampler_MaskTex);
 TEXTURE2D(_ContentTex);SAMPLER(sampler_ContentTex);
 TEXTURE2D(_LogoTex);SAMPLER(sampler_LogoTex);
 float4 _Crop0,_Crop1,_Crop2,_Window0,_Window1,_Window2;
 float _Desaturate0,_Desaturate1,_Desaturate2;int _DebugMode;
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
 struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 float3 world(float2 uv,float4 crop,float4 window,float desaturate)
 {
   float2 p=crop.xy+(uv-window.xy)/window.zw*crop.zw;
   float3 c=SAMPLE_TEXTURE2D(_ContentTex,sampler_ContentTex,saturate(p)).rgb;
   return lerp(c,dot(c,float3(.2126,.7152,.0722)).xxx,desaturate);
 }
 float4 frag(V i):SV_Target
 {
   float4 m=SAMPLE_TEXTURE2D(_MaskTex,sampler_MaskTex,i.uv);
   float coverage=max(m.r,max(m.g,m.b));
   if(_DebugMode>=1&&_DebugMode<=3)return float4(m[_DebugMode-1].xxx,1);
   if(_DebugMode==4)return float4(coverage.xxx,1);
   if(_DebugMode==5)return float4(m.a.xxx,1);
   if(_DebugMode==6)return float4(SAMPLE_TEXTURE2D(_ContentTex,sampler_ContentTex,i.uv).rgb,1);
   float3 c=(world(i.uv,_Crop0,_Window0,_Desaturate0)*m.r+world(i.uv,_Crop1,_Window1,_Desaturate1)*m.g+world(i.uv,_Crop2,_Window2,_Desaturate2)*m.b)/max(.0001,m.r+m.g+m.b);
   float4 logo=SAMPLE_TEXTURE2D(_LogoTex,sampler_LogoTex,i.uv);
   // A second moving brush deposits pigment. There is deliberately no global logo opacity.
   // Coverage remains in the masks; transparent PNG pixels deposit black underpainting.
   float3 pigment=logo.rgb*logo.a;
   c=lerp(c,pigment,saturate(m.a/max(.001,coverage)));
   return float4(c*coverage,1);
 }
 ENDHLSL }
 }
}
