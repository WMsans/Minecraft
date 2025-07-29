using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class WarpGridNode : InfiniteLandsNode, IAmplifyGraph
    {
        [Input] public HeightData Warp;
        [Input] public HeightData WarpVariant;
        [Input, Disabled] public HeightData Mask;

        [Input] public GridData BaseGrid;
        [Output] public GridData OutputGrid;

        public bool Amplified { get; set; }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            GraphTools.CopyConnectionsToInput(Graph, this, nameof(Warp), nameof(WarpVariant), allNodes, allEdges);
        }
        protected override bool SetInputValues(BranchData branch)
        {
            bool warpX = TryGetInputData(branch, out Warp, nameof(Warp));
            bool warpY = TryGetInputData(branch, out WarpVariant, nameof(WarpVariant));

            bool baseGrid = TryGetInputData(branch, out BaseGrid, nameof(BaseGrid));
            bool mask = TryGetInputData(branch, out Mask, nameof(Mask));

            return warpX && warpY && baseGrid && mask;
        }
        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var map = heightBranch.GetMap();

            IndexAndResolution warpXIndex = Warp.indexData;
            IndexAndResolution warpYIndex = WarpVariant.indexData;

            JobHandle dependancy = BaseGrid.jobHandle;
            var points = branch.GetData<ReturnableBranch>().GetData<float3>(warpXIndex.Length);
            JobHandle onceFinished = JobHandle.CombineDependencies(Warp.jobHandle, WarpVariant.jobHandle, dependancy);

            JobHandle finalJob;
            bool maskAssigned = IsAssigned(nameof(Mask));
            if (maskAssigned)
            {
                JobHandle afterBoth = JobHandle.CombineDependencies(onceFinished, Mask.jobHandle);
                finalJob = WarpPointsMaskedJob.ScheduleParallel(BaseGrid.meshGrid, BaseGrid.Resolution,
                    points, warpXIndex.Resolution,
                    map, warpXIndex, warpYIndex, Mask.indexData, afterBoth);
            }
            else
            {
                finalJob = WarpPointsJob.ScheduleParallel(BaseGrid.meshGrid, BaseGrid.Resolution,
                    points, warpXIndex.Resolution,
                    map, warpXIndex, warpYIndex,
                    onceFinished);
            }

            var finalScale = warpXIndex.Resolution * branch.ResolutionToScaleRatio;
            OutputGrid = new GridData(points,warpXIndex.Resolution,finalScale, finalJob);
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(OutputGrid, nameof(OutputGrid));
        }
    }
}