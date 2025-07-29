using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct RepeatedSelectorJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        float StartingOffset;
        float Size; 
        float EmptySize;
        float HeightBlendFactor;
        IndexAndResolution target;
        IndexAndResolution current;

        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            float verticalHeight = heightMap[current.StartIndex + index];

            var totalSpace = Size+EmptySize;
            var normalizedBlending = HeightBlendFactor/totalSpace;
            var normalizedEdge = Size/totalSpace;
            var mapped = frac((verticalHeight-StartingOffset)/totalSpace);
            float masked = 0;
            if (HeightBlendFactor > 0)
            {
                masked = smoothstep(1-normalizedBlending, 1, mapped) +
                    (1-smoothstep(normalizedEdge, normalizedEdge+normalizedBlending, mapped));
            }

            masked += 1-step(normalizedEdge, mapped);
            heightMap[target.StartIndex + i] = masked;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, IndexAndResolution current, IndexAndResolution target,
            float StartingOffset, float Size, float EmptySize, float HeightBlendFactor,
            JobHandle dependency) => new RepeatedSelectorJob()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            StartingOffset = StartingOffset,
            Size = Size,
            EmptySize = EmptySize,
            HeightBlendFactor = HeightBlendFactor,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}