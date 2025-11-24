Shader "Custom/MaskHole_Footprints"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

        Pass
        {
            Stencil
            {
                Ref 1
                Comp always
                Pass replace
            }

            ColorMask 0   // non scrive colore
            ZWrite Off    // non scrive depth

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // UV da 0–1, centro del quad = (0.5, 0.5)
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);

                // Se fuori dal cerchio → NON scrivere nello stencil
                if (dist > 0.5)
                    discard;

                return float4(0,0,0,0); // invisibile
            }
            ENDCG
        }
    }
}
