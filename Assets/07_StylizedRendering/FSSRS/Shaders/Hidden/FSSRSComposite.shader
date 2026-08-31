Shader "Hidden/FLOWSTATE/FSSRS Composite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "FSSRS Composite"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Assets/07_StylizedRendering/FSSRS/Shaders/Includes/FSSRSCommon.hlsl"
            #include "Assets/07_StylizedRendering/FSSRS/Shaders/Includes/FSSRSPrintPatterns.hlsl"

            half4 _OutlineColor;
            half _OutlineIntensity;
            half _OutlineThickness;
            half _DepthThreshold;
            half _NormalThreshold;
            half _LumaThreshold;
            half _PosterizeSteps;
            half _GrainIntensity;
            half _HalftoneIntensity;
            half _HalftoneScale;
            half _HatchIntensity;
            half _FSSRS_PaletteInfluence;
            int _DebugMode;

            half3 SampleColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel).rgb;
            }

            float LinearDepthAt(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                float2 texel = _BlitTexture_TexelSize.xy * _OutlineThickness;
                half3 source = SampleColor(uv);
                float centerDepth = LinearDepthAt(uv);
                half3 centerNormal = SampleSceneNormals(uv);
                half centerLuma = FSSRS_Luminance(source);

                float depthEdge = 0;
                half normalEdge = 0;
                half lumaEdge = 0;

                const float2 directions[4] =
                {
                    float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1)
                };

                [unroll] for (int index = 0; index < 4; index++)
                {
                    float2 sampleUV = saturate(uv + directions[index] * texel);
                    float sampleDepth = LinearDepthAt(sampleUV);
                    half3 sampleNormal = SampleSceneNormals(sampleUV);
                    half sampleLuma = FSSRS_Luminance(SampleColor(sampleUV));

                    depthEdge = max(depthEdge, abs(sampleDepth - centerDepth) / max(centerDepth, 0.1));
                    normalEdge = max(normalEdge, 1.0h - saturate(dot(centerNormal, sampleNormal)));
                    lumaEdge = max(lumaEdge, abs(sampleLuma - centerLuma));
                }

                half depthMask = smoothstep(_DepthThreshold, _DepthThreshold * 2.0h, depthEdge);
                half normalMask = smoothstep(_NormalThreshold, _NormalThreshold * 1.75h, normalEdge);
                half lumaMask = smoothstep(_LumaThreshold, _LumaThreshold * 1.75h, lumaEdge);
                half edge = saturate(depthMask + max(normalMask, lumaMask * 0.35h) * 0.58h);

                half3 result = source;
                if (_PosterizeSteps >= 2.0h)
                {
                    half quantizedLuma = FSSRS_Quantize(centerLuma, _PosterizeSteps);
                    half posterizedRatio = quantizedLuma / max(centerLuma, 0.035h);
                    result *= lerp(1.0h, posterizedRatio, 0.58h);
                }

                half paletteBand = FSSRS_Quantize(centerLuma, 4.0h);
                half3 paletteColor = FSSRS_PaletteRamp(
                    paletteBand,
                    _FSSRS_ShadowColor.rgb,
                    _FSSRS_MidColor.rgb,
                    _FSSRS_HighlightColor.rgb);
                half paletteValidity = step(0.02h, FSSRS_Luminance(_FSSRS_HighlightColor.rgb));
                half paletteLuma = max(FSSRS_Luminance(paletteColor), 0.2h);
                result = lerp(result, result * paletteColor / paletteLuma,
                    _FSSRS_PaletteInfluence * paletteValidity * 0.45h);

                float2 pixelPosition = uv * _ScreenParams.xy;
                half dots = FSSRS_Halftone(pixelPosition, _HalftoneScale, centerLuma);
                half dotMask = dots * saturate(1.0h - abs(centerLuma - 0.45h) * 2.2h) * _HalftoneIntensity;
                half hatch = FSSRS_CrossHatch(pixelPosition, _HalftoneScale * 1.35h, 1.0h - centerLuma);
                half hatchMask = hatch * saturate(0.58h - centerLuma) * _HatchIntensity;
                result = lerp(result, _FSSRS_InkColor.rgb, saturate(dotMask + hatchMask));

                half grain = (FSSRS_Hash21(floor(pixelPosition)) - 0.5h) * _GrainIntensity;
                result += grain;
                result = lerp(result, _OutlineColor.rgb, edge * _OutlineIntensity * _OutlineColor.a);

                if (_DebugMode == 1)
                    result = centerDepth.xxx / max(centerDepth + 5.0, 0.001);
                else if (_DebugMode == 2)
                    result = centerNormal * 0.5h + 0.5h;
                else if (_DebugMode == 3)
                    result = half3(depthMask, normalMask, lumaMask);
                else if (_DebugMode == 4)
                    result = paletteBand.xxx;

                return half4(max(result, 0.0h), 1.0h);
            }
            ENDHLSL
        }
    }
}
