Shader "Custom/RetroCamera"
{
    Properties
    {
        _PixelSize ("Pixel Size", Range(1, 12)) = 4
        _ColorLevels ("Color Levels", Range(2, 64)) = 24
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Retro Full Screen Pass"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelSize;
                float _ColorLevels;
                float _DitherStrength;
            CBUFFER_END

            // 4x4 Bayer dithering pattern.
            float Bayer4x4(int2 pixel)
            {
                int x = pixel.x % 4;
                int y = pixel.y % 4;

                if (y == 0)
                {
                    if (x == 0) return 0.0 / 16.0;
                    if (x == 1) return 8.0 / 16.0;
                    if (x == 2) return 2.0 / 16.0;
                    return 10.0 / 16.0;
                }

                if (y == 1)
                {
                    if (x == 0) return 12.0 / 16.0;
                    if (x == 1) return 4.0 / 16.0;
                    if (x == 2) return 14.0 / 16.0;
                    return 6.0 / 16.0;
                }

                if (y == 2)
                {
                    if (x == 0) return 3.0 / 16.0;
                    if (x == 1) return 11.0 / 16.0;
                    if (x == 2) return 1.0 / 16.0;
                    return 9.0 / 16.0;
                }

                if (x == 0) return 15.0 / 16.0;
                if (x == 1) return 7.0 / 16.0;
                if (x == 2) return 13.0 / 16.0;
                return 5.0 / 16.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // Dimensions of the source camera image.
                float2 resolution = _BlitTexture_TexelSize.zw;

                // 1 = original resolution.
                // Higher values = larger visible pixels.
                float pixelSize = max(_PixelSize, 1.0);

                float2 pixelatedResolution =
                    max(floor(resolution / pixelSize), 1.0);

                // Snap UVs to a lower-resolution grid.
                float2 pixelCoordinate =
                    floor(uv * pixelatedResolution);

                float2 pixelatedUV =
                    (pixelCoordinate + 0.5) /
                    pixelatedResolution;

                // Sample the camera image.
                float3 color =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        pixelatedUV
                    ).rgb;

                // Ordered dithering.
                float dither =
                    Bayer4x4(int2(pixelCoordinate));

                float levels =
                    max(_ColorLevels, 2.0);

                float ditherOffset =
                    (dither - 0.5) *
                    _DitherStrength /
                    (levels - 1.0);

                // Reduce the number of colors.
                color =
                    saturate(color + ditherOffset);

                color =
                    round(color * (levels - 1.0)) /
                    (levels - 1.0);

                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }

    Fallback Off
}