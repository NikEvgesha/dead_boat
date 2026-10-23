Shader "Workshop/OccludedWorldUI114" {
Properties { [PerRendererData] _MainTex ("Font or sprite",2D)="white" {} _Color("Tint",Color)=(1,1,1,1) _FontOnly("Font alpha only",Float)=0 }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"} Cull Off Lighting Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
sampler2D _MainTex;fixed4 _Color;float _FontOnly;
v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
fixed4 frag(v2f i):SV_Target {fixed4 sample=tex2D(_MainTex,i.uv);fixed4 c=fixed4(lerp(sample.rgb,fixed3(1,1,1),_FontOnly),sample.a)*i.color;clip(c.a-.005);return c;}
ENDCG } } }
