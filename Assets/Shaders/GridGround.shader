Shader "MiniMapGame/GridGround"
{
    Properties
    {
        [Header(Base Colors)]
        _BaseColor ("Base Color (Low Elev)", Color) = (0.035, 0.047, 0.07, 1)
        _MidColor ("Mid Elevation Color", Color) = (0.08, 0.06, 0.04, 1)
        _HighColor ("High Elevation Color", Color) = (0.10, 0.09, 0.08, 1)

        [Header(Semantic Tints)]
        _MoistureTint ("Moisture Tint", Color) = (0.02, 0.04, 0.06, 1)
        _RoadTint ("Road Influence Tint", Color) = (0.06, 0.055, 0.05, 1)
        _BuildingTint ("Building Influence Tint", Color) = (0.055, 0.05, 0.045, 1)
        _SlopeColor ("Slope Tint", Color) = (0.06, 0.05, 0.04, 1)

        [Header(Grid)]
        _GridColor ("Grid Color", Color) = (0.06, 0.08, 0.12, 1)
        _GridSize ("Grid Size", Float) = 20
        _GridOpacity ("Grid Opacity", Range(0, 1)) = 0.15

        [Header(Contour)]
        _ContourInterval ("Contour Interval (world units)", Float) = 2.0
        _ContourLineWidth ("Contour Line Width", Range(0.005, 0.1)) = 0.03
        _ContourColor ("Contour Line Color", Color) = (0.02, 0.025, 0.035, 1)

        [Header(Hillshade)]
        _HillshadeLightDir ("Hillshade Light Dir (XY)", Vector) = (-0.7, 0.7, 0, 0)

        [Header(Compositing Strengths — set by MapManager)]
        _HillshadeStrength ("Hillshade Strength", Range(0, 1)) = 0.55
        _ContourStrength ("Contour Strength", Range(0, 1)) = 0.25
        _MoistureStrength ("Moisture Strength", Range(0, 1)) = 0.45
        _RoadInfluenceStrength ("Road Influence Strength", Range(0, 1)) = 0.3
        _BuildingInfluenceStrength ("Building Influence Strength", Range(0, 1)) = 0.25
        _NearStart ("Near Blend Start", Float) = 20
        _NearEnd ("Near Blend End", Float) = 80

        [Header(Mask Textures — bound at runtime)]
        _GroundHeightSlopeTex ("HeightSlope Mask", 2D) = "gray" {}
        _GroundSemanticTex ("Semantic Mask", 2D) = "black" {}
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

            TEXTURE2D(_GroundHeightSlopeTex);
            SAMPLER(sampler_GroundHeightSlopeTex);
            TEXTURE2D(_GroundSemanticTex);
            SAMPLER(sampler_GroundSemanticTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _MidColor;
                half4 _HighColor;
                half4 _MoistureTint;
                half4 _RoadTint;
                half4 _BuildingTint;
                half4 _SlopeColor;
                half4 _GridColor;
                float _GridSize;
                half _GridOpacity;
                float _ContourInterval;
                float _ContourLineWidth;
                half4 _ContourColor;
                float4 _HillshadeLightDir;
                half _HillshadeStrength;
                half _ContourStrength;
                half _MoistureStrength;
                half _RoadInfluenceStrength;
                half _BuildingInfluenceStrength;
                float _NearStart;
                float _NearEnd;
                float4 _GroundHeightSlopeTex_TexelSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // ── Sample baked masks ──
                half4 hs = SAMPLE_TEXTURE2D(_GroundHeightSlopeTex, sampler_GroundHeightSlopeTex, uv);
                half4 sem = SAMPLE_TEXTURE2D(_GroundSemanticTex, sampler_GroundSemanticTex, uv);

                float elevNorm   = hs.r; // [0,1] normalized elevation
                float slopeNorm  = hs.g; // [0,1] normalized slope
                float curvature  = hs.b; // 0.5 = flat, <0.5 = concave, >0.5 = convex
                float jitter     = hs.a; // contour jitter

                float moisture      = sem.r;
                float roadInf       = sem.g;
                float buildingInf   = sem.b;
                float intersectionB = sem.a;

                // ── 1. Elevation gradient ──
                half3 color = lerp(_BaseColor.rgb, _MidColor.rgb, saturate(elevNorm * 2.0));
                color = lerp(color, _HighColor.rgb, saturate(elevNorm * 2.0 - 1.0));

                // ── 2. Slope tinting ──
                float slopeMask = saturate((slopeNorm - 0.3) / 0.7);
                color = lerp(color, _SlopeColor.rgb, slopeMask * 0.6);

                // ── 3. Hillshade (4-tap normal reconstruction from elevation mask) ──
                float2 texel = _GroundHeightSlopeTex_TexelSize.xy;
                float eN = SAMPLE_TEXTURE2D(_GroundHeightSlopeTex, sampler_GroundHeightSlopeTex, uv + float2(0, texel.y)).r;
                float eS = SAMPLE_TEXTURE2D(_GroundHeightSlopeTex, sampler_GroundHeightSlopeTex, uv - float2(0, texel.y)).r;
                float eE = SAMPLE_TEXTURE2D(_GroundHeightSlopeTex, sampler_GroundHeightSlopeTex, uv + float2(texel.x, 0)).r;
                float eW = SAMPLE_TEXTURE2D(_GroundHeightSlopeTex, sampler_GroundHeightSlopeTex, uv - float2(texel.x, 0)).r;
                // Gradient in UV space, scaled to approximate world-space derivatives
                float dEdx = (eE - eW) * 0.5;
                float dEdy = (eN - eS) * 0.5;
                float2 lightDir2D = normalize(_HillshadeLightDir.xy);
                float hillshade = saturate(dot(float2(dEdx, dEdy), -lightDir2D) * 4.0 + 0.5);
                color *= lerp(1.0, hillshade, _HillshadeStrength);

                // ── 4. Curvature enhancement ──
                // Concave areas (valleys) slightly darker, convex (ridges) slightly brighter
                float curvatureFactor = lerp(0.92, 1.08, curvature);
                color *= curvatureFactor;

                // ── 5. Contour lines ──
                // Use real world-space elevation for contour precision
                float worldElev = input.positionWS.y;
                float contourPhase = frac(worldElev / _ContourInterval + jitter * 0.05);
                float contourLine = 1.0 - smoothstep(_ContourLineWidth, _ContourLineWidth * 2.0,
                    abs(contourPhase - 0.5));
                color = lerp(color, _ContourColor.rgb, contourLine * _ContourStrength);

                // ── 6. Moisture / shore influence ──
                color = lerp(color, _MoistureTint.rgb, moisture * _MoistureStrength);

                // ── 7. Road influence ──
                half3 roadColor = lerp(color, _RoadTint.rgb, 0.5);
                color = lerp(color, roadColor, roadInf * _RoadInfluenceStrength);

                // ── 8. Building influence ──
                half3 bldgColor = lerp(color, _BuildingTint.rgb, 0.4);
                color = lerp(color, bldgColor, buildingInf * _BuildingInfluenceStrength);

                // ── 9. Intersection boost (brighten intersections slightly) ──
                color += intersectionB * roadInf * 0.03;

                // ── 10. Near/far distance blend ──
                float camDist = distance(input.positionWS, _WorldSpaceCameraPos);
                float distanceFade = saturate((camDist - _NearStart) / max(_NearEnd - _NearStart, 1.0));

                // ── 11. Grid overlay (dual-scale, fades with distance) ──
                float2 worldUV = input.positionWS.xz / _GridSize;
                float2 grid = abs(frac(worldUV - 0.5) - 0.5) / fwidth(worldUV);
                float fineLineDistance = min(grid.x, grid.y);

                float2 worldUV2 = input.positionWS.xz / (_GridSize * 5.0);
                float2 grid2 = abs(frac(worldUV2 - 0.5) - 0.5) / fwidth(worldUV2);
                float coarseLineDistance = min(grid2.x, grid2.y);

                float gridMask = max(
                    1.0 - saturate(fineLineDistance),
                    (1.0 - saturate(coarseLineDistance)) * 0.5);
                // Fine grid fades at distance, coarse persists
                float gridFade = lerp(1.0, 0.3, distanceFade);
                color = lerp(color, _GridColor.rgb, gridMask * _GridOpacity * gridFade);

                // ── 12. Lighting ──
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 lighting = mainLight.color * NdotL + unity_AmbientSky.rgb;
                color *= lighting;

                // ── 13. Fog ──
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
