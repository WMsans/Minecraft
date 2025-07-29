using UnityEngine;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

namespace sapra.InfiniteLands
{
    [CustomNode("Shape", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/shape",
        synonims = new string[]{"Simple Form", "Cube",
            "HalfSphere",
            "Cone",
            "Bump",
            "Pyramid",
            "Cylinder",
            "Torus"})]
    public class ShapesNode : InfiniteLandsNode
    {
        public enum ShapeType
        {
            Cube,
            HalfSphere,
            Cone,
            Bump,
            Pyramid,
            Cylinder,
            Torus
        }
        public ShapeType Shape;


        public Vector2 MinMaxHeight = new Vector2(0, 1);
        public Vector2 Origin = Vector2.zero;
        [ShowIf(nameof(Rotable))] public float YRotation = 0;
        [Min(1)] public float Size = 200;
        public bool Rotable => Shape == ShapeType.Cube || Shape == ShapeType.Pyramid;

        [Output] public HeightData Output;
        [Input, Hide] public GridData Grid;
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Grid, nameof(Grid));
        }

        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpaceVectorized(this, nameof(Output), out var targetMap);
            
            NativeArray<float3> targetGrid = new NativeArray<float3>(targetSpace.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            JobHandle collectPoints = CollectSectionOfGrid.ScheduleParallel(Grid.meshGrid, Grid.Resolution, targetGrid, targetSpace.Resolution, Grid.jobHandle);
            NativeArray<float3x4> vectorized = targetGrid.Reinterpret<float3x4>(sizeof(float) * 3);

            Vector3 origin = new Vector3(Origin.x, 0, Origin.y);
            JobHandle jobHandle;
            switch (Shape)
            {
                case ShapeType.HalfSphere:
                    jobHandle = ShapeJob<SHalfSphere>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position - origin,
                    YRotation, Size, targetSpace,
                    collectPoints);
                    break;
                case ShapeType.Cone:
                    jobHandle = ShapeJob<SCone>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position - origin,
                    YRotation, Size, targetSpace,
                    collectPoints);
                    break;
                case ShapeType.Pyramid:
                    jobHandle = ShapeJob<SPyramid>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position - origin,
                    YRotation, Size, targetSpace,
                    collectPoints);
                    break;
                case ShapeType.Bump:
                    jobHandle = ShapeJob<SBump>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position - origin,
                    YRotation, Size, targetSpace,
                    collectPoints);
                    break;
                case ShapeType.Cylinder:
                    jobHandle = ShapeJob<SCylinder>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position - origin,
                    YRotation, Size, targetSpace,
                    collectPoints);
                    break;
                case ShapeType.Torus:
                    jobHandle = ShapeJob<SHalfTorus>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position - origin,
                    YRotation, Size, targetSpace,
                    collectPoints);
                    break;
                default:
                    jobHandle = ShapeJob<SSquare>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position - origin,
                    YRotation, Size, targetSpace,
                    collectPoints);
                    break;
            }
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