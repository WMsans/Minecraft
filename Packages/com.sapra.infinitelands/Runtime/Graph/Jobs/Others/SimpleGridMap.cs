using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct SimpleGridMap : IJobFor
    {
        [WriteOnly] NativeArray<float3> targetPoints;

        int Resolution;
        float MeshScale;
        public void Execute(int index)
        {              
            int x = index % (Resolution+1);
            int y = index / (Resolution+1);

            float3 position = 0;
            position.z = (float)y / Resolution - 0.5f;
            position.x = (float)x / Resolution - 0.5f;
            position *= MeshScale;

            targetPoints[index] = position;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3> targetPoints, 
            int Resolution, float MeshScale, JobHandle dependency) => new SimpleGridMap()
        {
            targetPoints = targetPoints,
            Resolution = Resolution,
            MeshScale = MeshScale,
        }.ScheduleParallel(targetPoints.Length, Resolution, dependency);
    }
}