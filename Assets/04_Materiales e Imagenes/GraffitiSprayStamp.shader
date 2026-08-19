Shader "FLOWSTATE/Graffiti/LayeredSprayStamp"
{
    Properties
    {
        _GlobalOpacity ("Global Opacity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Layered Spray Stamp"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 profile : TEXCOORD1;
                float4 spray : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 profile : TEXCOORD1;
                float4 spray : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float _GlobalOpacity;
            CBUFFER_END

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.profile = input.profile;
                output.spray = input.spray;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 sprayPoint = (input.uv - 0.5) * 2.0;
                float radius = length(sprayPoint);

                float coreRadius = input.profile.x;
                float haloRadius = max(coreRadius + 0.01, input.profile.y);
                float haloOpacity = input.profile.z;
                float seed = input.profile.w;

                float nozzleShape = input.spray.x;
                float grainDensity = max(8.0, input.spray.y);
                float coreHardness = saturate(input.spray.z);
                float instability = saturate(input.spray.w);

                float antialias = max(fwidth(radius) * 1.35, lerp(0.085, 0.018, coreHardness));
                float core = 1.0 - smoothstep(coreRadius - antialias, coreRadius + antialias, radius);
                float haloBand = (1.0 - smoothstep(coreRadius, haloRadius, radius)) * (1.0 - core);

                float2 grainCell = floor((sprayPoint + 1.0) * grainDensity);
                float grainNoise = Hash21(grainCell + seed * 17.13);
                float haloProgress = saturate((haloRadius - radius) / max(0.01, haloRadius - coreRadius));
                float grainThreshold = lerp(0.82, 0.48, haloProgress);
                float grain = smoothstep(grainThreshold - 0.08, grainThreshold + 0.08, grainNoise);

                float breakupNoise = Hash21(grainCell * 1.71 + seed * 31.7);
                float breakup = lerp(1.0, smoothstep(instability * 0.7, instability * 0.7 + 0.12, breakupNoise), instability);

                float splatter = 0.0;
                if (nozzleShape > 3.5)
                {
                    float2 splatterCell = floor((sprayPoint + 1.0) * 18.0);
                    float spotNoise = Hash21(splatterCell + seed * 7.0);
                    float spot = smoothstep(0.76, 0.94, spotNoise);
                    splatter = spot * (1.0 - smoothstep(0.25, haloRadius * 1.12, radius)) * 0.72;
                    core *= 0.82 + grainNoise * 0.18;
                }

                float alpha = max(core * breakup, haloBand * haloOpacity * (0.12 + grain * 0.88));
                alpha = max(alpha, splatter);
                alpha = saturate(alpha * input.color.a * _GlobalOpacity);

                clip(alpha - 0.003);
                return half4(input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
