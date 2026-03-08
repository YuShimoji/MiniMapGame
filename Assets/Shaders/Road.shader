Shader "MiniMapGame/Road"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.22, 0.31, 0.41, 1)
        _CasingColor ("Casing Color", Color) = (0.17, 0.24, 0.35, 1)
        _MarkingColor ("Marking Color", Color) = (0.85, 0.85, 0.75, 1)
        _CurbColor ("Curb Color", Color) = (0.3, 0.3, 0.3, 1)
        _CurbRatio ("Curb Ratio", Float) = 0.071
        _ShoulderRatio ("Shoulder Ratio", Float) = 0.107
        _LaneCount ("Lane Count", Int) = 4
        _MarkingWidthRatio ("Marking Width Ratio", Float) = 0.014
        _HasCenterLine ("Has Center Line", Float) = 1
        _CenterLineSolid ("Center Line Solid", Float) = 1
        _HasLaneDividers ("Has Lane Dividers", Float) = 1
        _HasEdgeLines ("Has Edge Lines", Float) = 1
        _DashLength ("Dash Length", Float) = 2.0
        _DashGap ("Dash Gap", Float) = 1.5
        _Roughness ("Roughness", Range(0,1)) = 0.2
        _Wear ("Wear", Range(0,1)) = 0.1
        _CrackDensity ("Crack Density", Range(0,1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+1"
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
                half4 _CasingColor;
                half4 _MarkingColor;
                half4 _CurbColor;
                float _CurbRatio;
                float _ShoulderRatio;
                int _LaneCount;
                float _MarkingWidthRatio;
                float _HasCenterLine;
                float _CenterLineSolid;
                float _HasLaneDividers;
                float _HasEdgeLines;
                float _DashLength;
                float _DashGap;
                half _Roughness;
                half _Wear;
                half _CrackDensity;
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
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            // Simple procedural hash
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Simple value noise
            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // UV coordinates: u = across width (0-1), v = along length
                float u = input.uv.x;
                float v = input.uv.y;

                // Mirror for symmetry: 0=center, 1=edge
                float uMirror = abs(u - 0.5) * 2.0;

                // Screen-space derivatives for anti-aliasing
                float du = fwidth(uMirror);

                // Zone boundaries
                float curbStart = 1.0 - _CurbRatio;
                float shoulderStart = curbStart - _ShoulderRatio;

                // Initialize color
                half3 color = _BaseColor.rgb;

                // Zone detection with smoothstep transitions
                float curbMask = smoothstep(curbStart - du, curbStart + du, uMirror);
                float shoulderMask = smoothstep(shoulderStart - du, shoulderStart + du, uMirror);

                // Apply zone colors
                color = lerp(color, _CasingColor.rgb, shoulderMask * (1.0 - curbMask));
                color = lerp(color, _CurbColor.rgb, curbMask);

                // Lane markings (only in lane area)
                if (uMirror < shoulderStart)
                {
                    // Normalize to lane area: 0=center, 1=shoulder edge
                    float laneU = uMirror / max(shoulderStart, 0.001);

                    // Dash pattern
                    float dashCycle = _DashLength + _DashGap;
                    float dashPhase = fmod(v, dashCycle);
                    float inDash = step(dashPhase, _DashLength);

                    // Half marking width in normalized space
                    float halfMarkingWidth = (_MarkingWidthRatio * 0.5) / max(shoulderStart, 0.001);
                    float markingEdge = halfMarkingWidth;

                    // Center line
                    if (_HasCenterLine > 0.5)
                    {
                        float centerDist = abs(laneU);
                        float centerLineVisible = (_CenterLineSolid > 0.5) ? 1.0 : inDash;
                        float centerMask = smoothstep(markingEdge + du, markingEdge - du, centerDist);
                        color = lerp(color, _MarkingColor.rgb, centerMask * centerLineVisible);
                    }

                    // Lane dividers
                    if (_HasLaneDividers > 0.5 && _LaneCount > 2)
                    {
                        int halfLanes = max(_LaneCount / 2, 1);

                        // Limit iterations to avoid shader complexity
                        for (int i = 1; i < min(halfLanes, 4); i++)
                        {
                            float boundary = float(i) / float(halfLanes);
                            float dividerDist = abs(laneU - boundary);
                            float dividerMask = smoothstep(halfMarkingWidth + du, halfMarkingWidth - du, dividerDist);
                            color = lerp(color, _MarkingColor.rgb, dividerMask * inDash);
                        }
                    }

                    // Edge lines (solid)
                    if (_HasEdgeLines > 0.5)
                    {
                        float edgeDist = abs(uMirror - shoulderStart);
                        float edgeMask = smoothstep(_MarkingWidthRatio * 0.5 + du, _MarkingWidthRatio * 0.5 - du, edgeDist);
                        color = lerp(color, _MarkingColor.rgb, edgeMask);
                    }
                }

                // Surface wear and weathering
                float2 worldUV = input.positionWS.xz;

                // Wear patches
                float wearNoise = noise2D(worldUV * 3.0);
                color *= lerp(1.0, 0.85, wearNoise * _Wear);

                // Cracks
                float crackNoise = hash21(floor(worldUV * 8.0));
                float crackThreshold = 1.0 - _CrackDensity;
                if (crackNoise > crackThreshold)
                {
                    color *= 0.7;
                }

                // Roughness variation
                float roughnessNoise = noise2D(worldUV * 2.0);
                color *= lerp(1.0, 0.95, roughnessNoise * _Roughness);

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
