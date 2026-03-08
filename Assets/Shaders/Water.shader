Shader "MiniMapGame/Water"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.08, 0.18, 0.35, 0.75)
        _ShallowColor ("Shallow Color", Color) = (0.15, 0.35, 0.45, 0.45)
        _DeepColor ("Deep Color", Color) = (0.02, 0.08, 0.22, 0.90)
        _DepthFalloff ("Depth Falloff", Range(0.1, 5)) = 1.5
        _ScrollSpeed ("Scroll Speed", Float) = 0.3
        _WaveScale ("Wave Scale", Float) = 40
        _WaveIntensity ("Wave Intensity", Range(0, 0.3)) = 0.08
        _Roughness ("Surface Roughness", Range(0, 1)) = 0.3
        _SpecularPower ("Specular Power", Float) = 32
        _SpecularIntensity ("Specular Intensity", Range(0, 1)) = 0.4
        _FoamColor ("Foam Color", Color) = (0.8, 0.9, 1.0, 0.6)
        _FoamThreshold ("Foam Threshold", Range(0, 0.5)) = 0.15
        _FoamScale ("Foam Scale", Float) = 60
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShallowColor;
                half4 _DeepColor;
                float _DepthFalloff;
                float _ScrollSpeed;
                float _WaveScale;
                half _WaveIntensity;
                half _Roughness;
                float _SpecularPower;
                half _SpecularIntensity;
                half4 _FoamColor;
                half _FoamThreshold;
                float _FoamScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float2 depthUV : TEXCOORD4;
                float4 vertColor : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.depthUV = input.uv2;
                output.vertColor = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            // Simple hash for foam noise
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Depth from UV2 channel (0 = surface/shallow, 1 = deep)
                float depth = input.depthUV.x;

                // Depth-based color blending
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb,
                    pow(saturate(depth), _DepthFalloff));

                // Fallback to BaseColor if no depth data
                waterColor = lerp(_BaseColor.rgb, waterColor, step(0.001, depth + 0.001));

                float2 worldUV = input.positionWS.xz / _WaveScale;
                float time = _Time.y * _ScrollSpeed;

                // Roughness: from vertex color R channel, fallback to material property
                float roughness = input.vertColor.r > 0.01 ? input.vertColor.r : _Roughness;

                // Two scrolling wave layers
                float wave1 = sin((worldUV.x + worldUV.y) * 6.28 + time * 2.0) * 0.5 + 0.5;
                float wave2 = sin((worldUV.x - worldUV.y * 0.7) * 4.0 + time * 1.3) * 0.5 + 0.5;
                float waveMix = (wave1 + wave2) * 0.5;

                // Wave intensity modulated by roughness
                waterColor += waveMix * _WaveIntensity * (0.5 + roughness * 0.5);

                // Shore foam effect (appears in shallow water)
                if (depth < _FoamThreshold && depth > 0.001)
                {
                    float2 foamUV = input.positionWS.xz / _FoamScale;
                    float foamTime = time * 0.15;

                    // Animated foam noise
                    float foam1 = hash21(floor(foamUV * 8.0) + floor(foamTime));
                    float foam2 = hash21(floor(foamUV * 12.0 + 0.5) + floor(foamTime * 1.3 + 0.7));
                    float foamNoise = (foam1 + foam2) * 0.5;

                    float foamMask = smoothstep(_FoamThreshold, 0.0, depth) * foamNoise;
                    waterColor = lerp(waterColor, _FoamColor.rgb, foamMask * _FoamColor.a);
                }

                // Lighting
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 lighting = mainLight.color * (NdotL * 0.5 + 0.5) + unity_AmbientSky.rgb;
                waterColor *= lighting;

                // Specular highlight (reduced by roughness)
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(input.normalWS, halfDir)), _SpecularPower);
                waterColor += mainLight.color * spec * _SpecularIntensity * (1.0 - roughness * 0.5);

                waterColor = MixFog(waterColor, input.fogFactor);

                // Alpha: deeper = more opaque
                float alpha = lerp(_ShallowColor.a, _DeepColor.a, saturate(depth));
                // Fallback alpha when no depth data
                alpha = lerp(_BaseColor.a, alpha, step(0.001, depth + 0.001));

                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
