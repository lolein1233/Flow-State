#ifndef FLOW_STATE_FSSRS_COMMON_INCLUDED
#define FLOW_STATE_FSSRS_COMMON_INCLUDED

half4 _FSSRS_PaperColor;
half4 _FSSRS_InkColor;
half4 _FSSRS_ShadowColor;
half4 _FSSRS_MidColor;
half4 _FSSRS_HighlightColor;
half4 _FSSRS_AccentColor;

inline half FSSRS_Luminance(half3 color)
{
    return dot(color, half3(0.2126h, 0.7152h, 0.0722h));
}

inline half FSSRS_Quantize(half value, half steps)
{
    steps = max(steps, 2.0h);
    return floor(saturate(value) * (steps - 1.0h) + 0.5h) / (steps - 1.0h);
}

inline half3 FSSRS_PaletteRamp(half value, half3 shadowColor, half3 midColor, half3 highlightColor)
{
    half shadowToMid = saturate(value * 2.0h);
    half midToHighlight = saturate(value * 2.0h - 1.0h);
    return lerp(lerp(shadowColor, midColor, shadowToMid), highlightColor, midToHighlight);
}

inline float FSSRS_Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

#endif
