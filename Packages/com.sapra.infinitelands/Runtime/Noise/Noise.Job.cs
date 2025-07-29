using System.Runtime.CompilerServices;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    internal static partial class Noise
    {
        public interface INoise
        {
            float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, float frequency, float smoothness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 GetFractalNoise<N>(float4x3 position, int seed, NoiseSettings settings)
            where N : struct, INoise
        {
            var hash = SmallXXHash4.Seed(seed);
            int frequency = 1;
            float amplitude = 1f, amplitudeSum = 0f;
            float4 sum = 0;

            position = settings.Matrix.TransformVectors(position);

            for (int o = 0; o < settings.octaves; o++)
            {
                float4 noiseValue = default(N).GetNoise4(position, hash + o, frequency, settings.SmoothVoronoi);
                if(settings.ridgeMode)
                    noiseValue = abs(noiseValue*2f-1f);
                sum += noiseValue * amplitude;

                frequency = frequency*settings.lacunarity;
                amplitude = amplitude*settings.persistence;
                if (amplitude < 0.000001f) break;

                amplitudeSum += pow(settings.persistence, o);
            }
            float4 value = lerp(settings.minmaxValue.x, settings.minmaxValue.y, sum / amplitudeSum);
            return value;
        }
    }
}