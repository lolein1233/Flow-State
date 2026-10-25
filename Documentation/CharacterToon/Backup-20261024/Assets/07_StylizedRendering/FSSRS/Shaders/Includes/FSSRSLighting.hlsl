#ifndef FLOW_STATE_FSSRS_LIGHTING_INCLUDED
#define FLOW_STATE_FSSRS_LIGHTING_INCLUDED

struct FSSRSLightResult
{
    half intensity;
    half3 color;
};

inline void FSSRS_AccumulateLight(
    Light light,
    half3 normalWS,
    inout half brightest,
    inout half3 lightColor)
{
    half attenuation = light.distanceAttenuation * light.shadowAttenuation;
    half amount = saturate(dot(normalWS, light.direction)) * attenuation;
    lightColor += light.color * amount;
    brightest = max(brightest, amount * max(max(light.color.r, light.color.g), light.color.b));
}

inline FSSRSLightResult FSSRS_EvaluateLights(InputData inputData, half4 shadowMask)
{
    FSSRSLightResult result;
    result.intensity = 0.0h;
    result.color = 0.0h;

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    FSSRS_AccumulateLight(mainLight, inputData.normalWS, result.intensity, result.color);

    #if defined(_ADDITIONAL_LIGHTS)
        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
            [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
            {
                FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
                FSSRS_AccumulateLight(light, inputData.normalWS, result.intensity, result.color);
            }
        #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
            FSSRS_AccumulateLight(light, inputData.normalWS, result.intensity, result.color);
        LIGHT_LOOP_END
    #endif

    return result;
}

#endif
