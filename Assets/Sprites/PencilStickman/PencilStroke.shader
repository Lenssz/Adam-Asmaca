Shader "Pencil Stickman/Ordered Stroke"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _StrokeMask ("Drawing order (linear)", 2D) = "black" {}
        _DrawProgress ("Progress", Range(0,1)) = 1
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_StrokeMask); SAMPLER(sampler_StrokeMask);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _DrawProgress;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float2 encoded = SAMPLE_TEXTURE2D(_StrokeMask, sampler_StrokeMask, input.uv).rg;
                float order = (encoded.r * 255.0 * 256.0 + encoded.g * 255.0) / 65535.0;
                // Endpoint branches preserve the original RGBA exactly at completion.
                float visible = _DrawProgress >= 1 ? 1 : (_DrawProgress <= 0 ? 0 : smoothstep(order - 0.002, order + 0.002, _DrawProgress));
                color.a *= visible;
                return color;
            }
            ENDHLSL
        }
    }
}
