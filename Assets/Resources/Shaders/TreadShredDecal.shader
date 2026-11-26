// D-039-AC-03 and D-027-AC-02: soft flat marks on the ground (contact shadows, scorch marks, blast rings). Unlit,
// alpha-blended: the texture's alpha shapes the mark and _Color sets its colour and strength. Drawn after the opaque
// scene so a tank's body hides the part of its shadow beneath it. Kept under Resources so it ships in every build.
Shader "TreadShred/Decal"
{
    Properties
    {
        _MainTex ("Shape (alpha)", 2D) = "white" {}
        _Color ("Color", Color) = (0, 0, 0, 0.75)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(_Color.rgb, _Color.a * tex2D(_MainTex, i.uv).a);
            }
            ENDCG
        }
    }
}
