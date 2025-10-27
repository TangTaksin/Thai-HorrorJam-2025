Shader "CustomRenderTexture/RevealShader_Lit"
{
    Properties
    {
        [Header(Top Erasable Layer)]
        _MainTex ("Top Texture (Erasable, RGBA)", 2D) = "white" {}
        _TopNormal ("Top Normal Map", 2D) = "bump" {}

        [Header(Hidden Layer)]
        _RevealTex ("Hidden Texture (Revealed, RGB)", 2D) = "black" {}
        _RevealNormal ("Hidden Normal Map", 2D) = "bump" {}

        [Header(PBR Settings)]
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }
    SubShader
    {
        // นี่คือ Shader แบบ Opaque (ทึบแสง)
        // "ความโปร่งใส" ของเราจริงๆ แล้วคือการ "ผสม Texture" ไม่ใช่การทำให้วัตถุใส
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM

        // นี่คือการบอกว่า ให้ใช้ระบบแสงแบบ Standard PBR และสร้างเงา
        #pragma surface surf Standard fullforwardshadows

        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _RevealTex;
        sampler2D _TopNormal;
        sampler2D _RevealNormal;

        struct Input
        {
            // เราจะใช้ UV ช่องเดียวกันสำหรับ Texture ทั้งหมด
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 1. อ่านค่าสีจาก Texture ทั้งสอง
            fixed4 topColor = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 revealColor = tex2D(_RevealTex, IN.uv_MainTex);

            // 2. อ่านค่า Normal Map จากทั้งสอง (ถ้ามี)
            fixed3 topNormal = UnpackNormal(tex2D(_TopNormal, IN.uv_MainTex));
            fixed3 revealNormal = UnpackNormal(tex2D(_RevealNormal, IN.uv_MainTex));

            // 3. นี่คือหัวใจสำคัญ : ดึงค่า Alpha จาก Texture ด้านบน
            // เพื่อใช้เป็นตัวผสม (blend factor)
            fixed blend = topColor.a; // (0 = ลบแล้ว, 1 = ยังไม่ลบ)

            // 4. ผสม (Lerp) ทุกอย่างเข้าด้วยกัน
            fixed3 finalAlbedo = lerp(revealColor.rgb, topColor.rgb, blend);
            fixed3 finalNormal = lerp(revealNormal, topNormal, blend);

            // 5. ส่งค่าทั้งหมดไปให้ระบบแสง PBR
            o.Albedo = finalAlbedo; // สีพื้นผิว
            o.Normal = finalNormal; // พื้นผิว (นูน / ลึก)
            o.Metallic = _Metallic; // ความเป็นโลหะ
            o.Smoothness = _Glossiness; // ความเนียน / มันวาว
            o.Alpha = 1.0; // วัตถุนี้ทึบแสง 100 %
        }
        ENDCG
    }
    FallBack "Diffuse"
}
