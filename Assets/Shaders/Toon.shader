Shader "Custom/URPToon"
{
    Properties
    {
        _Color("Color", Color) = (0.5, 0.65, 1, 1)
        _MainTex("Main Texture", 2D) = "white" {}
        [HDR] _AmbientColor("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
        [HDR] _ToonLitColor("Toon Lit Color", Color) = (1, 1, 1, 1)
        [HDR] _ToonShadowColor("Toon Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _ToonThreshold("Toon Threshold", Range(0, 1)) = 0.5
        _ToonSmoothness("Toon Smoothness", Range(0.001, 0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float4 _AmbientColor;
            float4 _ToonLitColor;
            float4 _ToonShadowColor;
            float _ToonThreshold;
            float _ToonSmoothness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normal);
                float3 worldPos = TransformObjectToWorld(input.position.xyz);
                output.viewDirWS = GetWorldSpaceViewDir(worldPos);
                output.shadowCoord = TransformWorldToShadowCoord(worldPos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample main texture
                half4 sample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Normalize inputs
                float3 normalWS = normalize(input.normalWS);

                // Get main light
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 lightDirWS = mainLight.direction;
                half NdotL = saturate(dot(normalWS, lightDirWS));
                half toonFactor = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, NdotL);
                toonFactor *= mainLight.shadowAttenuation;
                half3 toonShadedColor = lerp(_ToonShadowColor.rgb, _ToonLitColor.rgb, toonFactor);

                // Final color
                half3 finalColor = (_Color * sample).rgb * toonShadedColor + _AmbientColor.rgb;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 position : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.position.xyz);
                output.positionCS = TransformWorldToHClip(worldPos);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}