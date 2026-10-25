#ifndef FLOW_STATE_FSSRS_PRINT_PATTERNS_INCLUDED
#define FLOW_STATE_FSSRS_PRINT_PATTERNS_INCLUDED

inline half FSSRS_Hatch(float2 pixelPosition, half scale)
{
    float diagonal = (pixelPosition.x + pixelPosition.y * 0.72) / max(scale, 1.0h);
    float stripeDistance = abs(frac(diagonal) - 0.5);
    return 1.0h - smoothstep(0.10h, 0.23h, stripeDistance);
}

inline half FSSRS_CrossHatch(float2 pixelPosition, half scale, half density)
{
    half first = FSSRS_Hatch(pixelPosition, scale);
    half second = FSSRS_Hatch(float2(-pixelPosition.x, pixelPosition.y), scale * 1.17h);
    return saturate(first + second * saturate(density * 1.5h - 0.35h));
}

inline half FSSRS_Halftone(float2 pixelPosition, half scale, half tone)
{
    float2 cell = frac(pixelPosition / max(scale, 1.0h)) - 0.5;
    float radius = lerp(0.10, 0.62, saturate(1.0h - tone));
    float distanceToCenter = length(cell);
    float width = max(fwidth(distanceToCenter), 0.015);
    return 1.0h - smoothstep(radius - width, radius + width, distanceToCenter);
}

#endif
