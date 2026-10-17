Shader "FLOWSTATE/Graffiti/LayeredSprayStamp"
{
    Properties
    {
        _GlobalOpacity ("Global Opacity", Range(0, 1)) = 1
        [HideInInspector] _ZTest ("Depth Test", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Layered Spray Stamp"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GraffitiSprayCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 profile : TEXCOORD1;
                float4 spray : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 profile : TEXCOORD1;
                float4 spray : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float _GlobalOpacity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.profile = input.profile;
                output.spray = input.spray;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return FlowEvaluateSpray(input.uv, input.color, input.profile, input.spray, _GlobalOpacity);
            }
            ENDHLSL
        }
    }
}
