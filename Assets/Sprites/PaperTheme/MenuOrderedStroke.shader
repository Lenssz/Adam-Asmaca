Shader "Paper Menu/Ordered UI Stroke"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StrokeMask ("Stroke order", 2D) = "black" {}
        _Progress ("Progress", Range(0,1)) = 1
        _Mode ("Profile mode", Float) = 0
        _DrawingRect ("Local rect", Vector) = (0,0,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex, _StrokeMask;
            fixed4 _Color, _TextureSampleAdd;
            float4 _DrawingRect, _ClipRect;
            float _Progress, _Mode;
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 color=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
                float order;
                if (_Mode > 1.5) order=i.uv.x*.55+(1-i.uv.y)*.45;
                else if (_Mode > .5)
                {
                    float2 encoded=tex2D(_StrokeMask,i.uv).rg;
                    order=(encoded.r*255*256+encoded.g*255)/65535;
                }
                else
                {
                    float2 size=max(_DrawingRect.zw-11,1);
                    float2 p=clamp(i.local.xy-_DrawingRect.xy-5.5,0,size);
                    float4 distances=float4(size.y-p.y,size.x-p.x,p.y,p.x);
                    float nearest=min(min(distances.x,distances.y),min(distances.z,distances.w));
                    float along;
                    if(distances.x<=nearest) along=p.x;
                    else if(distances.y<=nearest) along=size.x+size.y-p.y;
                    else if(distances.z<=nearest) along=size.x+size.y+size.x-p.x;
                    else along=2*size.x+size.y+p.y;
                    order=along/(2*(size.x+size.y));
                }
                color.a*=(_Progress>=1 ? 1 : (_Progress<=0 ? 0 : step(order,_Progress)));
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
