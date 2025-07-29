using Unity.Jobs;
using UnityEngine;

using System;
using Unity.Mathematics;
using Unity.Collections;
using static sapra.InfiniteLands.Noise;

namespace sapra.InfiniteLands
{
    [CustomNode("Perlin Noise", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/perlin_noise.html")]
    public class PerlinNoiseNode : InfiniteLandsNode
    {
        public enum PerlinType{PerlinValue, Perlin}
        public PerlinType NoiseType = PerlinType.Perlin;
        public Vector2 MinMaxHeight = new Vector2(0, 1);
        [Min(0.001f)] public float TileSize = 100;
        [Min(1)] public int Octaves = 1;
        public Vector3 Rotation;

        [ShowIf(nameof(octavesEnabled))][Range(1,10)]public int Lacunarity = 2;
        [ShowIf(nameof(octavesEnabled))][Range(0f, 1f)] public float Persistence = .5f;
        public bool RidgeMode;
        private bool octavesEnabled => Octaves > 1;
        
        [Output] public HeightData Output;
        [Input, Hide] public GridData Grid;

        NoiseSettings getsettings()
        {
            return new NoiseSettings()
            {
                scale = TileSize,
                octaves = Octaves,
                minmaxValue = MinMaxHeight,
                lacunarity = Lacunarity,
                persistence = Persistence,
                rotation = Rotation,
                ridgeMode = RidgeMode
            };
        }
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Grid, nameof(Grid));
        }

        protected override bool Process(BranchData branch)
        {
            NoiseSettings noiseSettings = getsettings();
            int indexOffset = GetRandomIndex();
            float maxOctaves = Mathf.Pow(int.MaxValue, 1f / Lacunarity);
            noiseSettings.octaves = Mathf.Max(1, Mathf.Min(noiseSettings.octaves, Mathf.FloorToInt(maxOctaves)));

            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpaceVectorized(this, nameof(Output), out var targetMap);

            NativeArray<float3> targetGrid = new NativeArray<float3>(targetSpace.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            JobHandle collectPoints = CollectSectionOfGrid.ScheduleParallel(Grid.meshGrid, Grid.Resolution, targetGrid, targetSpace.Resolution, Grid.jobHandle);
            NativeArray<float3x4> vectorized = targetGrid.Reinterpret<float3x4>(sizeof(float) * 3);

            JobHandle jobHandle;
            switch (NoiseType)
            {
                case PerlinType.PerlinValue:
                    jobHandle = NoiseJob<Perlin2D<Value>>.ScheduleParallel(vectorized,
                        targetMap, noiseSettings, branch.terrain.Position, targetSpace,
                        branch.meshSettings.Seed + indexOffset,
                        collectPoints);
                    break;
                case PerlinType.Perlin:
                    jobHandle = NoiseJob<Perlin2D<Perlin>>.ScheduleParallel(vectorized,
                        targetMap, noiseSettings, branch.terrain.Position,
                        targetSpace,
                        branch.meshSettings.Seed + indexOffset,
                        collectPoints);
                    break;
                default:
                    jobHandle = NoiseJob<Perlin2D<Perlin>>.ScheduleParallel(vectorized,
                        targetMap, noiseSettings, branch.terrain.Position, targetSpace,
                        branch.meshSettings.Seed + indexOffset,
                        collectPoints);
                    break;
            }
            Output = new HeightData(jobHandle, targetSpace, MinMaxHeight);
            targetGrid.Dispose(jobHandle);
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}