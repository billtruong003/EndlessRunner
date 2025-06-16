Shader "Hidden/OutlineURP"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
    	
    	Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
			
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

			TEXTURE2D(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);

			TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_TexelSize;
			float _Scale;
			float4 _Color;
			float4 _NormalColor;
			float _DepthThreshold;
			float _DepthNormalThreshold;
			float _DepthNormalThresholdScale;
			float _NormalThreshold;
			float4x4 _ClipToView;
			CBUFFER_END


			float4 alphaBlend(float4 top, float4 bottom)
			{
				float3 color = (top.rgb * top.a) + (bottom.rgb * (1.0 - top.a));
				float alpha = top.a + bottom.a * (1.0 - top.a);

				return float4(color, alpha);
			}

			struct Attributes
			{
				uint vertexID : SV_VertexID;
			};


			struct Varyings
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
                float3 viewSpaceDir : TEXCOORD1;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				
				// Create a full-screen triangle
				float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
				output.texcoord = uv;
				output.vertex = float4(uv * 2.0 - 1.0, 0.0, 1.0);
				
				#if UNITY_UV_STARTS_AT_TOP
				output.vertex.y *= -1.0;
				#endif
				
                output.viewSpaceDir = mul(_ClipToView, output.vertex).xyz;
				return output;
			}

			float4 Frag(Varyings i) : SV_Target
			{
				float halfScaleFloor = floor(_Scale * 0.5);
				float halfScaleCeil = ceil(_Scale * 0.5);

				float2 bottomLeftUV = i.texcoord - _MainTex_TexelSize.xy * halfScaleFloor;
				float2 topRightUV = i.texcoord + _MainTex_TexelSize.xy * halfScaleCeil;  
				float2 bottomRightUV = i.texcoord + float2(_MainTex_TexelSize.x * halfScaleCeil, -_MainTex_TexelSize.y * halfScaleFloor);
				float2 topLeftUV = i.texcoord + float2(-_MainTex_TexelSize.x * halfScaleFloor, _MainTex_TexelSize.y * halfScaleCeil);

                // Normal sampling - decode from the normal texture
				float3 normal0 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, bottomLeftUV).xyz;
				float3 normal1 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, topRightUV).xyz;
				float3 normal2 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, bottomRightUV).xyz;
				float3 normal3 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, topLeftUV).xyz;
				
				// Decode normals from [0,1] to [-1,1]
				normal0 = normal0 * 2.0 - 1.0;
				normal1 = normal1 * 2.0 - 1.0;
				normal2 = normal2 * 2.0 - 1.0;
				normal3 = normal3 * 2.0 - 1.0;

                // Depth sampling
				float depth0 = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, bottomLeftUV).r;
				float depth1 = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, topRightUV).r;
				float depth2 = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, bottomRightUV).r;
				float depth3 = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, topLeftUV).r;
                
                // Convert depth to linear eye space
                float linearDepth0 = LinearEyeDepth(depth0, _ZBufferParams);
                float linearDepth1 = LinearEyeDepth(depth1, _ZBufferParams);
                float linearDepth2 = LinearEyeDepth(depth2, _ZBufferParams);
                float linearDepth3 = LinearEyeDepth(depth3, _ZBufferParams);
				
				// Normal is already in view space from the texture
				float3 viewNormal = normalize(normal0);

				float NdotV = 1.0 - dot(viewNormal, -normalize(i.viewSpaceDir));

				float normalThreshold01 = saturate((NdotV - _DepthNormalThreshold) / (1.0 - _DepthNormalThreshold));
				float normalThreshold = normalThreshold01 * _DepthNormalThresholdScale + 1.0;

				float depthThreshold = _DepthThreshold / linearDepth0 * normalThreshold;
				
				float depthFiniteDifference0 = linearDepth1 - linearDepth0;
				float depthFiniteDifference1 = linearDepth3 - linearDepth2;
				
				float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2));
				edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

				float3 normalFiniteDifference0 = normal1 - normal0;
				float3 normalFiniteDifference1 = normal3 - normal2;
				float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
				edgeNormal = edgeNormal > _NormalThreshold ? 1 : 0;

				float edge = max(edgeDepth, edgeNormal);

				float4 edgeColor = float4(_Color.rgb, _Color.a * edgeDepth);
				float4 normalColor = float4(_NormalColor.rgb, _NormalColor.a * edgeNormal);

				float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

				float4 finalNormalColor = alphaBlend(normalColor, color);
        		float4 finalColor = alphaBlend(edgeColor, finalNormalColor);

				return finalColor;
			}
            ENDHLSL
        }
    }
}
