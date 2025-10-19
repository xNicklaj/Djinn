// Copyright (c) 2024 Léo Chaumartin
// The BiRP Unlit Impostor shader


Shader "Mirage/ImpostorUnlit"
{

    Properties
    {
        [HideInInspector] _MainTex("Albedo", 2D) = "white" {}
        [HideInInspector]_GridSize("Grid Size", float) = 64
        [HideInInspector]_LongitudeSamples("Longitude Samples", range(1, 64)) = 36
        [HideInInspector]_LongitudeOffset("Longitude Offset", range(0, 360)) = 0
        [HideInInspector]_LongitudeStep("Longitude Step", range(0.0, 180.0)) = 0.0
        [HideInInspector]_LatitudeSamples("Latitude Samples", range(0, 24)) = 3
        [HideInInspector]_LatitudeOffset("Latitude Offset", range(-24, 24)) = 0
        [HideInInspector]_LatitudeStep("Latitude Step", range(0.0, 90.0)) = 15.0
        [HideInInspector][MaterialToggle]_SmartSphere("Smart Sphere", Float) = 0

        [MaterialToggle]_BillboardingEnabled("Enabled", Float) = 1
        [MaterialToggle]_ClampBillboarding("Clamp Latitude", Float) = 1
        _ZOffset("Z Offset", range(0, 4)) = 0.5

        _Brightness("Brightness", range(0, 2)) = 1.0
        _Saturation("Saturation", range(0, 2)) = 1.0

        _Cutout("Cutout", range(0, 1)) = 0.355
        [MaterialToggle]_Smooth("Interpolation", Float) = 1
        _InterpolationSteepness("Steepness", range(-1, 1)) = 0.5
        [MaterialToggle]_DitheringFade("Dithering Fade", Float) = 0

        _YawOffset("Yaw Offset", range(0, 6.283185)) = 0.0
        _ElevationOffset("Elevation Offset", range(-1.570796, 1.570796)) = 0.0

    }
        SubShader
        {



            CGINCLUDE
        #pragma require integers
        #pragma target 3.0  
        #pragma multi_compile_instancing

            ENDCG
            Tags{ "RenderType" = "Opaque"}
            ZWrite On

            Cull Off // Renders both sides for two-sided shadows

            CGPROGRAM
            #pragma surface surf NoLighting vertex:vert alphatest:_Cutout addshadow fullforwardshadows dithercrossfade
            #define PI 3.14159
            #define TWO_PI 6.28318
            #define DEG2RAD 0.01745328

            fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
            {
                fixed4 c;
                c.rgb = s.Albedo;
                c.a = s.Alpha;
                return c;
            }


            struct Input {
              float2 uv_MainTex;
              float3 screenPos;
              float3 originViewDir;
              bool clamped;
            };

            float3 normalize(float3 v)
            {
                return rsqrt(dot(v, v)) * v;
            }

            sampler2D _MainTex;
            float _GridSize;
            uint _LongitudeSamples;
            float _LongitudeOffset;
            float _LongitudeStep;
            float _LatitudeSamples;
            float _LatitudeOffset;
            float _LatitudeStep;
            float _Brightness;
            float _Saturation;
            float _SmartSphere;
            float _Smooth;
            float _InterpolationSteepness;
            float _DitheringFade;
            float _YawOffset;
            float _ElevationOffset;
            float _BillboardingEnabled;
            float _ClampBillboarding;
            float _ZOffset;
            
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TreeInstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)


            #include "MirageGeometryHelper.hlsl"
            #include "MirageCoreHelper.hlsl"

            void vert(inout appdata_full v, out Input o) {
                bool clamped;
                Billboarding(v.vertex.xyz, v.normal.xyz, v.tangent.xyz, clamped);
                o.uv_MainTex = v.texcoord.xy;
                o.screenPos = ComputeScreenPos(v.vertex).xyz;
                o.originViewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz);
                o.clamped = clamped;
            }

            void surf(Input i, inout SurfaceOutput o)
            {
                float4 color, normal;
                float alpha, beta;
                float2 gridUv, gridUvLB, gridUvLT, gridUvRB, gridUvRT;
                GetImpostorUV_float(i.uv_MainTex, i.originViewDir, gridUv, gridUvLB, gridUvLT, gridUvRB, gridUvRT, alpha, beta);

                if (_Smooth > 0.5) {
                    ApplyAsinAtanSteepness_float(alpha, _InterpolationSteepness, alpha);
                    ApplyAsinAtanSteepness_float(beta, _InterpolationSteepness, beta);
                    float4 colorLB = tex2D(_MainTex, gridUvLB);
                    float4 colorRB = tex2D(_MainTex, gridUvRB);
                    float4 colorLT = tex2D(_MainTex, gridUvLT);
                    float4 colorRT = tex2D(_MainTex, gridUvRT);
                    float4 colorB = lerp(colorRB, colorLB, beta);
                    float4 colorT = lerp(colorRT, colorLT, beta);
                    color = lerp(colorT, colorB, alpha);
                }
                else {
                    color = tex2D(_MainTex, gridUv);
                }
                if (_DitheringFade > .5 && Dither(i.uv_MainTex, color.a * 2.0) < 0.)
                    discard;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                if (_TreeInstanceColor.r > 0.0) {
                    float3 hsv = RGBToHSV(color.rgb);
                    hsv.x += _TreeInstanceColor.r;
                    o.Emission = clamp(HSVToRGB(hsv) * _Brightness, 0.0, 1.0);
                }
                else
#endif
                    o.Emission = clamp(SaturateColor(color, _Saturation) * _Brightness, 0.0, 1.0);
                o.Alpha = color.a;
                o.Albedo = float4(0.0, 0.0, 0.0, 0.0);
            }
            ENDCG
        }
            CustomEditor "Mirage.Impostors.Elements.MirageUnlitShaderGUI"
}