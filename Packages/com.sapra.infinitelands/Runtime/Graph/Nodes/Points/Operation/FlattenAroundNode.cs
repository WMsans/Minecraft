using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

namespace sapra.InfiniteLands
{
    [CustomNode("Flatten Around", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/operations/flattenaround")]
    public class FlattenAroundNode : InfiniteLandsNode
    {
        [Input] public PointInstance Points;
        [Input] public HeightData HeightMap;
        [Input, Hide] public GridData Grid;

        [Input, Disabled] public HeightData Texture;
        [Output] public HeightData Output;

        public bool isTextureAssigned => IsAssigned(nameof(Texture));
        public bool IfMaskNotAssigned => !isTextureAssigned;

        [Min(1)] public float Size = 250;
        public bool AppliesTerrainHeight = true;
        public bool AffectedByScale = false;
        
        [ShowIf(nameof(isTextureAssigned))]public bool AffectedByRotation = false;
        [ShowIf(nameof(IfMaskNotAssigned))][Min(0.1f)] public float FallofDistance = 1;
        

        private PointManager pointManager;
        private PointInstance previousPoints;
        private List<PointTransform> ValidPoints = new();
        private List<AwaitingHeight> GeneratedHeights = new();
        private BranchData textureSettings;
        private float FinalSize;

        protected override bool SetInputValues(BranchData branch)
        {
            bool points = TryGetInputData(branch, out previousPoints, nameof(Points));
            bool heightMap = TryGetInputData(branch, out HeightMap, nameof(HeightMap));
            bool grid = TryGetInputData(branch, out Grid, nameof(Grid));
            return points && heightMap && grid;
        }
        protected override bool Process(BranchData branch)
        {
            if (state.SubState == 0)
            {
                pointManager = branch.GetGlobalData<PointManager>();
                ValidPoints.Clear();

                if (isTextureAssigned)
                {
                    int resolution = Mathf.CeilToInt(branch.ScaleToResolutionRatio * Size);
                    FinalSize = resolution / branch.ScaleToResolutionRatio;
                    MeshSettings meshSettings = new MeshSettings()
                    {
                        Resolution = resolution,
                        MeshScale = FinalSize,
                        Seed = branch.meshSettings.Seed,
                    };

                    TerrainConfiguration terrain = new TerrainConfiguration(default, branch.terrain.TerrainNormal, Vector3.zero);
                    textureSettings = BranchData.NewChildBranch(meshSettings, terrain, branch, GetNodesInInput(nameof(Texture)));
                }

                state.IncreaseSubState();
            }
            if (state.SubState == 1)
            {
                if (isTextureAssigned && !TryGetInputData(textureSettings, out Texture, nameof(Texture))) return false;       
                state.IncreaseSubState();
            }


            if (state.SubState == 2)
            {
  
                if (!previousPoints.GetAllPoints(Grid.MeshScale, branch.terrain.Position, out var foundPoints)) return false;

                if (AppliesTerrainHeight)
                {
                    foreach (var point in foundPoints)
                    {
                        MeshSettings meshSettings = new MeshSettings()
                        {
                            Resolution = 3,
                            MeshScale = 50,
                            Seed = branch.meshSettings.Seed,
                        };
                        var height = pointManager.GetDataAtPoint(this, nameof(HeightMap), point.Position, meshSettings);
                        GeneratedHeights.Add(new AwaitingHeight()
                        {
                            awaitingHeight = height,
                            point = point,
                        });
                    }
                    state.IncreaseSubState();
                }
                else
                {
                    ValidPoints.AddRange(foundPoints);
                    state.SetSubState(4);
                }
            }

            if (state.SubState == 3)
            {
                var updater = new UpdatePointPosition(ValidPoints);
                if (AwaitableTools.IterateOverItems(GeneratedHeights, ref updater))
                    state.IncreaseSubState();
            }

            if (state.SubState == 4)
            {
                HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
                var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
                NativeArray<PointTransform> flattenedPoints = branch.GetData<ReturnableBranch>().GetData(ValidPoints);
                JobHandle combinedJob = JobHandle.CombineDependencies(HeightMap.jobHandle, Grid.jobHandle);

                JobHandle job;
                if (isTextureAssigned)
                {
                    HeightMapBranch textureBranch = textureSettings.GetData<HeightMapBranch>();
                    var textureMap = textureBranch.GetMap();
                    JobHandle combined = JobHandle.CombineDependencies(Texture.jobHandle, combinedJob);

                    job = FlattenAtPointsTextureJob.ScheduleParallel(map, textureMap, Grid.meshGrid, Grid.Resolution, flattenedPoints,
                        targetSpace, HeightMap.indexData, Texture.indexData,
                        ValidPoints.Count, FinalSize,
                        AffectedByScale, AffectedByRotation,
                        branch.terrain.Position, combined);
                }
                else
                {
                    job = FlattenAtPointsJob.ScheduleParallel(map, Grid.meshGrid, Grid.Resolution, flattenedPoints, HeightMap.indexData, targetSpace,
                        ValidPoints.Count, FallofDistance, Size, AffectedByScale,
                        branch.terrain.Position, combinedJob);
                }

                Output = new HeightData(job, targetSpace, HeightMap.minMaxValue);
                state.IncreaseSubState();
            }

            return state.SubState == 5;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
        private struct UpdatePointPosition : ICallMethod<AwaitingHeight>
        {
            List<PointTransform> ValidPoints;
            public UpdatePointPosition(List<PointTransform> pointTransforms)
            {
                this.ValidPoints = pointTransforms;
            }
            public bool Callback(AwaitingHeight value)
            {
                if (!value.awaitingHeight.ProcessData()) return false;

                var result = value.awaitingHeight.Result;
                var point = value.point;

                ValidPoints.Add(point.UpdatePosition(new Vector3(point.Position.x, result, point.Position.z)));
                return true;
            }
        }
        private struct AwaitingHeight{
            public HeightAtPoint awaitingHeight;
            public PointTransform point;
        }
    }
}