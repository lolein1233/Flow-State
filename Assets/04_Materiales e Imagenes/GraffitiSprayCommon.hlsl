#ifndef FLOWSTATE_GRAFFITI_SPRAY_COMMON_INCLUDED
#define FLOWSTATE_GRAFFITI_SPRAY_COMMON_INCLUDED

float FlowSprayHash21(float2 value)
{
    value = frac(value * float2(123.34, 456.21));
    value += dot(value, value + 45.32);
    return frac(value.x * value.y);
}

half4 FlowEvaluateSpray(float2 uv, float4 color, float4 profile, float4 spray, float globalOpacity)
{
    float2 sprayPoint = (uv - 0.5) * 2.0;
    float radius = length(sprayPoint);
    float coreRadius = profile.x;
    float haloRadius = max(coreRadius + 0.01, profile.y);
    float haloOpacity = profile.z;
    float seed = profile.w;
    float nozzleShape = spray.x;
    float grainDensity = max(8.0, spray.y);
    float coreHardness = saturate(spray.z);
    float instability = saturate(spray.w);

    float2 organicCell = floor((sprayPoint + 1.0) * 11.0);
    float organicNoise = FlowSprayHash21(organicCell + seed * 5.73);
    float warpedRadius = radius * (1.0 + (organicNoise - 0.5) * lerp(0.075, 0.15, 1.0 - coreHardness));
    float antialias = max(fwidth(warpedRadius) * 1.35, lerp(0.085, 0.018, coreHardness));
    float core = 1.0 - smoothstep(coreRadius - antialias, coreRadius + antialias, warpedRadius);
    float haloBand = (1.0 - smoothstep(coreRadius, haloRadius, warpedRadius)) * (1.0 - core);

    float2 grainCell = floor((sprayPoint + 1.0) * grainDensity);
    float grainNoise = FlowSprayHash21(grainCell + seed * 17.13);
    float haloProgress = saturate((haloRadius - warpedRadius) / max(0.01, haloRadius - coreRadius));
    float grainThreshold = lerp(0.86, 0.46, haloProgress) + (organicNoise - 0.5) * 0.12;
    float grain = smoothstep(grainThreshold - 0.08, grainThreshold + 0.08, grainNoise);
    float breakupNoise = FlowSprayHash21(grainCell * 1.71 + seed * 31.7);
    float breakup = lerp(1.0, smoothstep(instability * 0.7, instability * 0.7 + 0.12, breakupNoise), instability);

    float splatter = 0.0;
    if (nozzleShape > 3.5)
    {
        float2 splatterCell = floor((sprayPoint + 1.0) * 18.0);
        float spotNoise = FlowSprayHash21(splatterCell + seed * 7.0);
        splatter = smoothstep(0.76, 0.94, spotNoise) *
            (1.0 - smoothstep(0.25, haloRadius * 1.12, warpedRadius)) * 0.72;
        core *= 0.82 + grainNoise * 0.18;
    }

    float2 dropletGrid = (sprayPoint + 1.0) * lerp(13.0, 21.0, coreHardness);
    float2 dropletCell = floor(dropletGrid);
    float2 dropletUv = frac(dropletGrid) - 0.5;
    float dropletSeed = FlowSprayHash21(dropletCell + seed * 11.9);
    float dropletRadius = lerp(0.08, 0.2, FlowSprayHash21(dropletCell + seed * 19.1));
    float dropletDot = 1.0 - smoothstep(dropletRadius * 0.55, dropletRadius, length(dropletUv));
    float dropletBand = smoothstep(coreRadius * 0.72, coreRadius, warpedRadius) *
        (1.0 - smoothstep(haloRadius, haloRadius * 1.13, warpedRadius));
    float dropletChance = lerp(0.9, 0.76, saturate(haloOpacity + instability * 0.45));
    float microDroplets = step(dropletChance, dropletSeed) * dropletDot * dropletBand;

    float alpha = max(core * breakup, haloBand * haloOpacity * (0.12 + grain * 0.88));
    alpha = max(alpha, splatter);
    alpha = max(alpha, microDroplets * lerp(0.34, 0.72, haloOpacity));
    alpha = saturate(alpha * color.a * globalOpacity);
    clip(alpha - 0.003);
    return half4(color.rgb, alpha);
}
#endif
