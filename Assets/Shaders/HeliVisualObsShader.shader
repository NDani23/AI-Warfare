Shader "Custom/MLAgentsTeamColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
            // Per-object uniform
            float _TeamStatus; // 0 = Environment, 1 = Teammate, 2 = Opponent

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                if (_TeamStatus == 1) // Teammate (green)
                    return fixed4(0, 1, 0, 1);
                else if (_TeamStatus == 2) // Opponent (red)
                    return fixed4(1, 0, 0, 1);
                else // Environment (grayscale)
                {
                    float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                    return fixed4(gray, gray, gray, 1);
                }
            }
            ENDCG
        }
    }
}