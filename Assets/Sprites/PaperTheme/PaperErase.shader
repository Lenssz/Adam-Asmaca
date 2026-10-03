Shader "Paper Menu/Erase Page"
{
    Properties
    {
        [PerRendererData] _MainTex("Page snapshot",2D)="white"{}
        _PaperTex("Clean notebook",2D)="white"{}
        _Progress("Erase progress",Range(0,1))=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            sampler2D _MainTex,_PaperTex; float _Progress;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float noise=sin(i.uv.x*79+sin(i.uv.y*87))* .008 + sin(i.uv.x*163)*.004;
                float front=1-i.uv.y+noise;
                float erased=_Progress<=0?0:(_Progress>=1?1:smoothstep(front-.018,front+.025,_Progress));
                float4 old=tex2D(_MainTex,i.uv),paper=tex2D(_PaperTex,i.uv);
                // A faint graphite residue is briefly left behind the moving eraser, then removed.
                float residue=(1-smoothstep(front+.025,front+.10,_Progress))*.12*erased;
                return float4(lerp(old.rgb,paper.rgb,erased)*(1-residue*.12),1);
            }
            ENDCG
        }
    }
}
