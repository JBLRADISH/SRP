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
                float3 worldNormal : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float viewDepth : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Surface;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            CBUFFER_START(CustomShadow)
                UNITY_DECLARE_SHADOWMAP(_DirectionalShadowMap);
                float4x4 _DirectionalShadowMatrix[4];
                float _DirectionalShadowStrength;
                int _CascadeCount;
                float4 _CascadeCullingSpheres[4];
                float4 _ShadowDistanceFade;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewDepth = mul(unity_WorldToCamera, o.worldPos).z;
                return o;
            }

            struct surface
            {
                fixed3 worldNormal;
                fixed3 worldLight;
                fixed3 viewDir;
            };

            float Square(float v)
            {
                return v * v;
            }

            float DistanceSquare(float3 pa, float3 pb)
            {
                return dot(pa-pb, pa-pb);
            }

            #define MIN_REFLECTIVITY 0.04
            float OneMinusReflectivity()
            {
                float range = 1.0 - MIN_REFLECTIVITY;
                return range - _Metallic * range;
            }

            float SpecularStrength(surface surface)
            {
                fixed3 h = normalize(surface.worldLight + surface.viewDir);
                fixed nh2 = Square(saturate(dot(surface.worldNormal, h)));
                fixed lh2 = Square(saturate(dot(surface.worldLight, h)));
                fixed roughness = Square(1 - _Smoothness);
                float r2 = Square(roughness);
                float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
                float normalization = roughness * 4.0 + 2.0;
                return r2 / (d2 * max(0.1, lh2) * normalization);
            }

            fixed4 BRDF(surface surface)
            {
                fixed4 diffuse = fixed4(_Surface.rgb, 1.0) * OneMinusReflectivity() * _Surface.a;
                fixed4 specular = lerp(MIN_REFLECTIVITY, fixed4(_Surface.rgb, 1.0), _Metallic);
                return specular * SpecularStrength(surface) + diffuse;
            }

            int GetCascadeIndex(v2f i)
            {
                int j;
                for (j = 0; j < _CascadeCount; j++)
                {
                    float4 sphere = _CascadeCullingSpheres[j];
                    float distanceSqr = DistanceSquare(i.worldPos.xyz, sphere.xyz);
                    if (distanceSqr < sphere.w)
                    {
                        break;
                    }
                }
                return j;
            }

            float FadedShadowStrength(float distance, float scale, float fade)
            {
                return saturate((1.0 - distance * scale) * fade);
            }

            half Shadow(v2f i)
            {
                int cascadeIndex = GetCascadeIndex(i);
                if (cascadeIndex == _CascadeCount)
                {
                    return 1.0;
                }
                float strength = FadedShadowStrength(i.viewDepth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
                float4 shadowCoord = mul(_DirectionalShadowMatrix[cascadeIndex], i.worldPos);
                half shadow = UNITY_SAMPLE_SHADOW_PROJ(_DirectionalShadowMap, shadowCoord);
                shadow = lerp(1.0, shadow, _DirectionalShadowStrength * strength);
                return shadow;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                surface surface;
                surface.worldNormal = normalize(i.worldNormal);
                surface.worldLight = normalize(_WorldSpaceLightPos0.xyz);
                surface.viewDir = normalize(_WorldSpaceCameraPos - i.worldPos.xyz);
                fixed3 color = unity_LightColor0 * saturate(dot(surface.worldNormal, surface.worldLight)) * Shadow(i) * BRDF(surface);
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
#if UNITY_REVERSED_Z
	o.vertex.z = min(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
#else
	o.vertex.z = max(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
#endif
                return o;
            }

            void frag (v2f i)
            {
                
            }
            ENDCG
        }
    }
}
