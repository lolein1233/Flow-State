Shader "FLOWSTATE/UI/HolographicSprayCan"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _InkColor ("Ink", Color) = (0.025,0.018,0.045,1)
        _ShadowTint ("Printed Shadow", Color) = (0.22,0.08,0.24,1)
        _CoolTint ("Holographic Cool", Color) = (0.02,0.8,1,1)
        _WarmTint ("Holographic Warm", Color) = (1,0.08,0.48,1)
        _AcidTint ("Holographic Acid", Color) = (0.82,0.98,0.08,1)
        _HoloIntensity ("Holographic Intensity", Range(0,2)) = 0.85
        _PrintStrength ("Print Strength", Range(0,1)) = 0.18
        _HalftoneScale ("Halftone Scale", Range(3,18)) = 8
        _Motion ("Motion", Range(0,2)) = 0
        _Alert ("Low Paint Alert", Range(0,1)) = 0
        _Phase ("Phase", Float) = 0
        _Role ("Role", Range(0,2)) = 0
        _EchoAlpha ("Echo Alpha", Range(0,1)) = 0.42

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "HolographicCan"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float4 _InkColor;
            float4 _ShadowTint;
            float4 _CoolTint;
            float4 _WarmTint;
            float4 _AcidTint;
            float4 _ClipRect;
            float _HoloIntensity;
            float _PrintStrength;
            float _HalftoneScale;
            float _Motion;
            float _Alert;
            float _Phase;
            float _Role;
            float _EchoAlpha;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.localPosition = input.positionOS;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            float3 HolographicPalette(float phase)
            {
                float coolToWarm = 0.5 + 0.5 * sin(phase);
                float warmToAcid = saturate(0.5 + 0.5 * sin(phase + 2.094));
                return lerp(lerp(_CoolTint.rgb, _WarmTint.rgb, coolToWarm), _AcidTint.rgb, warmToAcid * 0.42);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = tex2D(_MainTex, input.uv) * input.color;
                float alpha = sprite.a;

                float2 texel = _MainTex_TexelSize.xy * 1.35;
                float neighbourAlpha = min(
                    min(tex2D(_MainTex, input.uv + float2(texel.x, 0)).a, tex2D(_MainTex, input.uv - float2(texel.x, 0)).a),
                    min(tex2D(_MainTex, input.uv + float2(0, texel.y)).a, tex2D(_MainTex, input.uv - float2(0, texel.y)).a));
                float innerRim = saturate((alpha - neighbourAlpha) * 4.5);

                float time = _Time.y * (0.38 + _Motion * 0.2) + _Phase;
                float diagonal = input.uv.x * 0.72 + input.uv.y;
                float sweepDistance = abs(frac(diagonal * 0.72 - time * 0.17) - 0.5);
                float sweep = pow(saturate(1.0 - sweepDistance * 7.5), 2.4);
                float scan = 0.5 + 0.5 * sin((input.uv.y * 92.0) - time * 8.0);
                float3 foil = HolographicPalette(diagonal * 6.0 + time * 2.1);

                float luminance = dot(sprite.rgb, float3(0.299, 0.587, 0.114));
                float band = floor(saturate(luminance) * 4.0) / 3.0;
                float3 printed = lerp(_InkColor.rgb, _ShadowTint.rgb, smoothstep(0.05, 0.42, band));
                printed = lerp(printed, lerp(sprite.rgb, _AcidTint.rgb, 0.12), smoothstep(0.4, 0.9, band));

                float2 dotCell = frac(input.positionCS.xy / max(_HalftoneScale, 1.0)) - 0.5;
                float dots = 1.0 - smoothstep(0.12, 0.19, dot(dotCell, dotCell));
                float printMask = dots * (1.0 - smoothstep(0.42, 0.82, luminance)) * _PrintStrength;

                // La lata conserva su dibujo original, pero recibe la misma lectura por
                // bandas, tinta y rim cromático que los materiales FSSRS del personaje.
                float3 color = lerp(sprite.rgb * 1.34, printed * 1.14, 0.36);
                color = lerp(color, _InkColor.rgb, printMask * 0.38);
                color += foil * _HoloIntensity * (0.055 + sweep * 0.46 + innerRim * 0.78 + scan * 0.045);
                color += lerp(_WarmTint.rgb, 1.0.xxx, sweep) * _Alert * (0.08 + 0.12 * sin(time * 9.0));

                if (_Role > 0.5)
                {
                    float3 echoTint = _Role < 1.5 ? _CoolTint.rgb : _WarmTint.rgb;
                    color = echoTint * (0.22 + luminance * 0.34 + sweep * 0.72 + innerRim * 0.55);
                    alpha *= _EchoAlpha * (0.62 + innerRim * 0.38);
                }

                #ifdef UNITY_UI_CLIP_RECT
                    alpha *= UnityGet2DClipping(input.localPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(alpha - 0.001);
                #endif

                return half4(saturate(color), alpha);
            }
            ENDHLSL
        }
    }
}
