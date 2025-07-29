Shader "Infinite Lands/DisplacementParticle"
{
    Properties
    {
        _Strength ("Strenght", Float) = 1
    }
    SubShader
    {
        ZWrite off
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Fade"}
        Pass
        {
            ColorMask G
            BlendOp Min
            Blend One One 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Common.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float4 vertex : SV_POSITION;
            };
            float2 _DisplaceTextureBoundary;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            float _Strength;
            fixed4 frag (v2f i) : SV_Target
            {
                float2 dist = i.uv-0.5;
                float strength = saturate((1-length(dist/.5))*_Strength);
                clip(strength-.01);
                return invLerp(_DisplaceTextureBoundary.x, _DisplaceTextureBoundary.y, i.uv.w);
            }
            ENDCG
        }
        Pass
        {
            Blend One One
            BlendOp Max
            ColorMask A
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };
            float _Strength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float2 dist = i.uv-0.5;
                float strength = saturate((1-length(dist/.5))*_Strength);
                return strength*i.color;
            }
            ENDCG


        } 
        Pass
        {
            ColorMask RB
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Packages\com.sapra.infinitelands\Runtime\Resources\Include\Random.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 centerandlifetime : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 centerandlifetime : TEXCOORD1;

            };
            float _Strength;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv.xy;
                o.color = v.color;
                o.centerandlifetime = v.centerandlifetime;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 dist = i.uv-0.5;                
                float strength = saturate((1-length(dist/.5))*_Strength*10);
                float3 idHash = randValueIndex(i.centerandlifetime.w);
                float modify = sin((i.centerandlifetime.y/i.centerandlifetime.z)*10)*(.5f+idHash*.1f)*(1-i.centerandlifetime.y);
                float2 result = lerp(i.uv, ((dist*modify)+.5), saturate(i.centerandlifetime.y*2+idHash*.2-.5));
                clip(strength-.01);
                return float4(result.x, 0, result.y,strength);
            }
            ENDCG
        } 
    }
}