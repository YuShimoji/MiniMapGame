Shader "MiniMapGame/BuildingFade"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.22, 0.28, 0.38, 1)
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        [Header(Roof Fade)]
        _FadeStartDist ("Fade Start Distance", Float) = 20.0
        _FadeEndDist ("Fade End Distance", Float) = 8.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Global property set by C# (Shader.SetGlobalVector)
            float4 _MiniMapPlayerPosition;

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _Smoothness;
                float _FadeStartDist;
                float _FadeEndDist;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                // Roof detection: fragments facing upward
                bool isRoof = normalWS.y > 0.7;

                if (isRoof)
                {
                    // XZ distance from player (ignore height)
                    float dist = distance(input.positionWS.xz, _MiniMapPlayerPosition.xz);

                    // 1.0 = fully visible (far), 0.0 = fully clipped (close)
                    float fade = saturate((dist - _FadeEndDist) / max(_FadeStartDist - _FadeEndDist, 0.01));

                    // Interleaved gradient noise for temporal-stable dither
                    float2 screenPos = input.positionCS.xy;
                    float dither = frac(52.9829189 * frac(dot(screenPos, float2(0.06711056, 0.00583715))));

                    clip(fade - dither);
                }

                // Simple N dot L lighting
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = _BaseColor.rgb * (mainLight.color * NdotL + unity_AmbientSky.rgb);

                // Emission
                half3 finalColor = diffuse + _EmissionColor.rgb;

                // Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }

        // Shadow caster pass (buildings cast shadows even when roof is faded)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _MiniMapPlayerPosition;

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _Smoothness;
                float _FadeStartDist;
                float _FadeEndDist;
            CBUFFER_END

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Apply shadow bias
                float3 posWS = posInputs.positionWS;
                float3 normalWS = output.normalWS;
                posWS = ApplyShadowBias(posWS, normalWS, _LightDirection);
                output.positionCS = TransformWorldToHClip(posWS);

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                // Also clip roof shadows near player for consistency
                float3 normalWS = normalize(input.normalWS);
                if (normalWS.y > 0.7)
                {
                    float dist = distance(input.positionWS.xz, _MiniMapPlayerPosition.xz);
                    float fade = saturate((dist - _FadeEndDist) / max(_FadeStartDist - _FadeEndDist, 0.01));
                    float2 screenPos = input.positionCS.xy;
                    float dither = frac(52.9829189 * frac(dot(screenPos, float2(0.06711056, 0.00583715))));
                    clip(fade - dither);
                }
                return 0;
            }

            ENDHLSL
        }
    }
}
