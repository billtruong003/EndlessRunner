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
        [HDR] _ToonLitColor("Toon Lit Color", Color) = (1, 1, 1, 1)
        [HDR] _ToonShadowColor("Toon Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _ToonThreshold("Toon Threshold", Range(0, 1)) = 0.5
        _ToonSmoothness("Toon Smoothness", Range(0.001, 0.5)) = 0.05
        _CurveValue("Vertical Curve", Range(-10, 10)) = 0.01
        _LateralCurve("Lateral Curve", Range(-10, 10)) = 0
        _MaxCurveDistance("Max Curve Distance", Float) = 100
        _MinCurveHeight("Min Curve Height", Float) = -100 // Prevent clipping below this height
        _CurveNormalOffset("Curve Normal Offset", Range(0, 1)) = 0.1 // Offset for normal adjustment due to curvature
        _OutlineThickness("Outline Thickness", Range(0, 0.1)) = 0.01
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineDistanceFactor("Outline Distance Factor", Range(0, 2)) = 0.5 // How much distance affects outline thickness
        _OutlineCurveFactor("Outline Curve Factor", Range(0, 2)) = 0.2 // How much curvature affects outline thickness
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "Queue" = "Geometry" }
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
                half4 _ToonLitColor;
                half4 _ToonShadowColor;
                float _ToonThreshold;
                float _ToonSmoothness;
                half4 _EmissionColor;
                half _EmissionIntensity;
                float _CurveValue;
                float _LateralCurve;
                float _MaxCurveDistance;
                float _MinCurveHeight;
                float _CurveNormalOffset;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 distanceXZ = posWS.xyz - _WorldSpaceCameraPos.xyz;
                distanceXZ.y = 0;
                float distXZ = length(distanceXZ);
                float distFactor = min(distXZ, _MaxCurveDistance) / _MaxCurveDistance;

                // Vertical curvature with clamping to simulate a downward curve as distance increases
                float offsetY = -distFactor * distFactor * _CurveValue * _MaxCurveDistance;
                offsetY = max(offsetY, _MinCurveHeight - posWS.y); // Prevent clipping below min height

                // Lateral curvature (Subway Surfers-style) to bend the world sideways
                float3 cameraForward = normalize(float3(_WorldSpaceCameraPos.x, 0, _WorldSpaceCameraPos.z) - posWS);
                float3 lateralDir = cross(float3(0, 1, 0), cameraForward);
                float offsetLateral = distFactor * distFactor * _LateralCurve * _MaxCurveDistance;
                float3 modifiedPosWS = posWS + float3(offsetLateral * lateralDir.x, offsetY, offsetLateral * lateralDir.z);

                // Transform normal to account for curvature, adjusting lighting to match the bent surface
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float3 modifiedNormalWS = normalize(normalWS + float3(offsetLateral * _CurveNormalOffset, -offsetY * _CurveNormalOffset, 0));

                // Ensure position stays within reasonable bounds to avoid extreme deformation
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

                // Get main light with shadow coord
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDirWS = mainLight.direction;
                half NdotL = saturate(dot(normalWS, lightDirWS));
                half toonFactor = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, NdotL);
                toonFactor *= mainLight.shadowAttenuation;
                half3 toonShadedColor = lerp(_ToonShadowColor.rgb, _ToonLitColor.rgb, toonFactor);

                // Final color with ambient and emission
                half3 finalColor = albedo.rgb * toonShadedColor + _AmbientColor.rgb + emission;
                return half4(finalColor, 1);
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

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float _OutlineThickness;
                float4 _OutlineColor;
                float _OutlineDistanceFactor;
                float _OutlineCurveFactor;
            CBUFFER_END

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 distanceXZ = posWS.xyz - _WorldSpaceCameraPos.xyz;
                distanceXZ.y = 0;
                float distXZ = length(distanceXZ);
                float distFactor = min(distXZ, _MaxCurveDistance) / _MaxCurveDistance;

                // Vertical curvature with clamping
                float offsetY = -distFactor * distFactor * _CurveValue * _MaxCurveDistance;
                offsetY = max(offsetY, _MinCurveHeight - posWS.y);

                // Lateral curvature
                float3 cameraForward = normalize(float3(_WorldSpaceCameraPos.x, 0, _WorldSpaceCameraPos.z) - posWS);
                float3 lateralDir = cross(float3(0, 1, 0), cameraForward);
                float offsetLateral = distFactor * distFactor * _LateralCurve * _MaxCurveDistance;
                float3 modifiedPosWS = posWS + float3(offsetLateral * lateralDir.x, offsetY, offsetLateral * lateralDir.z);

                // Ensure position stays within bounds
                modifiedPosWS.y = max(modifiedPosWS.y, _MinCurveHeight);

                // Offset along normal for outline, adjusted by curvature and distance factors for better adaptation
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float curveImpact = abs(offsetLateral) * _OutlineCurveFactor; // Impact of lateral curve on outline
                float adjustedThickness = _OutlineThickness * (1.0 + distFactor * _OutlineDistanceFactor + curveImpact);
                adjustedThickness = clamp(adjustedThickness, _OutlineThickness * 0.5, _OutlineThickness * 1.5); // Limit variation
                modifiedPosWS += normalWS * adjustedThickness;

                OUT.positionCS = TransformWorldToHClip(modifiedPosWS);
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}