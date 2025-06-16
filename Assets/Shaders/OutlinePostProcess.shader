Shader "Custom/OutlinePostProcess"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness("Outline Thickness", Range(0.1, 5.0)) = 1.0
        _DepthThreshold("Depth Threshold", Range(0.01, 1.0)) = 0.1
        _NormalThreshold("Normal Threshold", Range(0.01, 1.0)) = 0.4
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "OutlinePostProcess"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float _DepthThreshold;
                float _NormalThreshold;
            CBUFFER_END

            TEXTURE2D(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float4 color = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, screenUV);

                // Sample depth and normals
                float depth = SampleSceneDepth(screenUV);
                float3 normal = SampleSceneNormals(screenUV);

                // Calculate UV offsets for sampling neighbors
                float2 pixelSize = 1.0 / _ScreenParams.xy;
                float2 offsetX = float2(pixelSize.x * _OutlineThickness, 0);
                float2 offsetY = float2(0, pixelSize.y * _OutlineThickness);

                // Sample neighboring pixels for depth and normal differences
                float depthCenter = LinearEyeDepth(depth, _ZBufferParams);
                float depthRight = LinearEyeDepth(SampleSceneDepth(screenUV + offsetX), _ZBufferParams);
                float depthLeft = LinearEyeDepth(SampleSceneDepth(screenUV - offsetX), _ZBufferParams);
                float depthUp = LinearEyeDepth(SampleSceneDepth(screenUV + offsetY), _ZBufferParams);
                float depthDown = LinearEyeDepth(SampleSceneDepth(screenUV - offsetY), _ZBufferParams);

                float3 normalRight = SampleSceneNormals(screenUV + offsetX);
                float3 normalLeft = SampleSceneNormals(screenUV - offsetX);
                float3 normalUp = SampleSceneNormals(screenUV + offsetY);
                float3 normalDown = SampleSceneNormals(screenUV - offsetY);

                // Detect edges based on depth and normal differences
                float depthEdge = max(max(abs(depthCenter - depthRight), abs(depthCenter - depthLeft)),
                                      max(abs(depthCenter - depthUp), abs(depthCenter - depthDown)));
                float normalEdge = max(max(length(normal - normalRight), length(normal - normalLeft)),
                                       max(length(normal - normalUp), length(normal - normalDown)));

                // Apply thresholds to determine if it's an edge
                float isDepthEdge = step(_DepthThreshold, depthEdge);
                float isNormalEdge = step(_NormalThreshold, normalEdge);
                float isEdge = max(isDepthEdge, isNormalEdge);

                // Return outline color if it's an edge, otherwise return original color
                return lerp(color, _OutlineColor, isEdge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
} 