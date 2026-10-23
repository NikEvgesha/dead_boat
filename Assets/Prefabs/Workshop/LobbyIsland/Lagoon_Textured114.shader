Shader "Workshop/LagoonTextured114" {
Properties { _MainTex("Water texture",2D)="white"{} _Color("Tint",Color)=(.65,.92,1,1) _TileMeters("Tile size",Float)=18 }
SubShader { Tags {"RenderType"="Opaque" "Queue"="Geometry"} ZWrite On ZTest LEqual Cull Back
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata {float4 vertex:POSITION;};struct v2f{float4 pos:SV_POSITION;float3 world:TEXCOORD0;};sampler2D _MainTex;fixed4 _Color;float _TileMeters;
v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
fixed4 frag(v2f i):SV_Target{float2 uv=i.world.xz/_TileMeters;fixed3 a=tex2D(_MainTex,uv).rgb;fixed3 b=tex2D(_MainTex,uv*.63+float2(.27,.43)).rgb;fixed3 c=lerp(a,b,.28)*_Color.rgb;float wave=sin(i.world.x*.9+i.world.z*.43+_Time.y*.65)*sin(i.world.z*1.1-i.world.x*.21-_Time.y*.4);c+=pow(saturate(wave),12)*.055;return fixed4(c,1);}
ENDCG } } }
