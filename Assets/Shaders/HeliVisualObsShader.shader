Shader "Custom/MLAgentsHeliObs"
{
   Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RedThreshold ("Red Dominance Threshold", Range(0.5, 1)) = 0.7
        _GreenThreshold ("Green Dominance Threshold", Range(0.5, 1)) = 0.7
        _OtherMax ("Max Blue for Red/Green", Range(0, 0.5)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            float4 _MainTex_ST;
            float _RedThreshold;
            float _GreenThreshold;
            float _OtherMax;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                // Check if color is predominantly red (high red, low green/blue)
                bool isRed = col.r > _RedThreshold && col.g < _OtherMax && col.b < _OtherMax;
                
                // Check if color is predominantly green (high green, low red/blue)
                bool isGreen = col.g > _GreenThreshold && col.r < _OtherMax && col.b < _OtherMax;
                
                // If red or green, return original color
                if (isRed || isGreen)
                {
                    return col;
                }
                    
                // Convert to grayscale using luminance
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));

                //float gray = (col.r + col.g) * 0.35;
                
                // Output grayscale color, preserve alpha
                return fixed4(gray, gray, gray, 1);
            }
            ENDCG
        }
    }
}