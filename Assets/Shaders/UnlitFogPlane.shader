Shader "Cyanilux/Fog Plane Simple URP"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (1, 1, 1, 1)
        _Density ("Density", Range(0.01, 10)) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            // THÊM DÒNG NÀY VÀO
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _Density;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 screenUV = i.positionHCS.xy / i.positionHCS.w;
                float rawSceneDepth = SampleSceneDepth(screenUV); // Dòng này sẽ hoạt động
                float sceneLinearEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float planeRawDepth = i.positionHCS.z / i.positionHCS.w;
                float planeLinearEyeDepth = LinearEyeDepth(planeRawDepth, _ZBufferParams);
                float depthDifference = sceneLinearEyeDepth - planeLinearEyeDepth;
                depthDifference = max(0, depthDifference);
                float fogAlpha = saturate(depthDifference * _Density);
                fogAlpha *= _FogColor.a;
                return half4(_FogColor.rgb, fogAlpha);
            }
            ENDHLSL
        }
    }
}