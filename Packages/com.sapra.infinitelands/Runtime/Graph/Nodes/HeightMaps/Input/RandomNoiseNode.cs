using UnityEngine;


using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
namespace sapra.InfiniteLands
{
    [CustomNode("Random Noise", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/random")]
    public class RandomNoiseNode : InfiniteLandsNode
    {
        public Vector2 MinMaxHeight = new Vector2(0, 1);
        [Min(0.01f)] public float Size = 100;

        [Output] public HeightData Output;
        [Input, Hide] public GridData Grid;
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Grid, nameof(Grid));
        }

        protected override bool Process(BranchData branch)
        {
            if (MinMaxHeight.x >= MinMaxHeight.y)
                MinMaxHeight.x = MinMaxHeight.y - 0.1f;

            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpaceVectorized(this, nameof(Output), out var targetMap);
            
            NativeArray<float3> targetGrid = new NativeArray<float3>(targetSpace.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            JobHandle collectPoints = CollectSectionOfGrid.ScheduleParallel(Grid.meshGrid, Grid.Resolution, targetGrid, targetSpace.Resolution, Grid.jobHandle);
            NativeArray<float3x4> vectorized = targetGrid.Reinterpret<float3x4>(sizeof(float) * 3);

            JobHandle jobHandle = RandomJob.ScheduleParallel(vectorized,
                        targetMap, new Vector2(MinMaxHeight.x + .01f, MinMaxHeight.y - 0.01f),
                        branch.terrain.Position,
                        Size, targetSpace,
                        collectPoints);
            targetGrid.Dispose(jobHandle);
            Output = new HeightData(jobHandle, targetSpace, new Vector2(MinMaxHeight.x, MinMaxHeight.y));
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}