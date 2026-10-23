Shader "FLOWSTATE/FSSRS/Player Ground Contact"
{
    Properties
    {
        _CoreColor ("Ink Core", Color) = (0.01, 0.0, 0.02, 0.76)
        _AccentColor ("Cartoon Accent", Color) = (0.82, 0.02, 0.24, 0.30)
        _Opacity ("Opacity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-20"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "GroundContact"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _AccentColor;
                half _Opacity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = (input.uv - 0.5) * 2.0;
                float radius = length(centered);
                float body = 1.0 - smoothstep(0.24, 1.0, radius);

                // Tres escalones suaves mantienen el contacto legible con la estética comic.
                float toonBody = floor(saturate(body) * 3.999) / 3.0;
                toonBody = lerp(body, toonBody, 0.42);

                float ringOuter = 1.0 - smoothstep(0.76, 0.98, radius);
                float ringInner = 1.0 - smoothstep(0.55, 0.74, radius);
                float ring = saturate(ringOuter - ringInner);

                float2 halftoneCell = floor(input.uv * float2(24.0, 30.0));
                float halftone = fmod(halftoneCell.x + halftoneCell.y, 2.0);
                ring *= lerp(0.36, 0.9, halftone);

                half3 color = lerp(_CoreColor.rgb, _AccentColor.rgb, saturate(ring * 1.5));
                half alpha = saturate(toonBody * _CoreColor.a + ring * _AccentColor.a) * _Opacity;
                clip(alpha - 0.004);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
