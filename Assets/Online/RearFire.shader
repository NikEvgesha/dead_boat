Shader "DeadBoat/RearFire"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            Output vert(Input v)
            {
                Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(Output i):SV_Target
            {
                float y=saturate(i.uv.y);
                float bend=.07*sin(y*12-_Time.y*6);
                float edge=abs(i.uv.x-.5+bend)-.43*(1-y);
                float alpha=(1-smoothstep(-.03,.04,edge))*smoothstep(0,.12,y)*(1-smoothstep(.8,1,y));
                fixed3 color=lerp(fixed3(1,.9,.2),fixed3(1,.12,.01),y);
                return fixed4(color,alpha*i.color.a);
            }
            ENDCG
        }
    }
}
