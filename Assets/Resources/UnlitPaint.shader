Shader "Custom/UnlitPaint"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BrushTex ("Brush Texture", 2D) = "white" {}
        _BrushColor ("Brush Color", Color) = (1,0,0,1)
        _UVPos ("UV Pos & Size", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            sampler2D _BrushTex;
            float4 _BrushColor;
            float4 _UVPos;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv);

                float2 delta = i.uv - _UVPos.xy;
                float dist = length(delta);

                float2 brushUV = delta / _UVPos.z + 0.5;
                fixed4 brushCol = tex2D(_BrushTex, brushUV) * _BrushColor;

                // ขอบแปรงนุ่มตามขนาดจริง
                brushCol.a *= smoothstep(_UVPos.z, 0, dist);
                brushCol.rgb *= brushCol.a;

                // blend สีเก่ากับสีใหม่
                baseCol.rgb = lerp(baseCol.rgb, brushCol.rgb, brushCol.a);
                return baseCol;
            }
            ENDCG
        }
    }
}
