Shader "FLOWSTATE/FSSRS/Stylized Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0

        [Header(Stylized Lighting)]
        _BandCount("Light Bands", Range(2, 6)) = 4
        _ShadowColor("Local Shadow", Color) = (0.12, 0.17, 0.24, 1)
        _MidColor("Local Mid", Color) = (0.48, 0.65, 0.68, 1)
        _HighlightColor("Local Highlight", Color) = (1, 0.92, 0.68, 1)
        _PaletteInfluence("Global Palette Influence", Range(0, 1)) = 0.65
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.35
        _LightColorStrength("Light Color Strength", Range(0, 1)) = 0.35
        _RimColor("Rim Color", Color) = (0.15, 0.75, 0.9, 1)
        _RimStrength("Rim Strength", Range(0, 2)) = 0.2
        _RimPower("Rim Power", Range(0.5, 8)) = 3

        [Header(Print Surface)]
        _InkTexture("Ink Breakup", 2D) = "white" {}
        _HatchTexture("Hatch Texture", 2D) = "white" {}
        _InkTiling("Ink Tiling", Range(0.02, 4)) = 0.35
        _InkBreakup("Ink Breakup", Range(0, 1)) = 0.12
        _HatchStrength("Hatching", Range(0, 1)) = 0.2
        _HatchScale("Hatch Scale", Range(2, 24)) = 8
        _HalftoneStrength("Halftone", Range(0, 1)) = 0.08
        _HalftoneScale("Halftone Scale", Range(2, 16)) = 5
        [HDR] _EmissionColor("Emission", Color) = (0, 0, 0, 0)

        [HideInInspector] _Surface("Surface", Float) = 0
        [HideInInspector] _Blend("Blend", Float) = 0
        [HideInInspector] _Cull("Cull", Float) = 2
        [HideInInspector] _ZWrite("ZWrite", Float) = 1
        [HideInInspector] _Smoothness("Smoothness", Range(0, 1)) = 0.2
        [HideInInspector] _Metallic("Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "FSSRSForward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend One Zero
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex FSSRSVertex
            #pragma fragment FSSRSFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Assets/07_StylizedRendering/FSSRS/Shaders/Includes/FSSRSCommon.hlsl"
            #include "Assets/07_StylizedRendering/FSSRS/Shaders/Includes/FSSRSLighting.hlsl"
            #include "Assets/07_StylizedRendering/FSSRS/Shaders/Includes/FSSRSPrintPatterns.hlsl"

            TEXTURE2D(_InkTexture);
            SAMPLER(sampler_InkTexture);
            TEXTURE2D(_HatchTexture);
            SAMPLER(sampler_HatchTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _MidColor;
                half4 _HighlightColor;
                half4 _RimColor;
                half4 _EmissionColor;
                half _BumpScale;
                half _Cutoff;
                half _BandCount;
                half _PaletteInfluence;
                half _AmbientStrength;
                half _LightColorStrength;
                half _RimStrength;
                half _RimPower;
                half _InkTiling;
                half _InkBreakup;
                half _HatchStrength;
                half _HatchScale;
                half _HalftoneStrength;
                half _HalftoneScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings FSSRSVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 FSSRSFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(baseSample.a - _Cutoff);
                #endif

                half3 normalWS = normalize(input.normalWS);
                #if defined(_NORMALMAP)
                    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                    half3 bitangentWS = input.tangentWS.w * cross(normalWS, input.tangentWS.xyz);
                    normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS, normalWS));
                    normalWS = NormalizeNormalPerPixel(normalWS);
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseSample.rgb;
                surfaceData.alpha = baseSample.a;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;
                #if defined(_DBUFFER)
                    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
                    baseSample.rgb = surfaceData.albedo;
                    normalWS = inputData.normalWS;
                #endif

                FSSRSLightResult lighting = FSSRS_EvaluateLights(inputData, inputData.shadowMask);
                half ambient = saturate(FSSRS_Luminance(inputData.bakedGI) * _AmbientStrength);
                half band = FSSRS_Quantize(max(lighting.intensity, ambient), _BandCount);

                half globalPaletteValidity = step(0.02h, FSSRS_Luminance(_FSSRS_HighlightColor.rgb));
                half paletteMix = saturate(_PaletteInfluence) * globalPaletteValidity;
                half3 shadowColor = lerp(_ShadowColor.rgb, _FSSRS_ShadowColor.rgb, paletteMix);
                half3 midColor = lerp(_MidColor.rgb, _FSSRS_MidColor.rgb, paletteMix);
                half3 highlightColor = lerp(_HighlightColor.rgb, _FSSRS_HighlightColor.rgb, paletteMix);
                half3 ramp = FSSRS_PaletteRamp(band, shadowColor, midColor, highlightColor);

                half3 lightTint = lerp(1.0h, saturate(lighting.color + inputData.bakedGI), _LightColorStrength);
                half valueRamp = lerp(0.46h, 1.12h, band);
                half rampLuma = max(FSSRS_Luminance(ramp), 0.2h);
                half3 chromaRamp = lerp(1.0h, ramp / rampLuma, paletteMix * 0.48h);
                half3 color = baseSample.rgb * valueRamp * chromaRamp * lightTint;
                color += baseSample.rgb * inputData.bakedGI * 0.12h;

                float2 pixelPosition = input.positionCS.xy;
                half hatch = FSSRS_CrossHatch(pixelPosition, _HatchScale, 1.0h - band);
                half hatchTexture = SAMPLE_TEXTURE2D(_HatchTexture, sampler_HatchTexture,
                    input.positionWS.xz * _InkTiling).r;
                half hatchAmount = hatch * hatchTexture * (1.0h - band) * _HatchStrength;

                half dots = FSSRS_Halftone(pixelPosition, _HalftoneScale, band);
                half dotAmount = dots * saturate(1.0h - abs(band - 0.5h) * 2.0h) * _HalftoneStrength;

                float2 inkUV = (input.positionWS.xz + input.positionWS.xy * 0.37) * _InkTiling;
                half inkTexture = SAMPLE_TEXTURE2D(_InkTexture, sampler_InkTexture, inkUV).r;
                half breakup = lerp(1.0h, lerp(0.72h, 1.08h, inkTexture), _InkBreakup);
                color = lerp(color, _FSSRS_InkColor.rgb, saturate(hatchAmount + dotAmount));
                color *= breakup;

                half rim = pow(saturate(1.0h - dot(normalWS, inputData.viewDirectionWS)), _RimPower) * _RimStrength;
                color += _RimColor.rgb * rim + _EmissionColor.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color, baseSample.a);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "FlowState.Rendering.Editor.FlowStylizedShaderGUI"
}
