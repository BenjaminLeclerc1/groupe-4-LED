Shader "LED/LEDWallGrid"
{
    Properties
    {
        [MainTexture] _BaseMap("LED Texture", 2D) = "black" {}
        _GridSize("Grid Size", Float) = 128
        _Gap("Gap", Range(0, 0.45)) = 0.14
        _BackgroundColor("Background", Color) = (0.02, 0.02, 0.02, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "LEDWallGrid"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _GridSize;
                float _Gap;
                float4 _BackgroundColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 gridUv = input.uv * _GridSize;
                float2 cell = frac(gridUv);
                float2 cellId = floor(gridUv);
                float2 sampleUv = (cellId + 0.5) / _GridSize;

                half4 ledColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, sampleUv);

                float2 centered = cell - 0.5;
                float radius = max(0.05, 0.5 - _Gap * 0.5);
                float dist = length(centered);
                float mask = 1.0 - smoothstep(radius - 0.02, radius, dist);

                half3 color = lerp(_BackgroundColor.rgb, ledColor.rgb, mask);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
