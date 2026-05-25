Shader "Hidden/test"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Saturation ("Saturation", Range(0.6, 1.2)) = 0.85
        _Brightness ("Brightness", Range(0.85, 1.15)) = 1.0
        _TintColor ("Tint Color", Color) = (0.97, 0.98, 0.99, 0.35)
        _ShadowTint ("Shadow Tint", Color) = (0.92, 0.94, 0.97, 0.35)
        _HighlightTint ("Highlight Tint", Color) = (1.0, 0.98, 0.94, 0.35)
        _OverallWarmthColor ("Overall Warmth Color", Color) = (0.98, 0.96, 0.92, 0.45)
        _ShadowReplacementColor ("Shadow Replacement Color", Color) = (0.36, 0.33, 0.28, 1.0)
        _ExposureComp ("Exposure Comp", Range(0.7, 1.3)) = 1.0
        _Exposure ("Exposure", Range(0.7, 1.5)) = 1.0
        _Contrast ("Soft Contrast", Range(0.0, 1.0)) = 0.35
        _BlackLevel ("Black Level", Range(0.0, 0.3)) = 0.15
        _GrainIntensity ("Grain Intensity", Range(0.0, 0.05)) = 0.005
        _GrainSize ("Grain Size", Range(0.5, 4.0)) = 1.0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Saturation;
            float _Brightness;
            float4 _TintColor;
            float4 _ShadowTint;
            float4 _HighlightTint;
            float4 _OverallWarmthColor;
            float4 _ShadowReplacementColor;
            float _ExposureComp;
            float _Exposure;
            float _Contrast;
            float _BlackLevel;
            float _GrainIntensity;
            float _GrainSize;

            float Luma(float3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            float Rand(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float3 SoftLight(float3 baseCol, float3 blendCol)
            {
                float3 low = 2.0 * baseCol * blendCol + baseCol * baseCol * (1.0 - 2.0 * blendCol);
                float3 high = sqrt(baseCol) * (2.0 * blendCol - 1.0) + 2.0 * baseCol * (1.0 - blendCol);
                return lerp(low, high, step(0.5, blendCol));
            }

            float3 Overlay(float3 baseCol, float3 blendCol)
            {
                float3 low = 2.0 * baseCol * blendCol;
                float3 high = 1.0 - 2.0 * (1.0 - baseCol) * (1.0 - blendCol);
                return lerp(low, high, step(0.5, baseCol));
            }

            float3 ACESFitted(float3 x)
            {
                const float a = 2.51;
                const float b = 0.03;
                const float c = 2.43;
                const float d = 0.59;
                const float e = 0.14;
                return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // 基础柔化：轻微降饱和与中间调压缩，保留细节
                float luma = Luma(col.rgb);
                col.rgb = lerp(luma.xxx, col.rgb, _Saturation);
                float softLuma = smoothstep(0.08, 0.92, luma);
                col.rgb = lerp(col.rgb, col.rgb * (softLuma / max(luma, 1e-4)), 0.6);

                // 全局暖色统一：使用 Soft Light 进行暖色罩染
                col.rgb = lerp(col.rgb, SoftLight(col.rgb, _OverallWarmthColor.rgb), _OverallWarmthColor.a);

                // 阴影替换：最暗处平滑映射到温暖暗色
                float shadowMask = smoothstep(0.0, 0.35, luma);
                col.rgb = lerp(_ShadowReplacementColor.rgb, col.rgb, shadowMask);

                // 细微的高光/阴影偏色，仍保持 Soft Light
                col.rgb = lerp(col.rgb, SoftLight(col.rgb, _ShadowTint.rgb), _ShadowTint.a);
                col.rgb = lerp(col.rgb, SoftLight(col.rgb, _HighlightTint.rgb), _HighlightTint.a);

                // 曝光补偿与色调映射：Reinhard 压缩极亮与极暗
                col.rgb *= (_Exposure * _ExposureComp);
                col.rgb = col.rgb / (1.0 + col.rgb);

                // 柔和对比度：S 曲线集中中间调
                float3 softCurve = col.rgb * col.rgb * (3.0 - 2.0 * col.rgb);
                col.rgb = lerp(col.rgb, softCurve, _Contrast);

                fixed4 outCol = col;

                // 纸质微粒：基于时间的极细腻噪点，Overlay 混合
                float2 grainUV = (i.uv * _MainTex_TexelSize.zw) / max(_GrainSize, 0.001);
                float grain = Rand(grainUV + _Time.x) - 0.5;
                float3 grainOverlay = Overlay(outCol.rgb, outCol.rgb + grain);
                outCol.rgb = lerp(outCol.rgb, grainOverlay, _GrainIntensity);

                return outCol;
            }
            ENDCG
        }
    }
}
