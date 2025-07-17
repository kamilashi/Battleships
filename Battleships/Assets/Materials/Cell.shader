Shader "Unlit/Cell"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Radius("Radius", Range(0, 1)) = 0.5
        _Transparency("Transparency", Range(0, 2)) = 0.5
        _ScrollSpeed ("Scroll Speed", Vector) = (0.1, 0.1, 0, 0)
        _NoiseScale("NoiseScale", Range(0, 10)) = 0.5

        _Highlighted("Highlighted", Range(0, 1)) = 0.0
        _HighlightedColor ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"  }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "ShaderHelpers.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldObjPos : TEXCOORD1;
                float randId : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Radius;
            float _Transparency;
            float _Highlighted;
            float _NoiseScale;
            float4 _Color;
            float4 _HighlightedColor;

            Vector _ScrollSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float3 objWorldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                o.worldObjPos = objWorldPos;

                o.randId = (objWorldPos.x + objWorldPos.z  + 10.0f) / 3.0f;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float distance = UVtoRadius(i.uv, _Transparency);

                float alpha = 0.2f;
                float4 color = _HighlightedColor;

                if(_Highlighted < 0.5f)
                {
                    alpha = smoothstep(_Radius, 1.0f, distance);
    
                    float2 shiftedUV = i.uv + _ScrollSpeed.xy * _Time.y + i.randId.xx;
                    float noiseAlpha = noise(shiftedUV, _NoiseScale);

                    alpha *= noiseAlpha;
                    color = _Color;
                }

                return fixed4(color.xyz, alpha);
            }
            ENDCG
        }
    }
}
