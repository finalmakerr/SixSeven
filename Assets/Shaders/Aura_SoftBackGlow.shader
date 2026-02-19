Shader "Custom/Aura_SoftBackGlow"
{
    Properties
    {
        [HDR]_GlowColor ("Glow Color", Color) = (1.0, 0.9019608, 0.627451, 1.0)
        _Intensity ("Intensity", Range(0, 10)) = 2.5
        _Radius ("Radius", Range(0.05, 2.0)) = 0.65
        _Softness ("Softness", Range(0.01, 1.0)) = 0.4
        _InnerSharpness ("Inner Sharpness", Range(0.5, 8.0)) = 2.2
        _EdgeBoost ("Edge Boost", Range(0.0, 4.0)) = 1.4
        _GroundLift ("Ground Lift", Range(0.0, 1.0)) = 0.15
        _PulseAmount ("Pulse Amount", Range(0, 0.4)) = 0.08
        _PulseSpeed ("Pulse Speed", Range(0, 6)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _Intensity;
                half _Radius;
                half _Softness;
                half _InnerSharpness;
                half _EdgeBoost;
                half _GroundLift;
                half _PulseAmount;
                half _PulseSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1) Centered UV
                float2 centeredUV = input.uv - float2(0.5, 0.5);

                // Slight vertical bias to create subtle ground glow under feet.
                centeredUV.y += _GroundLift;

                // 2) Radial distance
                float radialDist = length(centeredUV);

                // 3) Invert radial distance (normalized by Radius)
                float radial = 1.0 - saturate(radialDist / max(_Radius, 0.0001));

                // 4) Smooth falloff control
                float soft = smoothstep(0.0, saturate(_Softness), radial);

                // 5) Sharpen inner glow and edge emphasis
                float core = pow(saturate(soft), max(_InnerSharpness, 0.001));
                float edge = pow(saturate(1.0 - radial), 2.0) * _EdgeBoost;

                // Optional pulse: Time -> Sine -> small modulation
                float pulse = 1.0 + (sin(_Time.y * _PulseSpeed) * _PulseAmount);

                // Combined glow mask (no clipping, smooth transparent falloff)
                float glowMask = saturate((soft + core + edge) * pulse);

                // 6 + 7) Color * Intensity
                half3 glow = _GlowColor.rgb * (_Intensity * glowMask);

                // Transparent additive output. Alpha participates in soft blending only.
                return half4(glow, glowMask * _GlowColor.a);
            }
            ENDHLSL
        }
    }
}
