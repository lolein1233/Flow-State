Shader "FLOWSTATE/Menu/Paint Composite"
{
 Properties { _MainTex("History",2D)="black"{} _StampTex("OWNED stencil",2D)="black"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline"} Pass {
 ZWrite Off ZTest Always Cull Off
 HLSLPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex,_StampTex;
 float4 _Rect,_PaintColor,_Accent;
 float _Angle,_Seed,_Reveal,_Grain,_Drips,_Retention;
 float hash(float2 p){p=frac(p*float2(123.34,456.21));p+=dot(p,p+45.32);return frac(p.x*p.y);}
 float4 frag(v2f_img i):SV_Target
 {
   float4 old=tex2D(_MainTex,i.uv);
   // Slow pigment erosion keeps saturation bounded without clearing history.
   old*= _Retention;
   float2 d=i.uv-_Rect.xy;
   float2 p=float2(d.x*cos(_Angle)-d.y*sin(_Angle),d.x*sin(_Angle)+d.y*cos(_Angle))/_Rect.zw+.5;
   float inside=step(0,p.x)*step(p.x,1)*step(0,p.y)*step(p.y,1);
   float4 mask=tex2D(_StampTex,p)*inside;
   float noise=hash(floor(p*float2(1100,360))+_Seed);
   float edge=hash(floor(p.y*70)+_Seed)*.045;
   float reveal=1-smoothstep(_Reveal*1.18-.09,_Reveal*1.18+.02,p.x+edge);
   reveal*=step(.001,_Reveal);
   float glyph=mask.r*lerp(1,step(_Grain*.64,noise),.78);
   float halo=mask.g*step(.7,noise)*.42;
   float drip=mask.b*_Drips*smoothstep(.15,.9,_Reveal);
   float a=saturate(max(glyph,max(halo,drip))*reveal);
   float2 shadowP=p+float2(-.01,.023);
   float border=tex2D(_StampTex,shadowP).r*inside*reveal*.9;
   // Fresh light underpainting protects letter contours against historical overdraw.
   float primer=mask.g*reveal*.94;
   old=float4(float3(.85,.83,.76)*primer,primer)+old*(1-primer);
   float4 under=float4(_Accent.rgb*border,border);
   old=under+old*(1-border);
   float sheen=pow(saturate(1-abs(p.x-_Reveal+.08)*14),4)*.19*(1-_Reveal);
   float3 ink=lerp(_PaintColor.rgb,_Accent.rgb,sheen);
   return float4(ink*a,a)+old*(1-a);
 }
 ENDHLSL
 } }
}
