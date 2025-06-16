Shader "Unlit/FogPlane"
{
    Properties
    {
        [Header(Fog Settings)]
        _Tint("Fog Tint", Color) = (1, 1, 1, .5)
        _FogStart("Fog Start Depth", Float) = 0.0
        _FogEnd("Fog End Depth", Float) = 10.0
        _FogDensity("Fog Density", Range(0, 5)) = 1

        [Header(Noise Settings)]
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.5
        _NoiseFrequency ("Noise Frequency", Float) = 1
        _NoiseSpeedX ("Noise Speed X", Float) = 1
        _NoiseSpeedY ("Noise Speed Y", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque"  "Queue" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
 
            #include "UnityCG.cginc"
 
            struct appdata
            {
                float4 vertex : POSITION;
            };
 
            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 scrPos : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                float depth : TEXCOORD4;
            };
 
            float4 _Tint;
            float _FogStart;
            float _FogEnd;
            float _FogDensity;

            sampler2D _NoiseTex;
            float _NoiseStrength;
            float _NoiseFrequency;
            float _NoiseSpeedX;
            float _NoiseSpeedY;
 
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.scrPos = ComputeScreenPos(o.vertex); // grab position on screen
                o.depth = -UnityObjectToViewPos(v.vertex).z;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
 
            fixed4 frag(v2f i) : SV_Target
            {
                half planeDepth = i.depth;

                // Calculate noise
                float2 noiseUV = i.worldPos.xz * _NoiseFrequency * 0.1;
                noiseUV += _Time.y * float2(_NoiseSpeedX, _NoiseSpeedY) * 0.01;
                half noise = tex2D(_NoiseTex, noiseUV).r;
                float noiseFactor = lerp(1.0, noise, _NoiseStrength);

                // Calculate fog amount based on distance from camera to the plane
                half fogAmount = saturate((planeDepth - _FogStart) / max(0.0001, _FogEnd - _FogStart));
                fogAmount *= _FogDensity * noiseFactor;
                
                half4 col = _Tint;
                col.a = saturate(col.a * fogAmount);
                
                UNITY_APPLY_FOG(i.fogCoord, col); // comment out this line if you want this fog to override the fog in lighting settings
                return col;
            }
            ENDCG
        }
    }
}