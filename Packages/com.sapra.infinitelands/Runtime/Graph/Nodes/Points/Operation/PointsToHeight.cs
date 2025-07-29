using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace sapra.InfiniteLands
{
    [CustomNode("Points to Height", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/operations/pointstoheight", synonims = new string[] { "Stamp" })]
    public class PointsToHeight : InfiniteLandsNode
    {
        [Input] public PointInstance Points;
        [Input, Hide] public GridData Grid;
        [Input, Disabled] public HeightData Texture;

        [Output] public HeightData Output;
        [Min(0.1f)] public float Size = 200;
        public bool IfMaskAssigned => !IsAssigned(nameof(Texture));
        public bool AffectedByScale = false;
        public bool AffectedByRotation = false;
        [ShowIf(nameof(ifAssigned))] public bool TextureHeightAffectedByScale;
        private bool ifAssigned => IsAssigned(nameof(Texture));

        [ShowIf(nameof(IfMaskAssigned))][Min(0.1f)] public float FallofDistance = 1;


        private BranchData textureSettings;
        private float FinalSize;

        protected override bool SetInputValues(BranchData branch)
        {
            bool points = TryGetInputData(branch, out Points, nameof(Points));
            bool grid = TryGetInputData(branch, out Grid, nameof(Grid));
            return points && grid;
        }
        protected override bool Process(BranchData branch)
        {
            if (state.SubState == 0)
            {
                if (ifAssigned)
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
                if (ifAssigned)
                {
                    if (!TryGetInputData(textureSettings, out Texture, nameof(Texture))) return false;
                }
                state.IncreaseSubState();
            }

            if (state.SubState == 2)
            {
                if (!Points.GetAllPoints(Grid.MeshScale + Size, branch.terrain.Position, out var foundPoints)) return false;

                HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
                var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
                JobHandle job;

                Vector2 minMaxHeight = new Vector2(0, 1);
                NativeArray<PointTransform> flattenedPoints = branch.GetData<ReturnableBranch>().GetData(foundPoints);
                if (ifAssigned)
                {
                    HeightMapBranch textureBranch = textureSettings.GetData<HeightMapBranch>();
                    var textureMap = textureBranch.GetMap();
                    JobHandle combined = JobHandle.CombineDependencies(Texture.jobHandle, Grid.jobHandle);
                    minMaxHeight = Texture.minMaxValue;
                    job = PointsToHeightTextureJob.ScheduleParallel(map, textureMap, Grid.meshGrid, Grid.Resolution,
                        flattenedPoints,
                        targetSpace, Texture.indexData,
                        foundPoints.Count, FinalSize,
                        AffectedByScale, AffectedByRotation, TextureHeightAffectedByScale, Texture.minMaxValue,
                        branch.terrain.Position, combined);
                }
                else
                {
                    job = PointsToHeightJob.ScheduleParallel(map, Grid.meshGrid, Grid.Resolution,
                        flattenedPoints, targetSpace,
                        flattenedPoints.Length, FallofDistance, Size,
                        AffectedByScale, branch.terrain.Position, Grid.jobHandle);
                }


                Output = new HeightData(job, targetSpace, minMaxHeight);
                state.IncreaseSubState();
            }

            return state.SubState == 3;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}