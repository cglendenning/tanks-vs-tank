// D-039-AC-03: a solid outline around each tank so it reads on every ground. The mesh is drawn again, pushed out
// along its (smoothed) normals by _Width world units, over the ground whatever the depth: seen nearly straight down,
// a tank's vertical sides would otherwise hide the rim behind the ground. The tank's own body draws after it
// (TreadShredTankLook raises its queue), covering all but the rim. Kept under Resources so it ships in every build.
Shader "TreadShred/Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Width ("Width (world units)", Float) = 0.05
    }
    SubShader
    {
        Tags { "Queue" = "Geometry+1" "RenderType" = "Opaque" }
        Pass
        {
            Cull Back
            ZTest Always
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Width;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                world += normalize(UnityObjectToWorldNormal(v.normal)) * _Width;
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
