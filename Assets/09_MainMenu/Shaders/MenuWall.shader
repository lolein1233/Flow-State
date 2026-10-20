Shader "FLOWSTATE/Menu/Living Wall"
{
 Properties
 {
  _PaintTex("Accumulated paint",2D)="black"{}
  _MainTex("Paper grain",2D)="white"{}
  _FreshMask("Current stencil",2D)="black"{}
  _BackdropMask("Monumental mask",2D)="black"{}
  _BackdropLogo("FLOW STATE logo",2D)="black"{}
  _BaseColor("Wall",Color)=(.035,.038,.045,1)
  _BackdropAccent("Holo accent",Color)=(1,.015,.31,1)
  _BackdropMotion("Motion amount",Range(0,1))=.82
  _BackdropHoloIntensity("Hologram intensity",Range(0,1))=.88
 }
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
 TEXTURE2D(_BackdropMask); SAMPLER(sampler_BackdropMask);
 TEXTURE2D(_BackdropLogo); SAMPLER(sampler_BackdropLogo);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_BackdropAccent;
 float4 _FreshRect,_FreshColor,_FreshAccent;
 float4 _BackdropParallax;
 float _FreshAngle,_FreshReveal,_FreshGrain,_Wetness;
 float _BackdropMotion,_BackdropHoloIntensity,_BackdropImpulse,_BackdropDirection;
 CBUFFER_END
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 float Hash21(float2 p){p=frac(p*float2(123.34,345.45));p+=dot(p,p+34.345);return frac(p.x*p.y);}
 half4 frag(V i):SV_Target{
 float grain=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv*3).r;
 float vignette=pow(saturate(length((i.uv-.5)*float2(1,.7))),2)*.35;
 float3 wall=_BaseColor.rgb*(.94+grain*.12-vignette);
 float motion=_BackdropMotion;
 float time=_Time.y;
 float2 parallax=_BackdropParallax.xy*float2(.005,.003)*motion;
 float scan=.5+.5*sin((i.uv.y+time*.018)*620);
 float maskBreath=1+sin(time*.48)*.008*motion;
 float2 maskUV=(i.uv-.5)/maskBreath+.5+parallax;
 float maskStrip=floor(maskUV.y*118);
 float maskTick=floor(time*7);
 float maskGlitchGate=step(.93,Hash21(float2(maskStrip,maskTick)));
 float maskGlitch=(Hash21(float2(maskStrip+11,maskTick))-.5)*.034*maskGlitchGate*motion;
 maskGlitch+=_BackdropDirection*_BackdropImpulse*.012;
 maskUV.x+=maskGlitch;
 float4 maskPlate=SAMPLE_TEXTURE2D(_BackdropMask,sampler_BackdropMask,maskUV);
 float chromaShift=(.0015+_BackdropImpulse*.0035)*motion;
 float maskRed=SAMPLE_TEXTURE2D(_BackdropMask,sampler_BackdropMask,maskUV+float2(chromaShift,0)).r;
 float maskBlue=SAMPLE_TEXTURE2D(_BackdropMask,sampler_BackdropMask,maskUV-float2(chromaShift,0)).b;
 float3 chromaticMask=float3(maskRed,maskPlate.g,maskBlue);
 float maskLuma=dot(maskPlate.rgb,float3(.299,.587,.114));
 float neighborLuma=dot(SAMPLE_TEXTURE2D(_BackdropMask,sampler_BackdropMask,maskUV+float2(.0025,0)).rgb,float3(.299,.587,.114));
 float holoEdge=saturate(abs(maskLuma-neighborLuma)*9+fwidth(maskLuma)*2.4);
 float holoSweep=pow(saturate(1-abs(frac(maskUV.y*1.65-time*.105)-.5)*2),7);
 float sparkle=step(.994,Hash21(floor(maskUV*float2(720,405))+maskTick))*maskLuma*motion;
 float3 coolHolo=float3(.28,.92,1.22);
 float3 holoColor=lerp(maskPlate.rgb,chromaticMask*lerp(float3(.72,.78,.88),coolHolo,holoSweep*.62),_BackdropHoloIntensity*.42);
 holoColor=lerp(holoColor,_BackdropAccent.rgb,holoEdge*(.18+_BackdropImpulse*.28));
 holoColor+=coolHolo*(holoSweep*.11+sparkle*.48)*motion*_BackdropHoloIntensity;
 float maskFlicker=.72+scan*.075*motion+_BackdropImpulse*.1;
 wall=lerp(wall,holoColor*maskFlicker,maskPlate.a*.9);

 float strip=floor(i.uv.y*92+time*8);
 float glitchGate=step(.91,Hash21(float2(strip,floor(time*8))));
 float glitch=(Hash21(float2(strip+7,floor(time*8)))-.5)*.028*glitchGate*motion;
 glitch+=_BackdropDirection*_BackdropImpulse*.014;
 float2 logoUV=(i.uv-float2(.5,.76))/float2(.72,.78)+.5;
 logoUV+=float2(glitch,parallax.y*.35);
 float4 logoPlate=SAMPLE_TEXTURE2D(_BackdropLogo,sampler_BackdropLogo,logoUV);
 float logoPulse=.82+sin(time*1.45)*.045*motion+_BackdropImpulse*.075;
 wall=lerp(wall,logoPlate.rgb*(1+scan*.05),logoPlate.a*logoPulse);

 float4 paint=SAMPLE_TEXTURE2D(_PaintTex,sampler_PaintTex,i.uv);
 // The bounded baked archive stays visible as dry pigment; the current stroke retains full contrast.
 paint*=.74;
 float3 result=paint.rgb+wall*(1-paint.a);
 float2 d=i.uv-_FreshRect.xy;
 float2 p=float2(d.x*cos(_FreshAngle)-d.y*sin(_FreshAngle),d.x*sin(_FreshAngle)+d.y*cos(_FreshAngle))/max(_FreshRect.zw,.001)+.5;
 float inside=step(0,p.x)*step(p.x,1)*step(0,p.y)*step(p.y,1);
 float reveal=(1-smoothstep(_FreshReveal*1.18-.09,_FreshReveal*1.18+.02,p.x))*step(.001,_FreshReveal);
 float4 mask=SAMPLE_TEXTURE2D(_FreshMask,sampler_FreshMask,p)*inside*reveal;
 float noise=frac(sin(dot(floor(p*float2(1100,360)),float2(12.9898,78.233)))*43758.5453);
 float a=mask.r*lerp(1,step(_FreshGrain*.64,noise),.68);
 result=lerp(result,float3(.86,.84,.79),mask.g*.58);
 float shadow=SAMPLE_TEXTURE2D(_FreshMask,sampler_FreshMask,p+float2(-.01,.023)).r*inside*reveal;
 result=lerp(result,_FreshAccent.rgb,shadow*.86);
 float sheen=pow(saturate(1-abs(p.y-.6)*7),8)*_Wetness*.22;
 result=lerp(result,lerp(_FreshColor.rgb,_FreshAccent.rgb,sheen),a);
 return half4(result,1);
 }
 ENDHLSL
 } }
}
