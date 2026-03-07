Shader "MiniMapGame/GridGround"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.035, 0.047, 0.07, 1)
        _GridColor ("Grid Color", Color) = (0.06, 0.08, 0.12, 1)
        _GridSize ("Grid Size", Float) = 20
        _GridOpacity ("Grid Opacity", Range(0, 1)) = 0.15
        _MidColor ("Mid Elevation Color", Color) = (0.08, 0.06, 0.04, 1)
        _HighColor ("High Elevation Color", Color) = (0.10, 0.09, 0.08, 1)
        _ElevMidThreshold ("Mid Elevation Threshold", Float) = 3.0
        _ElevHighThreshold ("High Elevation Threshold", Float) = 8.0
        _ElevBlendRange ("Elevation Blend Range", Float) = 2.0
        _SlopeColor ("Slope Tint", Color) = (0.06, 0.05, 0.04, 1)
        _SlopeThreshold ("Slope Threshold", Range(0, 1)) = 0.3
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

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GridColor;
                float _GridSize;
                half _GridOpacity;
                half4 _MidColor;
                half4 _HighColor;
                float _ElevMidThreshold;
                float _ElevHighThreshold;
                float _ElevBlendRange;
                half4 _SlopeColor;
                half _SlopeThreshold;
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
                // Grid calculation using world-space XZ
                float2 worldUV = input.positionWS.xz / _GridSize;
                float2 grid = abs(frac(worldUV - 0.5) - 0.5) / fwidth(worldUV);
                float line = min(grid.x, grid.y);
                float gridMask = 1.0 - saturate(line);

                // Elevation-based color blending
                float elev = input.positionWS.y;
                float midBlend = saturate((elev - _ElevMidThreshold) / max(_ElevBlendRange, 0.01));
                float highBlend = saturate((elev - _ElevHighThreshold) / max(_ElevBlendRange, 0.01));
                half3 terrainColor = lerp(_BaseColor.rgb, _MidColor.rgb, midBlend);
                terrainColor = lerp(terrainColor, _HighColor.rgb, highBlend);

                // Slope-based tinting (steep = rocky)
                float slopeFactor = 1.0 - saturate(dot(normalize(input.normalWS), float3(0, 1, 0)));
                float slopeMask = saturate((slopeFactor - _SlopeThreshold) / (1.0 - _SlopeThreshold));
                terrainColor = lerp(terrainColor, _SlopeColor.rgb, slopeMask * 0.6);

                // Blend terrain and grid colors
                half3 color = lerp(terrainColor, _GridColor.rgb, gridMask * _GridOpacity);

                // Simple Lambert lighting
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 lighting = mainLight.color * NdotL + unity_AmbientSky.rgb;
                color *= lighting;

                // Apply fog
                color = MixFog(color, input.fogFactor);

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass for receiving shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
