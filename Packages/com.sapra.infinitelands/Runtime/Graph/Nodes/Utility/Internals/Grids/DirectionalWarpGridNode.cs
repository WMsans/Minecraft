using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class DirectionalWarpGridNode : InfiniteLandsNode, IAmplifyGraph
    {
        [Input] public HeightData HeightMap;
        [Input] public NormalMapData NormalMap;
        [Input, Disabled] public HeightData Mask;

        [Input] public GridData BaseGrid;
        [Output] public GridData OutputGrid;

        public float Strength;
        public bool Amplified { get; set; }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            GraphTools.InterceptConnection<HeightToNormalNode>(guid, nameof(HeightMap), nameof(NormalMap),
                nameof(HeightToNormalNode.Input), nameof(HeightToNormalNode.NormalMap), allNodes, allEdges);
        }

        protected override bool SetInputValues(BranchData branch)
        {
            bool heightMap = TryGetInputData(branch, out HeightMap, nameof(HeightMap));
            bool normalMap = TryGetInputData(branch, out NormalMap, nameof(NormalMap));

            bool baseGrid = TryGetInputData(branch, out BaseGrid, nameof(BaseGrid));
            bool mask = TryGetInputData(branch, out Mask, nameof(Mask));

            return heightMap && normalMap && baseGrid && mask;
        }

        protected override bool Process(BranchData branch)
        {
            float targetStrength = Strength / branch.ScaleToResolutionRatio;
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var map = heightBranch.GetMap();

            JobHandle combined = JobHandle.CombineDependencies(BaseGrid.jobHandle, NormalMap.jobHandle);
            var points = branch.GetData<ReturnableBranch>().GetData<float3>(HeightMap.indexData.Length);

            JobHandle finalJob;
            if (IsAssigned(nameof(Mask)))
            {
                JobHandle afterBoth = JobHandle.CombineDependencies(combined, Mask.jobHandle);
                finalJob = WarpPointsNormalMaskedJob.ScheduleParallel(BaseGrid.meshGrid, BaseGrid.Resolution,
                    points, HeightMap.indexData.Resolution,
                    map, NormalMap.NormalMap, NormalMap.indexData, targetStrength, Mask.indexData,
                    afterBoth);
            }
            else
            {
                finalJob = WarpPointsNormalMapJob.ScheduleParallel(BaseGrid.meshGrid, BaseGrid.Resolution,
                    points, HeightMap.indexData.Resolution,
                    NormalMap.NormalMap, NormalMap.indexData, targetStrength,
                    combined);
            }

            var finalScale = HeightMap.indexData.Resolution * branch.ResolutionToScaleRatio;
            if (finalScale != BaseGrid.MeshScale)
                Debug.Log("tyo");
            OutputGrid = new GridData(points, HeightMap.indexData.Resolution, finalScale, finalJob);
            return true;
        }
        
        protected override void CacheOutputValues()
        {
            CacheOutputValue(OutputGrid, nameof(OutputGrid));
        }
    }
}