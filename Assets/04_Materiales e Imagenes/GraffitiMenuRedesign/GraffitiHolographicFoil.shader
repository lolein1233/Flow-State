Shader "FLOWSTATE/Graffiti/HolographicFoil"
{
    Properties
    {
        _BaseColor ("Base", Color) = (0.075, 0.085, 0.10, 1)
        _CoolTint ("Reflejo frio", Color) = (0.30, 0.48, 0.50, 1)
        _WarmTint ("Reflejo calido", Color) = (0.64, 0.48, 0.29, 1)
        _Intensity ("Intensidad holografica", Range(0,1)) = 0.35
        _Speed ("Velocidad del reflejo", Range(0,2)) = 0.25
        _HalftoneStrength ("Intensidad de puntos graffiti", Range(0,0.6)) = 0.28
        _HalftoneScale ("Tamano de trama de puntos", Range(4,24)) = 12
        [Enum(Fondo,0,Boquilla,1,Proyeccion,2)] _SurfaceMode ("Superficie", Float) = 0
        [HideInInspector] _Highlight ("Seleccion", Float) = 0
        [HideInInspector] _Phase ("Desfase", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Foil"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/07_StylizedRendering/FSSRS/Shaders/Includes/FSSRSPrintPatterns.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _CoolTint, _WarmTint;
                float _Intensity, _Speed, _SurfaceMode, _Highlight, _Phase;
                float _HalftoneStrength, _HalftoneScale;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed + _Phase;
                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float facing = saturate(dot(n, v));
                float rim = pow(1.0 - facing, 3.0);
                float3 color;
                if (_SurfaceMode > 1.5)
                {
                    float2 p = (i.uv - 0.5) * float2(1.0, 1.1);
                    float radius = length(p);
                    float angle = atan2(p.y, p.x);
                    float aa = max(fwidth(radius), 0.002);
                    float ring = 1.0 - smoothstep(0.003, 0.003 + aa, abs(radius - 0.42));
                    float innerRing = 1.0 - smoothstep(0.0015, 0.0015 + aa, abs(radius - 0.36));
                    float arc = smoothstep(-0.3, 0.2, sin(angle * 2.0 + t));
                    float3 tint = lerp(_CoolTint.rgb, _WarmTint.rgb, _Highlight);
                    color = _BaseColor.rgb * 0.42;
                    color += tint * (ring * arc + innerRing * 0.22) * (0.22 + _Highlight * 0.4);
                    color += tint * pow(saturate(1.0 - radius * 2.0), 3.0) * _Highlight * 0.08;
                }
                else if (_SurfaceMode > 0.5)
                {
                    // A slowly travelling studio reflection, not flashing emission.
                    float3 lightDirection = normalize(float3(sin(t) * 0.65, 0.8, cos(t) * 0.65));
                    float diffuse = saturate(dot(n, lightDirection)) * 0.65 + 0.35;
                    float specular = pow(saturate(dot(n, normalize(lightDirection + v))), 28.0);
                    float hue = 0.5 + 0.5 * sin(dot(n, float3(2.3, 1.7, 1.1)) + t);
                    float3 foil = lerp(_CoolTint.rgb, _WarmTint.rgb, hue);
                    color = _BaseColor.rgb * diffuse + foil * (_Intensity * (0.22 + rim * 0.8));
                    color += specular * (0.24 + _Highlight * 0.22);
                    color += _WarmTint.rgb * _Highlight * (0.06 + rim * 0.18);
                }
                else
                {
                    float2 uv = i.uv;
                    float wave = uv.x * 4.0 + uv.y * 2.1 + sin(uv.y * 5.0 + t * 0.45) * 1.3;
                    float sheen = pow(0.5 + 0.5 * sin(wave - t), 5.0);
                    float hue = 0.5 + 0.5 * sin(wave * 0.75 + t * 0.35 + facing);
                    float3 foil = lerp(_CoolTint.rgb, _WarmTint.rgb, hue);
                    // Fine etched curves are restricted to the edge, away from the labels.
                    float contour = abs(sin(wave * 6.0));
                    float etched = 1.0 - smoothstep(0.015, 0.015 + max(fwidth(contour), 0.025), contour);
                    float edgeArea = smoothstep(0.5, 0.95, uv.x) * smoothstep(0.3, 0.85, uv.y);
                    float borderDistance = min(min(uv.x, 1.0-uv.x), min(uv.y, 1.0-uv.y));
                    float border = 1.0 - smoothstep(0.003, 0.009, borderDistance);
                    float sweepPosition = frac(t * 0.11) * 1.6 - 0.3;
                    float sweep = 1.0 - smoothstep(0.0, 0.06, abs(uv.y - sweepPosition));
                    color = _BaseColor.rgb + foil * _Intensity * (0.12 + sheen * 0.65);
                    color += foil * _Intensity * (etched * edgeArea * 0.28 + border * 0.6 + sweep * 0.12);
                    // Reuse the project's comic print dots, anchored to the menu rather
                    // than the screen so the pattern stays still when the player looks.
                    float dots = FSSRS_Halftone(uv * float2(1200.0, 660.0), _HalftoneScale, 0.72);
                    float printMask = lerp(0.45, 1.0, smoothstep(0.15, 0.48, abs(uv.x - 0.5)));
                    color = lerp(color, color * 0.18, dots * _HalftoneStrength * printMask);
                }
                return half4(saturate(color), 1);
            }
            ENDHLSL
        }
    }
}
