Shader "FLOWSTATE/Cinematics/Logo Graffiti Reveal"
{
    Properties
    {
        _BaseMap ("Official Logo", 2D) = "white" {}
        _Reveal ("Paint Reveal", Range(0, 1)) = 0
        _Feather ("Wet Edge Feather", Range(0.005, 0.2)) = 0.055
        _NoiseScale ("Spray Noise Scale", Range(2, 80)) = 34
        _Emission ("Paint Emission", Range(0, 3)) = 0.35
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "GraffitiReveal"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Reveal;
                float _Feather;
                float _NoiseScale;
                float _Emission;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 logo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                // Layered continuous waves create an organic paint edge without
                // the square noise cells that read as digital tiles.
                float broadWave = sin(input.uv.y * 21.0 + input.uv.x * 7.0) * 0.024;
                float fineWave = sin(input.uv.y * 67.0 - input.uv.x * 31.0) * 0.007;
                float diagonalWave = sin(dot(input.uv, float2(83.0, 47.0))) * 0.004;
                float paintFront = input.uv.x + broadWave + fineWave + diagonalWave;
                float threshold = lerp(-0.09, 1.09, _Reveal);
                float revealed = 1.0 - smoothstep(threshold - _Feather, threshold + _Feather, paintFront);
                float wetEdge = saturate(1.0 - abs(paintFront - threshold) / max(_Feather * 2.4, 0.001));
                // The logo's own splatter provides the soft paint texture. Keep
                // the moving reveal front opaque so it never looks like a UI fade.
                float alpha = logo.a * step(0.52, revealed);
                clip(alpha - 0.008);
                logo.rgb *= 1.0 + _Emission + wetEdge * 0.32;
                logo.a = alpha;
                return logo;
            }
            ENDHLSL
        }
    }
}
