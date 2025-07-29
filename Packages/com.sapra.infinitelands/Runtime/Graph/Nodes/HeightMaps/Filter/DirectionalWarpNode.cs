using UnityEngine;

using System.Collections.Generic;
using System.Linq;

namespace sapra.InfiniteLands
{
    [CustomNode("Directional Warp", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/filter/directional_warp")]
    public class DirectionalWarpNode : InfiniteLandsNode, IHeightMapConnector, IAmplifyGraph
    {
        [Input] public HeightData HeightMap;
        [Input, Disabled] public HeightData Mask;
        [Output] public HeightData Output;

        [Min(0.1f)] public float Strength = 5;
        public bool Amplified { get; set; }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            var connectionToHeightMap = allEdges.FirstOrDefault(a => a.inputPort.nodeGuid == guid && a.inputPort.fieldName == nameof(HeightMap));
            if (connectionToHeightMap == null)
                return;

            int newNodesCount = GraphTools.CopyConnectionsToInput(Graph, this, nameof(HeightMap), nameof(HeightMap), allNodes, allEdges);
            List<InfiniteLandsNode> newNodes = allNodes.GetRange(allNodes.Count - newNodesCount, newNodesCount);

            DirectionalWarpGridNode directionalWarpGridNode = new DirectionalWarpGridNode();
            directionalWarpGridNode.Strength = Strength;
            directionalWarpGridNode.SetupNode(guid + "-warper", Vector2.zero);
            directionalWarpGridNode.Restart(Graph);

            GraphTools.FindEmptyAndAddConnection(typeof(GridData), directionalWarpGridNode, nameof(DirectionalWarpGridNode.OutputGrid), newNodes, allEdges);
            EdgeConnection warpGridConnection = new EdgeConnection(connectionToHeightMap.outputPort, new PortData(directionalWarpGridNode.guid, nameof(DirectionalWarpGridNode.HeightMap)));

            allEdges.Add(warpGridConnection);
            allNodes.Add(directionalWarpGridNode);
            if (IsAssigned(nameof(Mask)))
            {
                var connectionToMask = allEdges.FirstOrDefault(a => a.inputPort.nodeGuid == guid && a.inputPort.fieldName == nameof(Mask));
                EdgeConnection maskConnection = new EdgeConnection(connectionToHeightMap.outputPort, new PortData(directionalWarpGridNode.guid, nameof(DirectionalWarpGridNode.Mask)));
                allEdges.Add(connectionToMask);
            }
            directionalWarpGridNode.AmplifyGraphSafe(allNodes, allEdges);
        }

        //Done so that it doesn't allocate for Output since, after amplifying, it's the same as input height map
        public void  ConnectHeightMap(PathData currentBranch, float scaleToResolutionRatio, int acomulatedResolution)
        {
            currentBranch.AllocateInput(this, nameof(HeightMap), acomulatedResolution);
        }

        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out HeightMap, nameof(HeightMap));
        }

        protected override bool Process(BranchData branch)
        {
            Output = HeightMap;
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}