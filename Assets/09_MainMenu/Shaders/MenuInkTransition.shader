Shader "FLOWSTATE/Menu/Ink Transition"
{
 Properties
 {
  _Progress("Paint invasion",Range(0,1))=0
  _Color("Deep pigment",Color)=(.03,.02,.05,1)
  _AccentColor("Wet edge",Color)=(1,.45,.04,1)
  _Seed("Spray seed",Float)=17
  _BackdropMask("Monumental mask",2D)="black"{}
  _LoadingIconAtlas("Animated spray mask",2D)="black"{}
  [HideInInspector] _LoadingStartTime("Loading animation start",Float)=0
  _LoadingMode("Loading composition",Range(0,1))=0
  _LoadProgress("Scene load",Range(0,1))=0
 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Overlay+100" "RenderType"="Transparent"} Pass {
 ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 TEXTURE2D(_BackdropMask); SAMPLER(sampler_BackdropMask);
 TEXTURE2D(_LoadingIconAtlas); SAMPLER(sampler_LoadingIconAtlas);
 CBUFFER_START(UnityPerMaterial)
 float _Progress;float4 _Color;float4 _AccentColor;float _Seed;
 float _LoadingMode;float _LoadProgress;float _LoadingStartTime;float4 _BackdropParallax;
 CBUFFER_END
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise21(float2 p)
 {
  float2 id=floor(p),f=frac(p);f=f*f*(3-2*f);
  return lerp(lerp(hash21(id),hash21(id+float2(1,0)),f.x),lerp(hash21(id+float2(0,1)),hash21(id+1),f.x),f.y);
 }
 float3 FoilSpectrum(float phase)
 {
  float3 spectrum=.5+.5*cos(6.2831853*(phase+float3(0,.67,.33)));
  return pow(saturate(spectrum),1.3);
 }
 float4 SampleLoadingIcon(float2 uv,float screenAspect)
 {
  float animationTime=max(0,_Time.y-_LoadingStartTime);
  float bob=sin(animationTime*1.85)*.006;
  float pulse=1+sin(animationTime*2.35)*.012;
  float2 iconUV=(uv-float2(.5,.57+bob))*float2(screenAspect,1)/(.23*pulse)+.5;
  float inside=step(0,iconUV.x)*step(iconUV.x,1)*step(0,iconUV.y)*step(iconUV.y,1);
  float frame=floor(fmod(animationTime*30,81));
  float2 cell=float2(fmod(frame,9),8-floor(frame/9));
  // Half-texel padding keeps bilinear filtering inside the selected atlas cell.
  float2 atlasUV=(cell+lerp(.004,.996,saturate(iconUV)))/9;
  float4 icon=SAMPLE_TEXTURE2D(_LoadingIconAtlas,sampler_LoadingIconAtlas,atlasUV);
  icon.a*=inside;
  return icon;
 }
 half4 frag(V i):SV_Target{
  float screenAspect=max(1,_ScreenParams.x/max(1,_ScreenParams.y));
  // The transition quad is deliberately oversized (12 x 4). This maps its
  // visible center back to stable 0-1 screen coordinates at every aspect.
  float2 uv=(i.uv-.5)*float2(6/screenAspect,2)+.5;

  if(_LoadingMode>.001)
  {
   float2 parallax=_BackdropParallax.xy*float2(.006,.004);
   float strip=floor(uv.y*112);
   float tick=floor(_Time.y*8);
   float glitchGate=step(.94,hash21(float2(strip,tick)));
   float glitch=(hash21(float2(strip+19,tick))-.5)*.03*glitchGate;
   float2 maskUV=uv+parallax+float2(glitch,0);
   float3 plate=SAMPLE_TEXTURE2D(_BackdropMask,sampler_BackdropMask,maskUV).rgb;
   float luma=dot(plate,float3(.299,.587,.114));
   float maskEnergy=smoothstep(.035,.82,luma);
   float foilPhase=dot(maskUV,float2(1.3,.86))+dot(_BackdropParallax.xy,float2(.25,-.18))-_Time.y*.055;
   float3 foil=FoilSpectrum(foilPhase);
   float band=pow(saturate(.5+.5*sin(foilPhase*6.2831853)),1.7);
   float3 mono=luma.xxx*.34+float3(.006,.008,.014);
   mono=lerp(mono,foil*(.1+luma*.31),maskEnergy*band*.28);
   float scan=.5+.5*sin((uv.y+_Time.y*.025)*620);
   mono+=foil*(maskEnergy*scan*.025);
   mono*=1-pow(saturate(length((uv-.5)*float2(1.25,.9))),2)*.3;

   float3 uiFoil=lerp(float3(.86,.89,.95),FoilSpectrum(_Time.y*.12+uv.x*.8),.4);
   float4 icon=SampleLoadingIcon(uv,screenAspect);
   float iconLuma=dot(icon.rgb,float3(.299,.587,.114));
   float3 iconFoil=FoilSpectrum(_Time.y*.16+uv.x*.9-uv.y*.45);
   float3 iconColor=lerp(icon.rgb,iconFoil,.14*iconLuma);
   iconColor=lerp(iconColor,uiFoil,.07*iconLuma);
   float3 loadingColor=lerp(mono,iconColor*1.08,saturate(icon.a));
   loadingColor+=iconFoil*icon.a*iconLuma*.06;

   float2 barP=(uv-float2(.5,.326))*float2(screenAspect,1);
   float barTrack=step(abs(barP.x),.29)*step(abs(barP.y),.012);
   float fillEdge=lerp(-.29,.29,saturate(_LoadProgress));
   float barFill=barTrack*step(barP.x,fillEdge);
   float barSheen=barFill*pow(saturate(1-abs(barP.x-fillEdge)/.04),2);
   loadingColor=lerp(loadingColor,float3(.04,.045,.06),barTrack*.94);
   loadingColor+=uiFoil*(barFill*.8+barSheen*.5);
   float marker=(1-smoothstep(.002,.006,abs(barP.x-fillEdge)))*step(abs(barP.y),.018)*step(.002,_LoadProgress);
   loadingColor+=uiFoil*marker*.72;
   return half4(loadingColor,1);
  }

  // Outside the loading composition the overlay is a neutral cinematic fade.
  // Keeping this pure black avoids the former orange pigment flash both before
  // the loading screen and when gameplay is revealed.
  float fade=smoothstep(0,1,saturate(_Progress));
  return half4(0,0,0,fade);
 }
 ENDHLSL
 } }
}
