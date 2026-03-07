Shader "MiniMapGame/Water"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.08, 0.18, 0.35, 0.75)
        _ScrollSpeed ("Scroll Speed", Float) = 0.3
        _WaveScale ("Wave Scale", Float) = 40
        _WaveIntensity ("Wave Intensity", Range(0, 0.3)) = 0.08
        _SpecularPower ("Specular Power", Float) = 32
        _SpecularIntensity ("Specular Intensity", Range(0, 1)) = 0.4
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
                float _ScrollSpeed;
                float _WaveScale;
                half _WaveIntensity;
                float _SpecularPower;
                half _SpecularIntensity;
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
                float2 worldUV = input.positionWS.xz / _WaveScale;
                float time = _Time.y * _ScrollSpeed;

                // Two scrolling wave layers
                float wave1 = sin((worldUV.x + worldUV.y) * 6.28 + time * 2.0) * 0.5 + 0.5;
                float wave2 = sin((worldUV.x - worldUV.y * 0.7) * 4.0 + time * 1.3) * 0.5 + 0.5;
                float waveMix = (wave1 + wave2) * 0.5;

                // Color variation from waves
                half3 color = _BaseColor.rgb;
                color += waveMix * _WaveIntensity;

                // Simple lighting
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 lighting = mainLight.color * (NdotL * 0.5 + 0.5) + unity_AmbientSky.rgb;
                color *= lighting;

                // Specular highlight
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(input.normalWS, halfDir)), _SpecularPower);
                color += mainLight.color * spec * _SpecularIntensity;

                color = MixFog(color, input.fogFactor);

                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
