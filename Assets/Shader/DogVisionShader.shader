Shader "Custom/DogVisionShader_CleanEdges"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // 调整为更亮、更透的天蓝色
        _VisionColor ("Vision Color (Sky Blue)", Color) = (0.2, 0.7, 1.0, 1) 
        _EdgeColor ("Edge Color (Yellow)", Color) = (1, 0.9, 0, 1)
        
        // 深度敏感度：数值越小，对距离变化越敏感
        _DepthThreshold ("Depth Threshold", Range(0.001, 0.1)) = 0.01
        // 法线敏感度：数值越小，对角度变化越敏感
        _NormalThreshold ("Normal Threshold", Range(0.01, 1.0)) = 0.1
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

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            // Unity 自动提供的深度法线纹理
            sampler2D _CameraDepthNormalsTexture; 
            
            float4 _VisionColor;
            float4 _EdgeColor;
            float _DepthThreshold;
            float _NormalThreshold;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // 获取特定UV点的深度和法线信息
            void GetDepthNormal(float2 uv, out float depth, out float3 normal) {
                float4 depthnormal = tex2D(_CameraDepthNormalsTexture, uv);
                // 解码 Unity 压缩的深度和法线数据
                DecodeDepthNormal(depthnormal, depth, normal);
            }

            // 比较两点的深度和法线差异，返回边缘强度(0或1)
            float CheckEdge(float2 uv, float2 offset) {
                float depth1, depth2;
                float3 normal1, normal2;
                
                GetDepthNormal(uv, depth1, normal1);
                GetDepthNormal(uv + offset, depth2, normal2);
                
                // 1. 计算深度差异
                float depthDiff = abs(depth1 - depth2);
                // 深度边缘判定
                bool isDepthEdge = depthDiff > _DepthThreshold;
                
                // 2. 计算法线（角度）差异
                float3 normalDiff = abs(normal1 - normal2);
                // 法线边缘判定 (所有轴向差异之和)
                bool isNormalEdge = (normalDiff.x + normalDiff.y + normalDiff.z) > _NormalThreshold;
                
                // 只要满足一种，就是干净的边缘
                return (isDepthEdge || isNormalEdge) ? 1.0 : 0.0;
            }

            float4 frag (v2f i) : SV_Target {
                float4 original = tex2D(_MainTex, i.uv);
                
                // --- 1. 干净的边缘检测 (深度+法线) ---
                float2 delta = _MainTex_TexelSize.xy;
                float edge = 0;
                
                // 简单的十字采样
                edge += CheckEdge(i.uv, float2(delta.x, 0));
                edge += CheckEdge(i.uv, float2(-delta.x, 0));
                edge += CheckEdge(i.uv, float2(0, delta.y));
                edge += CheckEdge(i.uv, float2(0, -delta.y));
                
                edge = saturate(edge); // 钳制在0-1之间

                // --- 2. 优化后的色彩过滤 (天蓝色) ---
                // 计算亮度
                float luma = dot(original.rgb, float3(0.2126, 0.7152, 0.0722));
                
                // 使用亮度作为混合系数，让天蓝色更通透，暗部不发死
                float4 skyBlueVision = lerp(original * 0.2, _VisionColor, luma * 1.5 + 0.2);
                // 补偿一点全局亮度，让场景整体亮起来
                skyBlueVision += original * 0.1; 

                // --- 3. 混合结果 ---
                // 使用线性差值混合边缘，让边缘稍微柔和一点点
                float4 finalColor = lerp(skyBlueVision, _EdgeColor, edge);
                
                return finalColor;
            }
            ENDCG
        }
    }
}