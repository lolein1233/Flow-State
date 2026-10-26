Shader "FLOWSTATE/Graffiti/Quick Painted Artwork"
{
    Properties
    {
        _MainTex ("Artwork", 2D) = "white" {}
        _UVRect ("Sprite UV rectangle", Vector) = (0,0,1,1)
        _RotateArtwork ("Rotate trimmed artwork", Float) = 0
        _Progress ("Spray progress", Range(0,1)) = 0
        _Opacity ("Opacity", Range(0,1)) = 1
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+20" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _UVRect;
            float4 _MainTex_TexelSize;
            half4 _Tint;
            float _RotateArtwork, _Progress, _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 artUV = _RotateArtwork > 0.5 ? float2(i.uv.y, 1.0-i.uv.x) : i.uv;
                float2 uv = _UVRect.xy + artUV * _UVRect.zw;
                uv = clamp(uv, _UVRect.xy + _MainTex_TexelSize.xy*0.5,
                    _UVRect.xy + _UVRect.zw - _MainTex_TexelSize.xy*0.5);
                half4 art = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Tint;
                float row = min(7.0, floor((1.0-saturate(i.uv.y))*8.0));
                float scan = fmod(row,2.0) < 1.0 ? i.uv.x : 1.0-i.uv.x;
                float noise = frac(sin(dot(floor(i.uv*float2(210,130)),float2(12.9898,78.233)))*43758.5453);
                float order = (row + scan) / 8.0;
                float deposit = smoothstep(order-0.015, order+0.025, _Progress + (noise-0.5)*0.018);
                float pigment = lerp(0.3,1.0,saturate((_Progress-order)*5.0));
                float reveal = _Progress >= 0.9999 ? 1.0 : deposit * pigment;
                art.a *= (_Progress <= 0.0 ? 0.0 : reveal) * _Opacity;
                clip(art.a-0.002);
                return art;
            }
            ENDHLSL
        }
    }
}
