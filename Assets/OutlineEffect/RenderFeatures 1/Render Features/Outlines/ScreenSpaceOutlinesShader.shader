Shader "Hidden/ImprovedScreenSpaceOutlines"
{
    Properties
    {
        _OutlineScale ("Outline Scale", Float) = 1.0
        _RobertsCrossMultiplier ("Roberts Cross Multiplier", Float) = 100.0
        _DepthThreshold ("Depth Threshold", Float) = 10.0
        _NormalThreshold ("Normal Threshold", Float) = 0.4
        _ColorThreshold ("Color Threshold", Float) = 0.1
        _OutlineColor ("Outline Color", Color) = (0, 0.888169, 1, 1)
        [ToggleUI] _UseNormalOutline ("Use Normal Outline", Float) = 0.0
        [ToggleUI] _UseColorOutline ("Use Color Outline", Float) = 0.0
        _DistanceScaleFactor ("Distance Scale Factor", Float) = 0.1
        [ToggleUI] _UseGlowEffect ("Use Glow Effect", Float) = 0.0
        [Enum(Solid, 0, Dashed, 1, Dotted, 2)] _OutlineStyle ("Outline Style", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "DrawProcedural"
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _OutlineScale;
                float _RobertsCrossMultiplier;
                float _DepthThreshold;
                float _NormalThreshold;
                float _ColorThreshold;
                float4 _OutlineColor;
                float _UseNormalOutline;
                float _UseColorOutline;
                float _DistanceScaleFactor;
                float _UseGlowEffect;
                float _OutlineStyle;
            CBUFFER_END

            TEXTURE2D(_FilterTexture);
            SAMPLER(sampler_BlitTexture_PointClamp);
            TEXTURE2D(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                float4 position : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 uv = float2(input.vertexID & 1, input.vertexID >> 1);
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                output.uv = uv;
                float4 worldPos = mul(unity_MatrixInvVP, output.positionCS);
                worldPos.xyz /= worldPos.w;
                output.viewDir = worldPos.xyz - _WorldSpaceCameraPos;
                output.viewDir = normalize(output.viewDir);
                return output;
            }

            float4 Frag(Varyings input) : SV_TARGET
            {
                float2 uv = input.uv;
                float4 normalSample = SAMPLE_TEXTURE2D(_FilterTexture, sampler_BlitTexture_PointClamp, uv);
                if (normalSample.a < 0.5)
                {
                    normalSample.rgb = SampleSceneNormals(uv);
                }
                float3 normal = normalSample.rgb * 2.0 - 1.0;

                float depth = SampleSceneDepth(uv);
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);

                float2 texelSize = _ScreenParams.zw - 1.0;
                float2 offsets[8];
                offsets[0] = uv + _OutlineScale * float2(-texelSize.x, texelSize.y) * 0.5;
                offsets[1] = uv + _OutlineScale * float2(texelSize.x, texelSize.y) * 0.5;
                offsets[2] = uv + _OutlineScale * float2(texelSize.x, -texelSize.y) * 0.5;
                offsets[3] = uv + _OutlineScale * float2(-texelSize.x, -texelSize.y) * 0.5;
                offsets[4] = uv + _OutlineScale * float2(0, texelSize.y) * 0.5;
                offsets[5] = uv + _OutlineScale * float2(0, -texelSize.y) * 0.5;
                offsets[6] = uv + _OutlineScale * float2(texelSize.x, 0) * 0.5;
                offsets[7] = uv + _OutlineScale * float2(-texelSize.x, 0) * 0.5;

                float depthSamples[8];
                for (int i = 0; i < 8; i++)
                {
                    depthSamples[i] = SampleSceneDepth(offsets[i]);
                    depthSamples[i] = LinearEyeDepth(depthSamples[i], _ZBufferParams);
                }

                float depthDiff1 = abs(depthSamples[1] - depthSamples[0]);
                float depthDiff2 = abs(depthSamples[2] - depthSamples[3]);
                float depthDiff3 = abs(depthSamples[5] - depthSamples[4]);
                float depthDiff4 = abs(depthSamples[6] - depthSamples[7]);
                float depthEdge = max(max(depthDiff1, depthDiff2), max(depthDiff3, depthDiff4)) * _RobertsCrossMultiplier;
                float depthEdgeFactor = saturate((depthEdge - _DepthThreshold) / _DepthThreshold);

                float4 normalSamples[8];
                for (i = 0; i < 8; i++)
                {
                    normalSamples[i] = SAMPLE_TEXTURE2D(_FilterTexture, sampler_BlitTexture_PointClamp, offsets[i]);
                    if (normalSamples[i].a < 0.5)
                    {
                        normalSamples[i].rgb = SampleSceneNormals(offsets[i]);
                    }
                }
                float3 normalDiff1 = abs((normalSamples[1].rgb * 2.0 - 1.0) - (normalSamples[0].rgb * 2.0 - 1.0));
                float3 normalDiff2 = abs((normalSamples[2].rgb * 2.0 - 1.0) - (normalSamples[3].rgb * 2.0 - 1.0));
                float3 normalDiff3 = abs((normalSamples[5].rgb * 2.0 - 1.0) - (normalSamples[4].rgb * 2.0 - 1.0));
                float3 normalDiff4 = abs((normalSamples[6].rgb * 2.0 - 1.0) - (normalSamples[7].rgb * 2.0 - 1.0));
                float normalEdge = max(max(length(normalDiff1), length(normalDiff2)), max(length(normalDiff3), length(normalDiff4)));
                float normalEdgeFactor = saturate((normalEdge - _NormalThreshold) / _NormalThreshold) * _UseNormalOutline;

                float3 colorSamples[4];
                for (i = 0; i < 4; i++)
                {
                    colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, offsets[i]).rgb;
                }
                float3 colorDiff1 = colorSamples[1] - colorSamples[0];
                float3 colorDiff2 = colorSamples[2] - colorSamples[3];
                float colorEdge = sqrt(dot(colorDiff1, colorDiff1) + dot(colorDiff2, colorDiff2));
                float colorEdgeFactor = saturate((colorEdge - _ColorThreshold) / _ColorThreshold) * _UseColorOutline;

                float edgeFactor = max(depthEdgeFactor, max(normalEdgeFactor, colorEdgeFactor));

                float distance = linearDepth;
                float distanceFactor = 1.0 / (distance * _DistanceScaleFactor + 1.0);
                edgeFactor *= distanceFactor;

                if (_OutlineStyle == 1.0)
                {
                    float pattern = sin(input.uv.x * 50.0 * _ScreenParams.x / _ScreenParams.y) > 0.0 ? 1.0 : 0.0;
                    edgeFactor *= pattern;
                }
                else if (_OutlineStyle == 2.0)
                {
                    float patternX = sin(input.uv.x * 100.0 * _ScreenParams.x / _ScreenParams.y);
                    float patternY = sin(input.uv.y * 100.0);
                    float pattern = (patternX * patternY) > 0.0 ? 1.0 : 0.0;
                    edgeFactor *= pattern;
                }

                float4 outlineColor = _OutlineColor;
                if (_UseGlowEffect > 0.5)
                {
                    float glow = 0.5 + 0.5 * sin(_Time.y * 2.0);
                    outlineColor.rgb *= glow;
                }

                return edgeFactor * outlineColor;
            }
            ENDHLSL
        }
    }
    CustomEditor "UnityEditor.Rendering.Fullscreen.ShaderGraph.FullscreenShaderGUI"
    Fallback "Hidden/Shader Graph/FallbackError"
}