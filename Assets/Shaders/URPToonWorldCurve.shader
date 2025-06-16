Shader "Custom/URPToonWorldCurve"
{
    Properties
    {
        _Color("Color", Color) = (0.5, 0.65, 1, 1)
        _MainTex("Main Texture", 2D) = "white" {}
        [HDR] _AmbientColor("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
        [HDR] _SpecularColor("Specular Color", Color) = (0.9, 0.9, 0.9, 1)
        _Glossiness("Glossiness", Float) = 32
        [HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.716
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
        _CurveValue("Vertical Curve", Range(-10, 10)) = 0.01
        _LateralCurve("Lateral Curve", Range(-10, 10)) = 0
        _MaxCurveDistance("Max Curve Distance", Float) = 100
        _MinCurveHeight("Min Curve Height", Float) = -100
        _CurveNormalOffset("Curve Normal Offset", Range(0, 1)) = 0.1
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
            float4 _SpecularColor;
            float _Glossiness;
            float4 _RimColor;
            float _RimAmount;
            float _RimThreshold;
            float _CurveValue;
            float _LateralCurve;
            float _MaxCurveDistance;
            float _MinCurveHeight;
            float _CurveNormalOffset;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.position.xyz);
                float3 distanceXZ = posWS.xyz - _WorldSpaceCameraPos.xyz;
                distanceXZ.y = 0;
                float distXZ = length(distanceXZ);
                float distFactor = min(distXZ, _MaxCurveDistance) / max(_MaxCurveDistance, 0.0001);

                // Vertical curvature
                float offsetY = -distFactor * distFactor * _CurveValue * _MaxCurveDistance;
                offsetY = max(offsetY, _MinCurveHeight - posWS.y);

                // Lateral curvature
                float3 cameraForward = normalize(float3(_WorldSpaceCameraPos.x, 0, _WorldSpaceCameraPos.z) - posWS);
                float3 lateralDir = cross(float3(0, 1, 0), cameraForward);
                float offsetLateral = distFactor * distFactor * _LateralCurve * _MaxCurveDistance;
                float3 modifiedPosWS = posWS + float3(offsetLateral * lateralDir.x, offsetY, offsetLateral * lateralDir.z);

                // Transform normal for curvature
                float3 normalWS = TransformObjectToWorldNormal(input.normal);
                // Approximate normal based on curvature direction
                float3 curveDir = normalize(float3(offsetLateral * lateralDir.x, offsetY, offsetLateral * lateralDir.z));
                float3 modifiedNormalWS = normalize(normalWS - curveDir * _CurveNormalOffset);

                modifiedPosWS.y = max(modifiedPosWS.y, _MinCurveHeight);

                output.positionCS = TransformWorldToHClip(modifiedPosWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = modifiedNormalWS;
                output.viewDirWS = GetWorldSpaceViewDir(modifiedPosWS);
                output.shadowCoord = TransformWorldToShadowCoord(modifiedPosWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample main texture
                half4 sample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Normalize inputs
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDirWS = mainLight.direction;
                half3 lightColor = mainLight.color;
                half shadow = MainLightRealtimeShadow(input.shadowCoord);
                half NdotL = dot(normalWS, lightDirWS);
                half lightIntensity = smoothstep(0, 0.01, NdotL * shadow);
                half4 light = lightIntensity * half4(lightColor, 1);

                // Ambient light
                half4 ambient = _AmbientColor;

                // Specular
                float3 halfVector = normalize(lightDirWS + viewDirWS);
                float NdotH = dot(normalWS, halfVector);
                float specularIntensity = pow(abs(NdotH) * lightIntensity, _Glossiness * _Glossiness);
                float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
                half4 specular = specularIntensitySmooth * _SpecularColor;

                // Rim lighting
                float rimDot = 1 - dot(viewDirWS, normalWS);
                float rimIntensity = rimDot * pow(abs(NdotL), _RimThreshold);
                rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
                half4 rim = rimIntensity * _RimColor;

                // Final color
                half4 finalColor = _Color * sample * (ambient + light + specular + rim);
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

            struct Attributes
            {
                float4 position : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float _CurveValue;
            float _LateralCurve;
            float _MaxCurveDistance;
            float _MinCurveHeight;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.position.xyz);
                float3 distanceXZ = posWS.xyz - _WorldSpaceCameraPos.xyz;
                distanceXZ.y = 0;
                float distXZ = length(distanceXZ);
                float distFactor = min(distXZ, _MaxCurveDistance) / max(_MaxCurveDistance, 0.0001);

                float offsetY = -distFactor * distFactor * _CurveValue * _MaxCurveDistance;
                offsetY = max(offsetY, _MinCurveHeight - posWS.y);

                float3 cameraForward = normalize(float3(_WorldSpaceCameraPos.x, 0, _WorldSpaceCameraPos.z) - posWS);
                float3 lateralDir = cross(float3(0, 1, 0), cameraForward);
                float offsetLateral = distFactor * distFactor * _LateralCurve * _MaxCurveDistance;
                float3 modifiedPosWS = posWS + float3(offsetLateral * lateralDir.x, offsetY, offsetLateral * lateralDir.z);

                modifiedPosWS.y = max(modifiedPosWS.y, _MinCurveHeight);

                output.positionCS = TransformWorldToHClip(modifiedPosWS);
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