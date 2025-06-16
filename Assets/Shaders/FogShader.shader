Shader "Custom/TransparentFogObject"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _FogColor ("Fog Color", Color) = (0.5,0.5,0.5,1)
        _FogDensity ("Fog Density", Float) = 0.1
        _FogStart ("Fog Start", Float) = 0
        _FogEnd ("Fog End", Float) = 100
        [Toggle] _UseExponentialFog ("Use Exponential Fog", Float) = 0
        [Toggle] _UseHeightFog ("Use Height Fog", Float) = 0
        _HeightFogDensity ("Height Fog Density", Float) = 0.1
        _HeightFogStart ("Height Fog Start", Float) = 0
        _HeightFogEnd ("Height Fog End", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // Kích hoạt alpha blending
            ZWrite Off // Không ghi vào depth buffer

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _FogColor;
                float _FogDensity;
                float _FogStart;
                float _FogEnd;
                float _UseExponentialFog;
                float _UseHeightFog;
                float _HeightFogDensity;
                float _HeightFogStart;
                float _HeightFogEnd;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                float distance = length(input.worldPos - _WorldSpaceCameraPos);
                float fogFactor;

                if (_UseExponentialFog > 0.5)
                {
                    fogFactor = 1.0 - exp(-_FogDensity * distance);
                }
                else
                {
                    fogFactor = saturate((distance - _FogStart) / (_FogEnd - _FogStart));
                }

                // Thêm height fog nếu được bật
                if (_UseHeightFog > 0.5)
                {
                    float heightFactor = saturate((_HeightFogEnd - input.worldPos.y) / (_HeightFogEnd - _HeightFogStart));
                    fogFactor = max(fogFactor, heightFactor * _HeightFogDensity);
                }

                // Áp dụng fog factor vào alpha để tạo transparency
                half4 finalColor = texColor;
                finalColor.a *= (1.0 - fogFactor); // Giảm alpha dựa trên fog factor
                finalColor.rgb = lerp(texColor.rgb, _FogColor.rgb, fogFactor);

                return finalColor;
            }
            ENDHLSL
        }
    }
}