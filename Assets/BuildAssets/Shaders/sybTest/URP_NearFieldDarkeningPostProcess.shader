Shader "URP/NearFieldDarkeningPostProcess"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _DarkeningStartDistance("Darkening Start Distance", Range(0, 20)) = 10
        _DarkeningEndDistance("Darkening End Distance", Range(0, 5)) = 1
        _DarkeningIntensity("Darkening Intensity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float _DarkeningStartDistance;
        float _DarkeningEndDistance;
        float _DarkeningIntensity;
        CBUFFER_END

        TEXTURE2D_X(_MainTex);
        SAMPLER(sampler_MainTex);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 screenPos : TEXCOORD1;
        };

        TEXTURE2D_X(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);
        ENDHLSL

        Pass
        {
            Name "NearFieldDarkeningPostProcess"
            Tags { "LightMode" = "UniversalForward" }

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv = IN.uv;
            OUT.screenPos = ComputeScreenPos(OUT.positionCS);
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            // Get screen UV for sampling (with perspective correction)
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

            // Sample main texture (scene color)
            half4 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, screenUV);

            // Sample depth texture
            float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;

            // Check if depth is valid
            if (depth >= 1.0) // Skybox or far plane
            {
                return color;
            }

            // Convert depth to world space position
            float3 worldPos = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
            float3 cameraPos = _WorldSpaceCameraPos;
            
            // Calculate distance to camera (along camera forward direction)
            float3 viewDirection = normalize(worldPos - cameraPos);
            float distanceToCamera = length(worldPos - cameraPos);
            
            // Calculate darkening factor
            // 当距离小于_DarkeningEndDistance时，完全暗化（因子为0）
            // 当距离大于_DarkeningStartDistance时，不暗化（因子为1）
            // 在两个距离之间，进行平滑过渡
            float darkeningFactor = smoothstep(_DarkeningEndDistance, _DarkeningStartDistance, distanceToCamera);
            
            // Apply darkening intensity
            float finalDarkening = 1.0 - (1.0 - darkeningFactor) * _DarkeningIntensity;

            // Apply darkening
            color.rgb *= finalDarkening;

            return color;
        }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}