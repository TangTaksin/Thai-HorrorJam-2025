Shader "Custom/PaintOverlay"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _PaintTex ("Paint Overlay", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_PaintTex); SAMPLER(sampler_PaintTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 paintCol = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, i.uv);

                // Blend ตาม alpha ของ paint
                half3 finalCol = lerp(baseCol.rgb, paintCol.rgb, paintCol.a);

                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }
}