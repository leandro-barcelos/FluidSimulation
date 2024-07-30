Shader "Custom/2DDensityInfo"
{
    SubShader {
        Tags { "RenderType" = "Transparent" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Colors)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata_t i, uint instanceID : SV_InstanceID) {
                UNITY_SETUP_INSTANCE_ID(i);

                v2f o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.color = float4(1, 1, 1, 1);

                #ifdef UNITY_INSTANCING_ENABLED
                    o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Colors);
                #endif

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return i.color;
            }

            ENDCG
        }
    }
}
