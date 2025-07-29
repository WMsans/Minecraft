using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class FinalTerrainNode : InfiniteLandsNode, IAmplifyGraph
    {
        [Input, Hide] public HeightData FinalHeight;
        [Output] public HeightData Output;

        public bool Amplified { get; set; }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            var outputNode = Graph.GetOutputNode();
            allEdges.Add(new EdgeConnection(new PortData(outputNode.guid, nameof(HeightOutputNode.FinalTerrain)), new PortData(this.guid, nameof(FinalHeight))));
        }

        protected override bool Process(BranchData branch)
        {   
            return true;
        }
        protected override void CacheOutputValues()
        {
        }
        protected override bool SetInputValues(BranchData branch)
        {
            return true;
        }

        public override bool TryGetOutputData<T>(BranchData branch, out T data, string fieldName, int listIndex = -1)
        {
            return TryGetInputData(branch, out data, nameof(FinalHeight));
        }
    }
}