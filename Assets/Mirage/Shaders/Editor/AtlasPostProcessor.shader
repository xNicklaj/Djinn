// Copyright (c) 2025 Léo Chaumartin

Shader "Mirage/Internal/AtlasPostProcessor"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _DepthTex("ColorDepthMap (RGBA)", 2D) = "white" {}
    }
        SubShader
        {
            Pass
            {
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                uniform sampler2D _MainTex;
                uniform sampler2D _DepthTex;
                uniform half4 _MainTex_TexelSize;

                struct input
                {
                    float4 pos : POSITION;
                    half2 uv : TEXCOORD0;
                };

                struct output
                {
                    float4 pos : SV_POSITION;
                    half2 uv : TEXCOORD0;
                };


                output vert(input i)
                {
                    output o;
                    o.pos = UnityObjectToClipPos(i.pos);
                    o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, i.uv);
                    #if UNITY_UV_STARTS_AT_TOP
                    if (_MainTex_TexelSize.y < 0)
                            o.uv.y = 1 - o.uv.y;
                    #endif

                    return o;
                }

                fixed4 frag(output o) : COLOR
                {
                    float2 uv = o.uv;
                    float2 texelSize = _MainTex_TexelSize.xy;
                    
                    float4 colorCenter = tex2D(_MainTex, uv);
                    float depthCenter = tex2D(_DepthTex, uv).a;

                    float threshold = 0.5;
                
                    if (depthCenter > threshold)
                    {
                        return float4(colorCenter.rgb, depthCenter);
                    }
                    else
                    {
                       const int maxSteps = 256; // Limit number of samples to avoid infinite loops
                       const int maxRadius = 16; // Maximum radius of search

                       int2 dir = int2(1, 0); // Start moving right
                       int stepsTaken = 0;
                       int segmentLength = 1; // How many steps to take in current direction

                       int x = 0;
                       int y = 0;

                       int segmentPassed = 0;

                       for (int step = 0; step < maxSteps; ++step)
                       {
                           if (step > 0) // Skip center pixel (already tested)
                           {
                               float2 offset = float2(x, y) * texelSize;
                               float2 sampleUV = uv + offset;
                               float4 sampleColor = tex2D(_MainTex, sampleUV);
                               float sampleDepth = tex2D(_DepthTex, sampleUV).a;

                               if (sampleDepth > threshold)
                               {
                                   return float4(sampleColor.rgb, depthCenter);
                               }
                           }

                           x += dir.x;
                           y += dir.y;
                           stepsTaken++;

                           if (stepsTaken == segmentLength)
                           {
                               stepsTaken = 0;

                               int tmp = dir.x;
                               dir.x = -dir.y;
                               dir.y = tmp;

                               segmentPassed++;

                               if (segmentPassed == 2)
                               {
                                   segmentPassed = 0;
                                   segmentLength++;
                               }
                           }

                           if (abs(x) > maxRadius || abs(y) > maxRadius)
                               break;
                       }

                       return float4(0,0,0,0);
                    }
                }

                ENDCG
            }
        }
}