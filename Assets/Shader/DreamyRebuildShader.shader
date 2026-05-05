Shader "Custom/DreamyRebuild"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionSpeed ("Distortion Speed", Float) = 2.0
        _DistortionStrength ("Distortion Strength", Range(0, 0.05)) = 0.01
        _WaveFreq ("Wave Frequency", Float) = 10.0
        _DreamColor ("Dream Color (Tint)", Color) = (0.5, 0.4, 1.0, 1) // 蓝紫色
        _VignetteSoftness ("Edge Softness", Range(0.1, 1.0)) = 0.5
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

            sampler2D _MainTex;
            float _DistortionSpeed;
            float _DistortionStrength;
            float _WaveFreq;
            fixed4 _DreamColor;
            float _VignetteSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 计算边缘波动 (越靠近中心波动越小)
                float2 center = i.uv - 0.5;
                float distFromCenter = length(center);
                
                float wave = sin(i.uv.y * _WaveFreq + _Time.y * _DistortionSpeed) * _DistortionStrength;
                // 只在边缘应用波动
                float2 distortedUV = i.uv + (wave * distFromCenter);

                // 2. 采样原图
                fixed4 col = tex2D(_MainTex, distortedUV);

                // 3. 叠加蓝紫色调 (梦幻感)
                fixed3 dreamTint = lerp(col.rgb, col.rgb * _DreamColor.rgb, 0.4);
                
                // 4. 简单的暗角/模糊边缘效果
                float vignette = smoothstep(0.8, _VignetteSoftness, distFromCenter);
                dreamTint = lerp(dreamTint * 0.8, dreamTint, vignette);

                return fixed4(dreamTint, col.a);
            }
            ENDCG
        }
    }
}