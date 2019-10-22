Shader "Unlit/InstanciatedRandomColor"
{
	Properties
	{
        _ColorMin ("ColorMin", Color) = (0, 0, 0, 1)
        _ColorMax ("ColorMax", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma multi_compile_instancing
			
            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

			struct appdata
			{
				float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float4 color : COLOR0;
			};

            float4 _ColorMin;
            float4 _ColorMax;

			v2f vert (appdata v)
			{
				v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
				o.vertex = UnityObjectToClipPos(v.vertex);

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
                float r = sin(float(unity_InstanceID));
                float4 r4 = r * float4(77716.29635, 96473.81282, 41377.31262, 18131.834230);
                o.color = lerp(_ColorMin, _ColorMax, frac(r4));
#else
                o.color = _ColorMax;
#endif
                return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                return i.color;
			}
			ENDCG
		}
	}
}
