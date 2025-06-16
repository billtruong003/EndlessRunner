Shader "Custom/URPFog"
{
    Properties
    {
        [Header(Fog Settings)]
        _FogColor("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _FogStartDistance("Fog Start Distance", Float) = 10
        _FogEndDistance("Fog End Distance", Float) = 50
        _FogDensity("Fog Density", Range(0, 1)) = 0.5
        _FogHeightStart("Fog Height Start", Float) = 0
        _FogHeightEnd("Fog Height End", Float) = 10
        _FogHeightDensity("Fog Height Density", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FogPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
                float4 screenPos : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogStartDistance;
                float _FogEndDistance;
                float _FogDensity;
                float _FogHeightStart;
                float _FogHeightEnd;
                float _FogHeightDensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Calculate distance fog
                float distance = length(input.worldPos - _WorldSpaceCameraPos.xyz);
                float distanceFog = smoothstep(_FogStartDistance, _FogEndDistance, distance);
                distanceFog = distanceFog * _FogDensity;

                // Calculate height-based fog
                float heightFog = smoothstep(_FogHeightStart, _FogHeightEnd, input.worldPos.y);
                heightFog = heightFog * _FogHeightDensity;

                // Sample depth texture for depth-based fog contribution
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float depth = SampleSceneDepth(screenUV);
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float depthFog = smoothstep(_FogStartDistance, _FogEndDistance, linearDepth);
                depthFog = depthFog * _FogDensity;

                // Combine fog effects (you can adjust the blending logic as needed)
                float combinedFog = max(distanceFog, depthFog);
                combinedFog = min(combinedFog, heightFog + distanceFog);

                // Final fog color with alpha based on combined fog density
                half4 fogColor = _FogColor;
                fogColor.a = combinedFog * _FogColor.a;
                return fogColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
} 