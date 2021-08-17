Shader "CustomRP/Light"
{
    Properties
    {
        _Surface("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "LightMode"="CustomLit" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed3 worldNormal : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Surface;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            CBUFFER_START(CustomLight)
                float3 LightColor;
            CBUFFER_END

            CBUFFER_START(CustomShadow)
                sampler2D _DirectionalShadowMap;
                float4x4 _DirectionalShadowMatrix;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldNormal = worldNormal;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            struct brdf
            {
                fixed3 worldNormal;
                fixed3 worldLight;
                fixed3 viewDir;
                fixed roughness;
            };

            float Square(float v)
            {
                return v * v;
            }

            #define MIN_REFLECTIVITY 0.04
            float OneMinusReflectivity()
            {
                float range = 1.0 - MIN_REFLECTIVITY;
                return range - _Metallic * range;
            }

            float SpecularStrength(brdf brdf)
            {
                fixed3 h = normalize(brdf.worldLight + brdf.viewDir);
                fixed nh2 = Square(saturate(dot(brdf.worldNormal, h)));
                fixed lh2 = Square(saturate(dot(brdf.worldLight, h)));
                float r2 = Square(brdf.roughness);
                float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
                float normalization = brdf.roughness * 4.0 + 2.0;
                return r2 / (d2 * max(0.1, lh2) * normalization);
            }

            fixed4 BRDF(brdf brdf)
            {
                fixed4 diffuse = fixed4(_Surface.rgb, 1.0) * OneMinusReflectivity() * _Surface.a;
                fixed4 specular = lerp(MIN_REFLECTIVITY, fixed4(_Surface.rgb, 1.0), _Metallic);
                return specular * SpecularStrength(brdf) + diffuse;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                brdf brdf;
                brdf.worldNormal = normalize(i.worldNormal);
                brdf.worldLight = normalize(_WorldSpaceLightPos0.xyz);
                brdf.viewDir = normalize(_WorldSpaceCameraPos - i.worldPos.xyz);
                brdf.roughness = Square(1 - _Smoothness);
                float4 shadowCoord = mul(_DirectionalShadowMatrix, i.worldPos);
                fixed3 color = unity_LightColor0 * saturate(dot(brdf.worldNormal, brdf.worldLight)) * BRDF(brdf);
                return fixed4(color, _Surface.a);
            }
            ENDCG
        }
        
        pass
        {
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            void frag (v2f i)
            {
                
            }
            ENDCG
        }
    }
}
