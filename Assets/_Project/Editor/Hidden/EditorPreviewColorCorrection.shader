Shader "Hidden/EditorPreviewColorCorrection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Exposure ("Exposure", Float) = 0
        _Contrast ("Contrast", Float) = 1
        _Saturation ("Saturation", Float) = 1
        _Tint ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float _Exposure;
            float _Contrast;
            float _Saturation;
            float4 _Tint;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb;

                // Exposure
                col *= pow(2.0, _Exposure);

                // Contrast
                col = ((col - 0.5) * _Contrast) + 0.5;

                // Saturation
                float luminance = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(luminance.xxx, col, _Saturation);

                // Tint
                col *= _Tint.rgb;

                return float4(col, 1);
            }
            ENDHLSL
        }
    }
}
