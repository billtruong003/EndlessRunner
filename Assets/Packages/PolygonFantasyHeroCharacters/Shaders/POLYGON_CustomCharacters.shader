Shader "Shader Graphs/POLYGON_CustomCharacters_URP"
{
    Properties
    {
        _Color_Primary("Color_Primary", Color) = (0.2431373,0.4196079,0.6196079,0)
        _Color_Secondary("Color_Secondary", Color) = (0.8196079,0.6431373,0.2980392,0)
        _Color_Leather_Primary("Color_Leather_Primary", Color) = (0.282353,0.2078432,0.1647059,0)
        _Color_Metal_Primary("Color_Metal_Primary", Color) = (0.5960785,0.6117647,0.627451,0)
        _Color_Leather_Secondary("Color_Leather_Secondary", Color) = (0.372549,0.3294118,0.2784314,0)
        _Color_Metal_Dark("Color_Metal_Dark", Color) = (0.1764706,0.1960784,0.2156863,0)
        _Color_Metal_Secondary("Color_Metal_Secondary", Color) = (0.345098,0.3764706,0.3960785,0)
        _Color_Hair("Color_Hair", Color) = (0.2627451,0.2117647,0.1333333,0)
        _Color_Skin("Color_Skin", Color) = (1,0.8000001,0.682353,1)
        _Color_Stubble("Color_Stubble", Color) = (0.8039216,0.7019608,0.6313726,1)
        _Color_Scar("Color_Scar", Color) = (0.9294118,0.6862745,0.5921569,1)
        _Color_BodyArt("Color_BodyArt", Color) = (0.2283196,0.5822246,0.7573529,1)
        _Color_Eyes("Color_Eyes", Color) = (0.2283196,0.5822246,0.7573529,1)
        _Texture("Texture", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _Emission("Emission", Range(0, 1)) = 0
        _BodyArt_Amount("BodyArt_Amount", Range(0, 1)) = 0
        [HideInInspector]_Mask_01("Mask_01", 2D) = "white" {}
        [HideInInspector]_Mask_02("Mask_02", 2D) = "white" {}
        [HideInInspector]_Mask_03("Mask_03", 2D) = "white" {}
        [HideInInspector]_Mask_04("Mask_04", 2D) = "white" {}
        [HideInInspector]_Mask_05("Mask_05", 2D) = "white" {}
        [HideInInspector] _texcoord("", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry+0"
            "RenderPipeline" = "UniversalPipeline"
            "IsEmissive" = "true"
        }
        Cull Back
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            // Textures and samplers
            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);
            float4 _Texture_ST;

            TEXTURE2D(_Mask_01);
            SAMPLER(sampler_Mask_01);
            float4 _Mask_01_ST;

            TEXTURE2D(_Mask_02);
            SAMPLER(sampler_Mask_02);
            float4 _Mask_02_ST;

            TEXTURE2D(_Mask_03);
            SAMPLER(sampler_Mask_03);
            float4 _Mask_03_ST;

            TEXTURE2D(_Mask_04);
            SAMPLER(sampler_Mask_04);
            float4 _Mask_04_ST;

            TEXTURE2D(_Mask_05);
            SAMPLER(sampler_Mask_05);
            float4 _Mask_05_ST;

            // Properties
            float4 _Color_Primary;
            float4 _Color_Secondary;
            float4 _Color_Leather_Primary;
            float4 _Color_Metal_Primary;
            float4 _Color_Leather_Secondary;
            float4 _Color_Metal_Dark;
            float4 _Color_Metal_Secondary;
            float4 _Color_Hair;
            float4 _Color_Skin;
            float4 _Color_Stubble;
            float4 _Color_Scar;
            float4 _Color_BodyArt;
            float4 _Color_Eyes;
            float _Metallic;
            float _Smoothness;
            float _Emission;
            float _BodyArt_Amount;

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // UV transformations
                float2 uv_Texture = input.uv * _Texture_ST.xy + _Texture_ST.zw;
                float2 uv_Mask_01 = input.uv * _Mask_01_ST.xy + _Mask_01_ST.zw;
                float2 uv_Mask_02 = input.uv * _Mask_02_ST.xy + _Mask_02_ST.zw;
                float2 uv_Mask_03 = input.uv * _Mask_03_ST.xy + _Mask_03_ST.zw;
                float2 uv_Mask_04 = input.uv * _Mask_04_ST.xy + _Mask_04_ST.zw;
                float2 uv_Mask_05 = input.uv * _Mask_05_ST.xy + _Mask_05_ST.zw;

                // Sample textures
                float4 tex2DNode37 = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, uv_Texture);
                float4 tex2DNode156 = SAMPLE_TEXTURE2D(_Mask_01, sampler_Mask_01, uv_Mask_01);
                float4 tex2DNode158 = SAMPLE_TEXTURE2D(_Mask_02, sampler_Mask_02, uv_Mask_02);
                float4 tex2DNode160 = SAMPLE_TEXTURE2D(_Mask_03, sampler_Mask_03, uv_Mask_03);
                float4 tex2DNode162 = SAMPLE_TEXTURE2D(_Mask_04, sampler_Mask_04, uv_Mask_04);
                float4 tex2DNode179 = SAMPLE_TEXTURE2D(_Mask_05, sampler_Mask_05, uv_Mask_05);

                // Masking logic (replicating ASE MaskingFunction)
                float temp_output_25_0 = 0.5;
                float temp_output_22_0_g6 = step(tex2DNode156.r, temp_output_25_0);
                float4 lerpResult35 = lerp(tex2DNode37, _Color_Primary, temp_output_22_0_g6);

                float temp_output_22_0_g3 = step(tex2DNode156.g, temp_output_25_0);
                float4 lerpResult41 = lerp(lerpResult35, _Color_Secondary, temp_output_22_0_g3);

                float temp_output_22_0_g7 = step(tex2DNode162.r, temp_output_25_0);
                float4 lerpResult45 = lerp(lerpResult41, _Color_Leather_Primary, temp_output_22_0_g7);

                float temp_output_22_0_g9 = step(tex2DNode162.g, temp_output_25_0);
                float4 lerpResult65 = lerp(lerpResult45, _Color_Leather_Secondary, temp_output_22_0_g9);

                float temp_output_22_0_g10 = step(tex2DNode158.r, temp_output_25_0);
                float4 lerpResult124 = lerp(lerpResult65, _Color_Metal_Primary, temp_output_22_0_g10);

                float temp_output_22_0_g11 = step(tex2DNode158.g, temp_output_25_0);
                float4 lerpResult132 = lerp(lerpResult124, _Color_Metal_Secondary, temp_output_22_0_g11);

                float temp_output_22_0_g12 = step(tex2DNode158.b, temp_output_25_0);
                float4 lerpResult140 = lerp(lerpResult132, _Color_Metal_Dark, temp_output_22_0_g12);

                float temp_output_22_0_g14 = step(tex2DNode162.b, temp_output_25_0);
                float4 lerpResult49 = lerp(lerpResult140, _Color_Hair, temp_output_22_0_g14);

                float temp_output_22_0_g15 = step(tex2DNode160.r, temp_output_25_0);
                float4 lerpResult53 = lerp(lerpResult49, _Color_Skin, temp_output_22_0_g15);

                float temp_output_22_0_g16 = step(tex2DNode160.b, temp_output_25_0);
                float4 lerpResult57 = lerp(lerpResult53, _Color_Stubble, temp_output_22_0_g16);

                float temp_output_22_0_g18 = step(tex2DNode160.g, temp_output_25_0);
                float4 lerpResult61 = lerp(lerpResult57, _Color_Scar, temp_output_22_0_g18);

                float4 lerpResult181 = lerp(_Color_Eyes, lerpResult61, tex2DNode179.r);

                float4 temp_cast_0 = tex2DNode156.b;
                float4 color151 = float4(1, 1, 1, 0);
                float4 lerpResult152 = lerp(temp_cast_0, color151, (1.0 - _BodyArt_Amount));
                float4 lerpResult69 = lerp(_Color_BodyArt, lerpResult181, lerpResult152);

                // Emission
                float3 emission = (1.0 - tex2DNode179.r) * _Emission * lerpResult69.rgb;

                // PBR inputs for URP
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = GetShadowCoord(GetVertexPositionInputs(input.positionWS));

                // Surface data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = lerpResult69.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.emission = emission;
                surfaceData.alpha = 1.0;

                // URP lighting
                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
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

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = GetShadowCasterPositionCS(positionWS, normalWS);
                return output;
            }

            float4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}