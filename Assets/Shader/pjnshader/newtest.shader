Shader "Hidden/WarmCozyPost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Warmth ("Warmth", Color) = (0.95, 0.92, 0.88, 0.6)
        _ShadowColor ("Shadow Color", Color) = (0.30, 0.26, 0.22, 1.0)
        _Brightness ("Brightness", Range(0.8, 1.2)) = 1.0
        _Contrast ("Contrast", Range(0.0, 0.6)) = 0.2
    }
    SubShader
    {
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
            float4 _Warmth;
            float4 _ShadowColor;
            float _Brightness;
            float _Contrast;

            float Luma(float3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            float3 SoftLight(float3 baseCol, float3 blendCol)
            {
                float3 low = 2.0 * baseCol * blendCol + baseCol * baseCol * (1.0 - 2.0 * blendCol);
                float3 high = sqrt(baseCol) * (2.0 * blendCol - 1.0) + 2.0 * baseCol * (1.0 - blendCol);
                return lerp(low, high, step(0.5, blendCol));
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
                float3 col = tex2D(_MainTex, i.uv).rgb;

                // 轻微去饱和，让色彩更像马卡龙插画
                float luma = Luma(col);
                col = lerp(luma.xxx, col, 0.9);

                // Soft Light 暖色罩染，避免直接相乘带来的生硬
                col = lerp(col, SoftLight(col, _Warmth.rgb), _Warmth.a);

                // 暗部替换：把最暗区域映射到温暖深色，避免纯黑
                float shadowMask = smoothstep(0.0, 0.4, luma);
                col = lerp(_ShadowColor.rgb, col, shadowMask);

                // 轻微朦胧感：四向采样做极弱模糊
                float2 texel = _MainTex_TexelSize.xy;
                float3 blur = tex2D(_MainTex, i.uv + float2(texel.x, 0)).rgb;
                blur += tex2D(_MainTex, i.uv - float2(texel.x, 0)).rgb;
                blur += tex2D(_MainTex, i.uv + float2(0, texel.y)).rgb;
                blur += tex2D(_MainTex, i.uv - float2(0, texel.y)).rgb;
                blur *= 0.25;
                col = lerp(col, blur, 0.08);

                // 亮度补偿（避免线性乘法）
                col = saturate(col + (_Brightness - 1.0));

                // ACES 色调映射，压缩高光与对比度
                col = ACESFitted(col);

                // 低强度对比度压缩，集中中间调
                float3 softCurve = col * col * (3.0 - 2.0 * col);
                col = lerp(col, softCurve, _Contrast);

                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}
