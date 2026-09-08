Shader "FLOWSTATE/Menu/Living Wall"
{
 Properties { _PaintTex("Accumulated paint",2D)="black"{} _MainTex("Paper grain",2D)="white"{} _FreshMask("Current stencil",2D)="black"{} _BaseColor("Wall",Color)=(.86,.84,.78,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
 struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 TEXTURE2D(_PaintTex); SAMPLER(sampler_PaintTex);
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 TEXTURE2D(_FreshMask); SAMPLER(sampler_FreshMask);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;
 float4 _FreshRect,_FreshColor,_FreshAccent;
 float _FreshAngle,_FreshReveal,_FreshGrain,_Wetness;
 CBUFFER_END
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{
 float grain=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv*3).r;
 float vignette=pow(saturate(length((i.uv-.5)*float2(1,.7))),2)*.35;
 float3 wall=_BaseColor.rgb*(.94+grain*.12-vignette);
 float4 paint=SAMPLE_TEXTURE2D(_PaintTex,sampler_PaintTex,i.uv);
 // The bounded baked archive stays visible as dry pigment; the current stroke retains full contrast.
 paint*=.43;
 float3 result=paint.rgb+wall*(1-paint.a);
 float2 d=i.uv-_FreshRect.xy;
 float2 p=float2(d.x*cos(_FreshAngle)-d.y*sin(_FreshAngle),d.x*sin(_FreshAngle)+d.y*cos(_FreshAngle))/max(_FreshRect.zw,.001)+.5;
 float inside=step(0,p.x)*step(p.x,1)*step(0,p.y)*step(p.y,1);
 float reveal=(1-smoothstep(_FreshReveal*1.18-.09,_FreshReveal*1.18+.02,p.x))*step(.001,_FreshReveal);
 float4 mask=SAMPLE_TEXTURE2D(_FreshMask,sampler_FreshMask,p)*inside*reveal;
 float noise=frac(sin(dot(floor(p*float2(1100,360)),float2(12.9898,78.233)))*43758.5453);
 float a=mask.r*lerp(1,step(_FreshGrain*.64,noise),.68);
 result=lerp(result,_BaseColor.rgb,mask.g*.6);
 float shadow=SAMPLE_TEXTURE2D(_FreshMask,sampler_FreshMask,p+float2(-.01,.023)).r*inside*reveal;
 result=lerp(result,_FreshAccent.rgb,shadow*.86);
 float sheen=pow(saturate(1-abs(p.y-.6)*7),8)*_Wetness*.22;
 result=lerp(result,lerp(_FreshColor.rgb,_FreshAccent.rgb,sheen),a);
 return half4(result,1);
 }
 ENDHLSL
 } }
}
