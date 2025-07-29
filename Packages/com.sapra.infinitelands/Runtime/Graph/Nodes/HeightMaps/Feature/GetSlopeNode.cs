using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Get Slope", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/features/getslope")]
    public class GetSlopeNode : InfiniteLandsNode, IAmplifyGraph
    {
        [Input] public HeightData Input;
        [Input, Hide] public NormalMapData NormalMapData;

        [Output] public HeightData Output;
        public bool Amplified { get; set; }

        public GenerationModeNode FeatureMode = GenerationModeNode.Default;
        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            GraphTools.InterceptConnection<HeightToNormalNode>(guid, nameof(Input), nameof(NormalMapData),nameof(HeightToNormalNode.Input), nameof(HeightToNormalNode.NormalMap), allNodes, allEdges);
        }

        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out NormalMapData, nameof(NormalMapData));
        }

        protected override bool Process(BranchData branch)
        {
            var targetSpace = branch.GetData<HeightMapBranch>().GetAllocationSpace(this, nameof(Output), out var map);
            Matrix4x4 targetMatrix = branch.GetVectorMatrix(FeatureMode);
            JobHandle job = GetSlope.ScheduleParallel(NormalMapData.NormalMap, NormalMapData.indexData,
                map, targetMatrix,
                targetSpace,
                NormalMapData.jobHandle);
            Output = new HeightData(job, targetSpace, new Vector2(0, 1));
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}