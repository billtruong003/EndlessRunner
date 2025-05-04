Shader "GT01/RoundWorldToon"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _EmissionMap("Emission Map (RGB)", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionIntensity("Emission Intensity", Range(0, 10)) = 1
        [HDR] _AmbientColor("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
        [HDR] _SpecularColor("Specular Color", Color) = (0.9, 0.9, 0.9, 1)
        _Glossiness("Glossiness", Float) = 32
        [HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.716
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
        _CurveValue("Vertical Curve", Range(-1, 1)) = 0.01
        _LateralCurve("Lateral Curve", Range(-1, 1)) = 0
        _MaxCurveDistance("Max Curve Distance", Float) = 100
        _MinCurveHeight("Min Curve Height", Float) = -100 // Prevent clipping below this height
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        LOD 200

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NormalMap_ST;
                float4 _EmissionMap_ST;
                half4 _Color;
                half4 _AmbientColor;
                half4 _SpecularColor;
                float _Glossiness;
                half4 _EmissionColor;
                half _EmissionIntensity;
                half4 _RimColor;
                float _RimAmount;
                float _RimThreshold;
                float _CurveValue;
                float _LateralCurve;
                float _MaxCurveDistance;
                float _MinCurveHeight;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 distanceXZ = posWS.xyz - _WorldSpaceCameraPos.xyz;
                distanceXZ.y = 0;
                float distXZ = length(distanceXZ);
                float distFactor = min(distXZ, _MaxCurveDistance) / _MaxCurveDistance;

                // Vertical curvature with clamping
                float offsetY = -distFactor * distFactor * _CurveValue * _MaxCurveDistance;
                offsetY = max(offsetY, _MinCurveHeight - posWS.y); // Prevent clipping below min height

                // Lateral curvature (Subway Surfers-style)
                float3 cameraForward = normalize(float3(_WorldSpaceCameraPos.x, 0, _WorldSpaceCameraPos.z) - posWS);
                float3 lateralDir = cross(float3(0, 1, 0), cameraForward);
                float offsetLateral = distFactor * distFactor * _LateralCurve * _MaxCurveDistance;
                float3 modifiedPosWS = posWS + float3(offsetLateral * lateralDir.x, offsetY, offsetLateral * lateralDir.z);

                // Transform normal to account for curvature
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float3 modifiedNormalWS = normalize(normalWS + float3(offsetLateral * 0.1, -offsetY * 0.1, 0));

                // Ensure position stays within reasonable bounds
                modifiedPosWS.y = max(modifiedPosWS.y, _MinCurveHeight);

                OUT.positionCS = TransformWorldToHClip(modifiedPosWS);
                OUT.positionWS = modifiedPosWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS = modifiedNormalWS;
                OUT.viewDirWS = SafeNormalize(_WorldSpaceCameraPos.xyz - modifiedPosWS);
                OUT.tangentWS = float4(tangentWS, IN.tangentOS.w);
                OUT.shadowCoord = TransformWorldToShadowCoord(modifiedPosWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample textures
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb * _EmissionIntensity;

                // Tangent to world space normal
                float3 normalWS = normalize(IN.normalWS);
                float3 tangentWS = normalize(IN.tangentWS.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * IN.tangentWS.w;
                float3x3 TBN = float3x3(tangentWS, bitangentWS, normalWS);
                normalWS = normalize(mul(normalTS, TBN));

                // Normalize view direction
                float3 viewDirWS = normalize(IN.viewDirWS);

                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDirWS = mainLight.direction;
                half3 lightColor = mainLight.color;
                half shadow = MainLightRealtimeShadow(IN.shadowCoord);
                half NdotL = dot(normalWS, lightDirWS);
                half lightIntensity = smoothstep(0, 0.01, NdotL * shadow);
                half4 light = lightIntensity * half4(lightColor, 1);

                // Ambient light
                half4 ambient = _AmbientColor;

                // Specular
                float3 halfVector = normalize(lightDirWS + viewDirWS);
                float NdotH = dot(normalWS, halfVector);
                float specularIntensity = pow(NdotH * lightIntensity, _Glossiness * _Glossiness);
                float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
                half4 specular = specularIntensitySmooth * _SpecularColor;

                // Rim lighting
                float rimDot = 1 - dot(viewDirWS, normalWS);
                float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
                rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
                half4 rim = rimIntensity * _RimColor;

                // Final color
                half4 finalColor = albedo * (ambient + light + specular + rim) + half4(emission, 0);
                return finalColor;
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _CurveValue;
                float _LateralCurve;
                float _MaxCurveDistance;
                float _MinCurveHeight;
            CBUFFER_END

            float3 _LightDirection;

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // Apply curvature effect
                float3 distanceXZ = positionWS - _WorldSpaceCameraPos.xyz;
                distanceXZ.y = 0;
                float distXZ = length(distanceXZ);
                float distFactor = min(distXZ, _MaxCurveDistance) / _MaxCurveDistance;
                float offsetY = -distFactor * distFactor * _CurveValue * _MaxCurveDistance;
                offsetY = max(offsetY, _MinCurveHeight - positionWS.y);

                float3 cameraForward = normalize(float3(_WorldSpaceCameraPos.x, 0, _WorldSpaceCameraPos.z) - positionWS);
                float3 lateralDir = cross(float3(0, 1, 0), cameraForward);
                float offsetLateral = distFactor * distFactor * _LateralCurve * _MaxCurveDistance;
                positionWS += float3(offsetLateral * lateralDir.x, offsetY, offsetLateral * lateralDir.z);
                positionWS.y = max(positionWS.y, _MinCurveHeight);

                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}