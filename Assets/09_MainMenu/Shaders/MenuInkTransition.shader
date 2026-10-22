Shader "FLOWSTATE/Menu/Ink Transition"
{
 Properties
 {
  _Progress("Paint invasion",Range(0,1))=0
  _Color("Deep pigment",Color)=(.03,.02,.05,1)
  _AccentColor("Wet edge",Color)=(1,.45,.04,1)
  _Seed("Spray seed",Float)=17
  _BackdropMask("Monumental mask",2D)="black"{}
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
 CBUFFER_START(UnityPerMaterial)
 float _Progress;float4 _Color;float4 _AccentColor;float _Seed;
 float _LoadingMode;float _LoadProgress;float4 _BackdropParallax;
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
 float2 Rotate(float2 p,float angle)
 {
  float s=sin(angle),c=cos(angle);
  return float2(c*p.x-s*p.y,s*p.x+c*p.y);
 }
 float SegmentDistance(float2 p,float2 a,float2 b)
 {
  float2 pa=p-a,ba=b-a;
  float h=saturate(dot(pa,ba)/dot(ba,ba));
  return length(pa-ba*h);
 }
 float Hourglass(float2 uv,out float softGlow)
 {
  float aspect=max(1,_ScreenParams.x/max(1,_ScreenParams.y));
  float2 p=(uv-float2(.5,.565))*float2(aspect,1);
  float flipClock=_Time.y*.56;
  float flipCycle=frac(flipClock);
  float flip=(floor(flipClock)+smoothstep(.7,1,flipCycle))*3.14159265;
  p=Rotate(p,flip);
  float w=.105,h=.14;
  float frame=1;
  frame=min(frame,SegmentDistance(p,float2(-w,h),float2(w,h)));
  frame=min(frame,SegmentDistance(p,float2(-w,-h),float2(w,-h)));
  frame=min(frame,SegmentDistance(p,float2(-w,h),float2(w,-h)));
  frame=min(frame,SegmentDistance(p,float2(w,h),float2(-w,-h)));
  float frameLine=1-smoothstep(.004,.009,frame);
  float cycle=frac(_Time.y*.56);
  float upperWidth=max(0,p.y/h)*w*.82;
  float lowerWidth=max(0,-p.y/h)*w*.82;
  float upper=step(0,p.y)*step(p.y,h)*step(abs(p.x),upperWidth)*step(p.y,lerp(h*.88,.018,cycle));
  float lower=step(p.y,0)*step(-h,p.y)*step(abs(p.x),lowerWidth)*step(p.y,lerp(-h*.88,-.012,cycle));
  float stream=(1-smoothstep(.002,.006,abs(p.x)))*step(-h*.72,p.y)*step(p.y,.018)*step(cycle,.92);
  float sand=saturate(max(max(upper,lower),stream));
  softGlow=1-smoothstep(.01,.036,frame);
  return saturate(frameLine+sand*.82);
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

   float hourglassGlow;
   float hourglass=Hourglass(uv,hourglassGlow);
   float3 uiFoil=lerp(float3(.86,.89,.95),FoilSpectrum(_Time.y*.12+uv.x*.8),.4);
   float3 loadingColor=mono+uiFoil*(hourglass*.96+hourglassGlow*.12);

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

  float coarse=noise21(uv*9+_Seed);
  float2 fromNozzle=(uv-float2(.84,.29))*float2(1.78,1);
  float distortedRadius=length(fromNozzle)+(coarse-.5)*.28;
  float front=_Progress*2.16-.09;
  float body=1-smoothstep(front-.08,front+.055,distortedRadius);
  float diagonal=_Progress*1.1-(1-uv.x)*.72-abs(uv.y-.29)*.24+(coarse-.5)*.15;
  float streak=smoothstep(-.035,.07,diagonal)*smoothstep(.02,.16,uv.x);
  float alpha=saturate(max(body,streak*.72))*step(.001,_Progress);
  float edge=saturate(1-abs(distortedRadius-front)/.07)*alpha;
  float wet=noise21(uv*18-_Seed)*.05+edge*.68;
  float3 pigment=lerp(_Color.rgb,_AccentColor.rgb,saturate(wet));
  return half4(pigment,alpha);
 }
 ENDHLSL
 } }
}
