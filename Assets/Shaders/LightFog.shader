Shader "Custom/URPLightFog"
{
    Properties
    {
        [Header(Fog Settings)]
        _FogColor("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _FogStartDistance("Fog Start Distance", Float) = 10
        _FogEndDistance("Fog End Distance", Float) = 50
        _FogDensity("Fog Density", Range(0, 1)) = 0.5
        
        [Header(Render Settings)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 2 // Back by default
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 5 // SrcAlpha by default
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 10 // OneMinusSrcAlpha by default
        [Toggle] _ZWrite("ZWrite", Float) = 0 // Off by default
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 // LEqual by default
        _RenderQueueOffset("Render Queue Offset", Range(-50, 50)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        LOD 100
        
        Cull [_CullMode]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Blend [_SrcBlend] [_DstBlend]
        Offset [_RenderQueueOffset], [_RenderQueueOffset]

        Pass
        {
            Name "LightFogPass"
            Tags { "LightMode" = "UniversalForward" }

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
                float3 worldPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogStartDistance;
                float _FogEndDistance;
                float _FogDensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Calculate distance fog only
                float distance = length(input.worldPos - _WorldSpaceCameraPos.xyz);
                float distanceFog = smoothstep(_FogStartDistance, _FogEndDistance, distance);
                distanceFog = distanceFog * _FogDensity;

                // Final fog color with alpha based on distance fog density
                half4 fogColor = _FogColor;
                fogColor.a = distanceFog * _FogColor.a;
                return fogColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
} 