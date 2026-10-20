Shader "FLOWSTATE/Graffiti/Cartoon Aerosol Particle"
{
    Properties
    {
        _FallbackPaper ("Fallback Paper", Color) = (0.969, 0.949, 0.867, 1)
        _FallbackInk ("Fallback Ink", Color) = (0.031, 0.027, 0.047, 1)
        _FallbackShadow ("Fallback Shadow", Color) = (0.157, 0.071, 0.267, 1)
        _FallbackMid ("Fallback Cyan", Color) = (0.0, 0.735, 0.831, 1)
        _FallbackHighlight ("Fallback Yellow", Color) = (0.961, 0.902, 0.106, 1)
        _FallbackAccent ("Fallback Magenta", Color) = (0.941, 0.094, 0.514, 1)
        _InkWidthPx ("Ink Width (px)", Range(0, 2.5)) = 1.15
        _Softness ("Air Softness", Range(0, 0.5)) = 0.08
        _Breakup ("Broken Ink", Range(0, 0.5)) = 0.08
        _Registration ("Chromatic Registration", Range(0, 0.12)) = 0.03
        _PaperFlecks ("Paper Flecks", Range(0, 0.2)) = 0.025
        _AnimationPhase ("Animation Phase", Float) = 0
        _SprayEnergy ("Spray Energy", Range(0, 1)) = 1
        _Cone ("Cone", Range(0, 1)) = 0.5
        _Style ("Layer Style", Range(0, 3)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+25"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Printed Aerosol"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/07_StylizedRendering/FSSRS/Shaders/Includes/FSSRSCommon.hlsl"
            #include "Assets/07_StylizedRendering/FSSRS/Shaders/Includes/FSSRSPrintPatterns.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FallbackPaper;
                half4 _FallbackInk;
                half4 _FallbackShadow;
                half4 _FallbackMid;
                half4 _FallbackHighlight;
                half4 _FallbackAccent;
                half _InkWidthPx;
                half _Softness;
                half _Breakup;
                half _Registration;
                half _PaperFlecks;
                half _AnimationPhase;
                half _SprayEnergy;
                half _Cone;
                half _Style;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float BlobDistance(float2 sprayPoint, half phase, half style, half energy, half cone)
            {
                float radius = length(sprayPoint);
                float angle = atan2(sprayPoint.y, sprayPoint.x);
                half mist = step(0.5h, style) * (1.0h - step(1.5h, style));
                half droplet = step(1.5h, style) * (1.0h - step(2.5h, style));
                half bloom = step(2.5h, style);
                float firstLobe = sin(angle * lerp(7.0, 9.0, droplet) + phase * 5.1);
                float secondLobe = sin(angle * 13.0 - phase * 3.7 + radius * 3.0);
                float brokenLobe = sin(angle * 3.0 + floor(phase * 4.0) * 1.37);
                float amplitude = lerp(0.018, 0.032, saturate(mist + bloom));
                amplitude *= lerp(0.82, 1.22, energy) * lerp(0.9, 1.18, cone);
                float silhouette = 1.0 + firstLobe * amplitude + secondLobe * amplitude * 0.42;
                silhouette += brokenLobe * amplitude * lerp(0.08, 0.34, bloom);
                return radius - silhouette;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half paletteValidity = step(0.02h,
                    FSSRS_Luminance(_FSSRS_HighlightColor.rgb) + FSSRS_Luminance(_FSSRS_AccentColor.rgb));
                half3 paper = lerp(_FallbackPaper.rgb, _FSSRS_PaperColor.rgb, paletteValidity);
                half3 ink = lerp(_FallbackInk.rgb, _FSSRS_InkColor.rgb, paletteValidity);
                half3 shadow = lerp(_FallbackShadow.rgb, _FSSRS_ShadowColor.rgb, paletteValidity);
                // Keep the Creative Flow cyan/magenta/yellow identity visible while
                // still following the active emotional palette of the level.
                half3 cyan = lerp(_FallbackMid.rgb, _FSSRS_MidColor.rgb, paletteValidity * 0.42h);
                half3 yellow = lerp(_FallbackHighlight.rgb, _FSSRS_HighlightColor.rgb, paletteValidity * 0.72h);
                half3 magenta = lerp(_FallbackAccent.rgb, _FSSRS_AccentColor.rgb, paletteValidity * 0.58h);

                half mistStyle = step(0.5h, _Style) * (1.0h - step(1.5h, _Style));
                half dropletStyle = step(1.5h, _Style) * (1.0h - step(2.5h, _Style));
                half bloomStyle = step(2.5h, _Style);
                half energy = saturate(max(_SprayEnergy, _FSSRS_EmotionEnergy * 0.82h));
                half tickRate = lerp(9.0h, 15.0h, energy);
                half animationTime = _Time.y * lerp(0.9h, 1.55h, energy) + _AnimationPhase;
                half steppedTime = floor(animationTime * tickRate) / tickRate;

                float2 sprayPoint = (input.uv - 0.5) * 2.0;
                float radius = length(sprayPoint);
                float mainDistance = BlobDistance(sprayPoint, steppedTime, _Style, energy, _Cone);
                float antialias = max(fwidth(mainDistance) * 1.08, 0.0015);
                half outerMask = 1.0h - smoothstep(-antialias, antialias, mainDistance);
                half fillMask = 1.0h - smoothstep(
                    -(_InkWidthPx + 0.7h) * antialias,
                    -max(0.0h, _InkWidthPx - 0.7h) * antialias,
                    mainDistance);
                half inkRing = saturate(outerMask - fillMask);

                float2 pixel = input.positionCS.xy;
                half grain = FSSRS_Hash21(floor(pixel / lerp(2.6, 4.2, mistStyle)) +
                    floor(steppedTime * 17.0h) * float2(0.71h, 1.17h));
                half edgeAmount = saturate((radius - 0.24) / 0.76);
                half breakupKeep = smoothstep(_Breakup * edgeAmount,
                    _Breakup * edgeAmount + 0.16h, grain);
                breakupKeep = lerp(1.0h, breakupKeep, saturate(_Breakup * 4.0h));

                half radialDensity = pow(saturate(1.0h - radius * 0.94h), lerp(0.34h, 0.56h, _Softness * 2.0h));
                half bodyMask = lerp(fillMask, outerMask * radialDensity, mistStyle);
                bodyMask *= breakupKeep;
                inkRing *= lerp(breakupKeep, 1.0h, lerp(0.58h, 0.25h, mistStyle));

                float2 registrationOrbit = float2(sin(steppedTime * 3.1h), cos(steppedTime * 2.7h));
                float registration = _Registration * lerp(0.88, 1.22, energy);
                float2 cyanOffset = float2(-0.72, 0.84) * registration + registrationOrbit * registration * 0.08;
                float2 magentaOffset = float2(0.96, -0.58) * registration - registrationOrbit * registration * 0.07;
                float cyanDistance = BlobDistance(sprayPoint + cyanOffset, steppedTime + 0.73h, _Style, energy, _Cone);
                float magentaDistance = BlobDistance(sprayPoint + magentaOffset, steppedTime + 1.61h, _Style, energy, _Cone);
                half cyanMask = 1.0h - smoothstep(-antialias, antialias, cyanDistance);
                half magentaMask = 1.0h - smoothstep(-antialias, antialias, magentaDistance);
                half cyanPlate = saturate(cyanMask - outerMask * 0.86h);
                half magentaPlate = saturate(magentaMask - outerMask * 0.86h);
                half registrationStrength = lerp(0.24h, 0.18h, mistStyle);
                registrationStrength = lerp(registrationStrength, 0.36h, dropletStyle);
                registrationStrength = lerp(registrationStrength, 0.9h, bloomStyle);
                half plateAlpha = saturate(max(cyanPlate, magentaPlate) * registrationStrength);

                half halftone = FSSRS_Halftone(pixel, lerp(4.4h, 5.8h, mistStyle), radialDensity);
                half hatch = FSSRS_Hatch(pixel + steppedTime * 7.0h, 7.0h);
                half printInk = saturate(halftone * mistStyle * 0.13h + hatch * dropletStyle * 0.045h);
                half shadeWave = 0.5h + 0.5h * sin((sprayPoint.x + sprayPoint.y * 0.64h) * 5.2h - steppedTime * 3.4h);
                half3 sprayShadow = lerp(input.color.rgb, shadow, lerp(0.2h, 0.28h, mistStyle));
                half3 fillColor = lerp(sprayShadow, input.color.rgb, 0.62h + shadeWave * 0.2h);
                fillColor = lerp(fillColor, ink, printInk);
                fillColor = lerp(fillColor, yellow, bloomStyle * shadeWave * 0.075h);
                half darkPigment = 1.0h - smoothstep(0.055h, 0.24h, FSSRS_Luminance(input.color.rgb));
                half3 valveChromatic = lerp(cyan, magenta, shadeWave);
                half3 mistChromatic = lerp(shadow, valveChromatic, 0.38h);
                fillColor = lerp(fillColor, mistChromatic, mistStyle * darkPigment * 0.2h);
                fillColor = lerp(fillColor, valveChromatic, bloomStyle * darkPigment * 0.2h);

                half cycle = frac((pixel.x + pixel.y * 1.37h) * 0.007h - steppedTime * 0.32h);
                half3 cyanPlateColor = lerp(cyan, yellow, smoothstep(0.72h, 1.0h, cycle) * 0.16h);
                half3 magentaPlateColor = lerp(magenta, yellow, smoothstep(0.82h, 1.0h, 1.0h - cycle) * 0.12h);
                half plateSum = cyanPlate + magentaPlate;
                half3 plateColor = (cyanPlateColor * cyanPlate + magentaPlateColor * magentaPlate) /
                    max(plateSum, 0.001h);

                half fleckNoise = FSSRS_Hash21(floor(pixel / 2.0h) + floor(steppedTime * 11.0h) * 3.17h);
                half paperFleck = step(1.0h - _PaperFlecks, fleckNoise) * bodyMask;
                half ringBreak = step(0.34h, FSSRS_Hash21(floor(pixel / 3.0h) + floor(steppedTime * 9.0h) * 2.41h));
                inkRing *= lerp(0.28h, 1.0h, ringBreak);
                half ringStrength = 0.32h;
                ringStrength = lerp(ringStrength, 0.1h, mistStyle);
                ringStrength = lerp(ringStrength, 0.72h, dropletStyle);
                ringStrength = lerp(ringStrength, 0.94h, bloomStyle);
                half mainAlpha = saturate(bodyMask + inkRing * ringStrength);
                half alpha = max(mainAlpha, plateAlpha) * input.color.a;

                half3 finalColor = plateColor;
                finalColor = lerp(finalColor, ink, saturate(inkRing * ringStrength));
                finalColor = lerp(finalColor, fillColor, saturate(bodyMask));
                finalColor = lerp(finalColor, paper, paperFleck * 0.82h);

                clip(alpha - 0.004h);
                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor * alpha, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
