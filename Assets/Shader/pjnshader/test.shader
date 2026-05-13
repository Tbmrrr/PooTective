Shader "Hidden/test"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.01, 6)) = 1
        _DepthThreshold ("Depth Threshold", Range(0.001, 0.1)) = 0.01
        _NormalThreshold ("Normal Threshold", Range(0.01, 1.0)) = 0.1
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
            sampler2D _CameraDepthNormalsTexture;
            float4 _MainTex_TexelSize;
            float4 _OutlineColor;
            float _OutlineThickness;
            float _DepthThreshold;
            float _NormalThreshold;

            void GetDepthNormal(float2 uv, out float depth, out float3 normal)
            {
                float4 dn = tex2D(_CameraDepthNormalsTexture, uv);
                DecodeDepthNormal(dn, depth, normal);
            }

            float CheckEdge(float2 uv, float2 offset)
            {
                float depth1;
                float depth2;
                float3 normal1;
                float3 normal2;

                GetDepthNormal(uv, depth1, normal1);
                GetDepthNormal(uv + offset, depth2, normal2);

                float depthDiff = abs(depth1 - depth2);
                float3 normalDiff = abs(normal1 - normal2);

                float thickness = max(_OutlineThickness, 0.01);
                float depthThresh = _DepthThreshold * thickness;
                float normalThresh = _NormalThreshold * thickness;

                bool isDepthEdge = depthDiff > depthThresh;
                bool isNormalEdge = (normalDiff.x + normalDiff.y + normalDiff.z) > normalThresh;

                return (isDepthEdge || isNormalEdge) ? 1.0 : 0.0;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float thickness = max(_OutlineThickness, 0.01);
                float2 texel = _MainTex_TexelSize.xy * thickness;
                texel = max(texel, _MainTex_TexelSize.xy);
                float2 offsetX = float2(texel.x, 0);
                float2 offsetY = float2(0, texel.y);

                float edge = 0;
                edge += CheckEdge(i.uv, offsetX);
                edge += CheckEdge(i.uv, -offsetX);
                edge += CheckEdge(i.uv, offsetY);
                edge += CheckEdge(i.uv, -offsetY);
                edge = saturate(edge);
                fixed4 outline = _OutlineColor;
                return lerp(col, outline, edge * outline.a);
            }
            ENDCG
        }
    }
}
