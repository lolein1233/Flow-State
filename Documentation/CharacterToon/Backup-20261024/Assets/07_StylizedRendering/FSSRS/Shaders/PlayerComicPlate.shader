Shader "FLOWSTATE/FSSRS/Player Comic Plate"
{
    Properties
    {
        _PlateRole("Plate Role", Range(0, 3)) = 0
        _ShellWidth("Shell Width", Range(0, 0.2)) = 0.04
        _RegistrationOffset("Registration Offset", Vector) = (0, 0, 0, 0)
        _JitterAmount("Edge Jitter", Range(0, 0.5)) = 0.12
        _Breakup("Plate Breakup", Range(0, 0.5)) = 0.08
        _Alpha("Alpha", Range(0, 1)) = 1
        _AnimationPhase("Animation Phase", Float) = 0
        _PulseAmount("Pulse Amount", Range(0, 0.5)) = 0.12
        _FlowSpeed("Flow Speed", Range(0.1, 2)) = 1
        _PanelMotion("Moving Comic Panels", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+10"
        }

        Pass
        {
            Name "Player Comic Plate"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ComicPlateVertex
            #pragma fragment ComicPlateFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/07_StylizedRendering/FSSRS/Shaders/Includes/FSSRSCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _PlateRole;
                half _ShellWidth;
                half4 _RegistrationOffset;
                half _JitterAmount;
                half _Breakup;
                half _Alpha;
                half _AnimationPhase;
                half _PulseAmount;
                half _FlowSpeed;
                half _PanelMotion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ComicPlateVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                half energy = saturate(_FSSRS_EmotionEnergy);
                half time = _Time.y * _FlowSpeed * lerp(0.75h, 1.45h, energy) + _AnimationPhase;
                half steppedTime = floor(time * lerp(2.0h, 8.0h, energy)) * 0.125h;
                half edgeNoise = sin(dot(input.positionOS.xyz, float3(17.13, 29.71, 11.83)) + steppedTime);
                half flowingWave = sin(dot(input.positionOS.xyz, float3(4.8, 7.1, 3.2)) - time * 2.4h);
                half breath = sin(time * 1.35h + input.positionOS.y * 2.8h);
                half panelTravel = 0.5h + 0.5h * sin(dot(input.positionOS.xyz, float3(3.7, 5.9, 4.3)) - time * 4.1h);
                half panelRidge = panelTravel * panelTravel * panelTravel;
                half animationWeight = lerp(0.4h, 1.0h, energy);
                half widthScale = 1.0h + (edgeNoise * _JitterAmount + flowingWave * _JitterAmount * 0.25h + breath * _PulseAmount) * animationWeight;
                widthScale += panelRidge * _PanelMotion * animationWeight * 0.42h;
                half width = _ShellWidth * max(0.25h, widthScale);
                float3 positionOS = input.positionOS.xyz + normalize(input.normalOS) * width;
                output.positionCS = TransformObjectToHClip(positionOS);

                half pulse = 0.5h + 0.5h * sin(time * lerp(1.4h, 3.2h, energy));
                float2 orbit = float2(sin(time * 1.17h), cos(time * 0.93h));
                float2 registration = _RegistrationOffset.xy * lerp(0.82h, 1.18h, pulse * energy);
                registration += orbit * (_PulseAmount * lerp(1.0h, 4.0h, energy));
                output.positionCS.xy += registration * (2.0 / _ScreenParams.xy) * output.positionCS.w;
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.positionWS = TransformObjectToWorld(positionOS);
                return output;
            }

            half4 ComicPlateFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 pixelPosition = input.screenPosition.xy / input.screenPosition.w * _ScreenParams.xy;
                half block = FSSRS_Hash21(floor(pixelPosition / 7.0h) + _FSSRS_EmotionIndex * 13.7h);
                half time = _Time.y * _FlowSpeed + _AnimationPhase;
                half stripeWave = 0.5h + 0.5h * sin((pixelPosition.x + pixelPosition.y * 0.46h) * 0.12h - time * 3.4h + _FSSRS_EmotionIndex);
                half crossWave = 0.5h + 0.5h * sin((pixelPosition.x * -0.31h + pixelPosition.y) * 0.075h + time * 2.1h);
                half movingPanel = 0.5h + 0.5h * sin((pixelPosition.x + pixelPosition.y * 0.72h) * 0.04h - time * 4.2h);
                half stripe = smoothstep(0.32h, 0.68h, stripeWave);
                half cyclePosition = frac((pixelPosition.x + pixelPosition.y * 1.37h) * 0.007h - time * 0.32h);
                half3 colorCycle = lerp(_FSSRS_AccentColor.rgb, _FSSRS_MidColor.rgb, smoothstep(0.0h, 0.5h, cyclePosition));
                colorCycle = lerp(colorCycle, _FSSRS_HighlightColor.rgb, smoothstep(0.5h, 1.0h, cyclePosition));

                half3 plateColor = _FSSRS_PaperColor.rgb;
                half alpha = _Alpha;
                if (_PlateRole > 0.5h && _PlateRole < 1.5h)
                {
                    plateColor = lerp(_FSSRS_AccentColor.rgb, colorCycle, 0.48h + _FSSRS_EmotionEnergy * 0.42h);
                    plateColor = lerp(plateColor, _FSSRS_HighlightColor.rgb, stripe * 0.18h);
                    clip(block - _Breakup * 0.34h);
                }
                else if (_PlateRole > 1.5h && _PlateRole < 2.5h)
                {
                    plateColor = _FSSRS_InkColor.rgb;
                    clip(block - _Breakup * 0.1h);
                }
                else if (_PlateRole >= 2.5h)
                {
                    plateColor = lerp(colorCycle, _FSSRS_PaperColor.rgb, crossWave * 0.12h);
                    alpha *= lerp(0.72h, 1.0h, movingPanel);
                    clip(movingPanel - _PanelMotion * 0.22h);
                    clip(block - _Breakup * lerp(0.22h, 0.42h, crossWave));
                }

                return half4(plateColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
