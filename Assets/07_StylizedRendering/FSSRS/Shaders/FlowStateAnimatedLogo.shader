Shader "FLOW STATE/UI/Animated Punk Logo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ChromaticSplit ("Chromatic Split", Range(0, 0.08)) = 0.009
        _GlitchAmount ("Glitch Amount", Range(0, 0.08)) = 0.018
        _EchoIntensity ("Echo Intensity", Range(0, 2)) = 0.9
        _SweepIntensity ("Sweep Intensity", Range(0, 3)) = 1.25
        _CyanEcho ("Cyan Echo", Color) = (0,0.9,1,1)
        _MagentaEcho ("Magenta Echo", Color) = (1,0.02,0.48,1)
        _SweepColor ("Sweep Color", Color) = (1,0.92,0.24,1)
        _AnimationTime ("Animation Time", Float) = 0
        _BeatPulse ("Beat Pulse", Range(0, 1)) = 0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
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
            Name "AnimatedPunkLogo"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_TexelSize;
            float _ChromaticSplit;
            float _GlitchAmount;
            float _EchoIntensity;
            float _SweepIntensity;
            fixed4 _CyanEcho;
            fixed4 _MagentaEcho;
            fixed4 _SweepColor;
            float _AnimationTime;
            float _BeatPulse;

            float Hash(float2 value)
            {
                return frac(sin(dot(value, float2(127.1, 311.7))) * 43758.5453);
            }

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 SampleLogo(float2 uv)
            {
                return tex2D(_MainTex, saturate(uv)) + _TextureSampleAdd;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.texcoord;
                float timeStep = floor(_AnimationTime * 9.0);
                float band = floor(uv.y * 27.0);
                float bandNoise = Hash(float2(band, timeStep));
                float burst = step(0.83, bandNoise) * step(0.52, Hash(float2(timeStep, 4.7)));
                float horizontalTear = (bandNoise - 0.5) * _GlitchAmount * burst * (0.45 + _BeatPulse);
                float2 tornUv = uv + float2(horizontalTear, 0.0);

                float split = _ChromaticSplit * (0.7 + _BeatPulse * 1.6);
                float2 echoOffset = float2(split, split * 0.22);
                fixed4 source = SampleLogo(tornUv);
                fixed4 cyanSample = SampleLogo(tornUv + echoOffset * 1.35);
                fixed4 magentaSample = SampleLogo(tornUv - echoOffset * 1.7);

                float cyanOnly = saturate(cyanSample.a - source.a * 0.58);
                float magentaOnly = saturate(magentaSample.a - source.a * 0.58);
                float echoAlpha = max(cyanOnly, magentaOnly);
                float3 echoColor =
                    _CyanEcho.rgb * cyanOnly +
                    _MagentaEcho.rgb * magentaOnly;
                echoColor *= _EchoIntensity;

                float sweepAxis = uv.x * 0.86 + uv.y * 0.22;
                float sweepCenter = frac(_AnimationTime * 0.16) * 1.35 - 0.18;
                float sweepDistance = abs(sweepAxis - sweepCenter);
                float sweep = 1.0 - smoothstep(0.025, 0.12, sweepDistance);
                sweep *= source.a * _SweepIntensity;

                float sliceFlash = burst * step(0.62, frac(uv.x * 6.0 + bandNoise));
                float3 mainColor = source.rgb;
                mainColor += _SweepColor.rgb * sweep;
                mainColor = lerp(mainColor, _MagentaEcho.rgb, sliceFlash * 0.18);

                fixed4 color;
                color.rgb = lerp(echoColor, mainColor, source.a);
                color.a = max(source.a, echoAlpha * 0.9);
                color *= input.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
