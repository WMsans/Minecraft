using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace sapra.InfiniteLands
{
    [CustomNode("Scale", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/scale")]
    public class ScaleNode : InfiniteLandsNode, IAmplifyGraph
    {
        public enum ScaleMode{OnlyPoints, OnlyHeight, Both}
        [Input] public HeightData Input;
        [Output] public HeightData Output;

        public bool RecalculateIfDifferentSeed() => false;
        [Min(0.01f)] public float Amount = 1;
        public ScaleMode Mode = ScaleMode.Both;

        private bool ScalesHeight => Mode == ScaleMode.Both || Mode == ScaleMode.OnlyHeight;
        private bool ScalesPoints => Mode == ScaleMode.Both || Mode == ScaleMode.OnlyPoints;

        public bool Amplified { get; set; }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            int newNodesCount = GraphTools.CopyConnectionsToInput(Graph, this, nameof(Input), nameof(Input), allNodes, allEdges);
            List<InfiniteLandsNode> newNodes = allNodes.GetRange(allNodes.Count - newNodesCount, newNodesCount);

            ScaleGridNode scaleGridNode = new ScaleGridNode();
            scaleGridNode.SetupNode(guid + "-scaler", Vector2.zero);
            scaleGridNode.Restart(Graph);
            scaleGridNode.Amount = ScalesPoints ? 1f / Amount : 1;

            GraphTools.FindEmptyAndAddConnection(typeof(GridData), scaleGridNode, nameof(ScaleGridNode.OutputGrid), newNodes, allEdges);

            allNodes.Add(scaleGridNode);
        }
        
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Input, nameof(Input));
        }

        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);

            JobHandle job = CopyToFrom.ScheduleParallel(map, map,
                targetSpace, Input.indexData,
                Input.jobHandle, ScalesHeight? Amount: 1);

            Output = new HeightData(job, targetSpace, Input.minMaxValue);
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}