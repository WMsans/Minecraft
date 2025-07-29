using UnityEngine;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

namespace sapra.InfiniteLands
{
    [CustomNode("Position", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/position")]
    public class PositionNode : InfiniteLandsNode
    {
        [Output] public HeightData PositionX;
        [Output] public HeightData PositionZ;
        [Input, Hide] public GridData Grid;

        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Grid, nameof(Grid));
        }

        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpaceX = heightBranch.GetAllocationSpaceVectorized(this, nameof(PositionX), out var targetMap);
            var targetSpaceZ = heightBranch.GetAllocationSpace(this, nameof(PositionZ));

            NativeArray<float3> targetGrid = new NativeArray<float3>(targetSpaceX.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            JobHandle collectPoints = CollectSectionOfGrid.ScheduleParallel(Grid.meshGrid, Grid.Resolution, targetGrid, targetSpaceX.Resolution, Grid.jobHandle);
            NativeArray<float3x4> vectorized = targetGrid.Reinterpret<float3x4>(sizeof(float) * 3);


            JobHandle jobX = PositionJob.ScheduleParallel(vectorized,
                        targetMap, branch.terrain.Position, true, targetSpaceX,
                        collectPoints);

            JobHandle jobZ = PositionJob.ScheduleParallel(vectorized,
                        targetMap, branch.terrain.Position, false, targetSpaceZ,
                        collectPoints);

            JobHandle completed = JobHandle.CombineDependencies(jobX, jobZ);

            var position = branch.terrain.Position;
            PositionX = new HeightData(completed, targetSpaceX, new Vector2(position.x - Grid.MeshScale, position.x + Grid.MeshScale));
            PositionZ = new HeightData(completed, targetSpaceZ, new Vector2(position.z - Grid.MeshScale, position.z + Grid.MeshScale));
            targetGrid.Dispose(completed);
            return true;
        }
        
        protected override void CacheOutputValues()
        {
            CacheOutputValue(PositionX, nameof(PositionX));
            CacheOutputValue(PositionZ, nameof(PositionZ));
        }
    }
}