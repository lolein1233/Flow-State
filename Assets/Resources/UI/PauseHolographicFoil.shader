Shader "FLOWSTATE/UI/Pause Holographic Foil"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 0.22
        _HoloTime ("Unscaled animation time", Float) = 0
        _Pointer ("Pointer parallax", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            fixed4 _Color;
            float _HoloTime;
            float _Opacity;
            float4 _Pointer;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv + _Pointer.xy * float2(0.018, 0.012);
                float time = _HoloTime;
                float warp = sin(uv.y * 7.0 + time * 0.18) * 0.075 + sin(uv.x * 9.0 - time * 0.22) * 0.04;
                float diagonal = uv.x * 0.7 + uv.y * 0.43 + warp;
                float phase = diagonal * 1.5 - time * 0.045;
                float3 spectrum = 0.5 + 0.5 * cos(6.2831853 * (phase + float3(0.0, 0.33, 0.67)));
                float broadReflection = pow(saturate(0.5 + 0.5 * sin(diagonal * 10.0 - time * 0.44)), 5.0);
                float silver = pow(saturate(0.5 + 0.5 * sin(diagonal * 19.0 - time * 0.6)), 28.0);
                float3 ink = float3(0.035, 0.022, 0.075);
                float3 foil = spectrum * float3(0.34, 0.39, 0.48) + float3(0.025, 0.035, 0.055);
                float3 color = lerp(ink, foil, 0.42 + broadReflection * 0.47);
                color += silver * float3(0.08, 0.1, 0.12);
                float grain = frac(sin(dot(floor(i.vertex.xy), float2(12.9898, 78.233))) * 43758.5453);
                color += (grain - 0.5) * 0.01;
                float vignette = saturate(1.0 - dot((i.uv - 0.5) * float2(0.9, 0.7), (i.uv - 0.5) * float2(0.9, 0.7)));
                color *= 0.62 + vignette * 0.38;
                return fixed4(color * i.color.rgb, i.color.a * _Opacity);
            }
            ENDCG
        }
    }
}
