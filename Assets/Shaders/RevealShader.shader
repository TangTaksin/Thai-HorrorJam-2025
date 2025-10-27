Shader "CustomRenderTexture/RevealShader"
{
    Properties
    {
        [Header(Top Erasable Layer)]
        _MainTex ("Top Texture (Erasable)", 2D) = "white" {}

        [Header(Hidden Layer)]
        _RevealTex ("Hidden Texture (Revealed)", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100

        // เราต้องปิด ZWrite เพื่อให้วัตถุโปร่งใสทับซ้อนกันถูก
        ZWrite Off
        // ใช้การผสม Alpha แบบมาตรฐาน
        Blend SrcAlpha OneMinusSrcAlpha

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
            sampler2D _RevealTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // ใช้ UV เดียวกันสำหรับทั้งสอง Texture
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. อ่านสีจาก Texture ด้านบน (ที่ถูกลบ)
                fixed4 topColor = tex2D(_MainTex, i.uv);

                // 2. อ่านสีจาก Texture ด้านล่าง (ที่ซ่อนไว้)
                fixed4 revealColor = tex2D(_RevealTex, i.uv);

                // 3. นี่คือหัวใจสำคัญ :
                // ผสม (lerp) ระหว่างสีล่าง (revealColor) กับ สีบน (topColor)
                // โดยใช้ค่า Alpha (topColor.a) จาก Texture ด้านบนเป็นตัวกำหนด
                // - ถ้า topColor.a = 1 (ทึบ) -> ผลลัพธ์ = topColor
                // - ถ้า topColor.a = 0 (ใส) -> ผลลัพธ์ = revealColor
                fixed4 finalColor = lerp(revealColor, topColor, topColor.a);

                return finalColor;
            }
            ENDCG
        }
    }
}
