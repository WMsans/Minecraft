using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using System.Linq;
using System.Collections.Generic;

namespace sapra.InfiniteLands
{
    [CustomNode("Warp", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/filter/warp")]
    public class WarpNode : InfiniteLandsNode, IHeightMapConnector, IAmplifyGraph
    {
        [Input] public HeightData HeightMap;
        [Input] public HeightData Warp;

        [Input, Disabled] public HeightData Mask;
        [Output] public HeightData Output;
        public bool Amplified { get; set; }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            var connectionToWarp = allEdges.FirstOrDefault(a => a.inputPort.nodeGuid == guid && a.inputPort.fieldName == nameof(Warp));
            if (connectionToWarp == null)
                return;

            int newNodesCount = GraphTools.CopyConnectionsToInput(Graph, this, nameof(HeightMap), nameof(HeightMap), allNodes, allEdges);
            List<InfiniteLandsNode> newNodes = allNodes.GetRange(allNodes.Count - newNodesCount, newNodesCount);

            WarpGridNode warpGridNode = new WarpGridNode();
            warpGridNode.SetupNode(guid + "-warper", Vector2.zero);
            warpGridNode.Restart(Graph);
            

            GraphTools.FindEmptyAndAddConnection(typeof(GridData), warpGridNode, nameof(WarpGridNode.OutputGrid), newNodes, allEdges);
            EdgeConnection warpGridConnection = new EdgeConnection(connectionToWarp.outputPort, new PortData(warpGridNode.guid, nameof(WarpGridNode.Warp)));

            allEdges.Add(warpGridConnection);
            allNodes.Add(warpGridNode);
            if (IsAssigned(nameof(Mask)))
            {
                var connectionToMask = allEdges.FirstOrDefault(a => a.inputPort.nodeGuid == guid && a.inputPort.fieldName == nameof(Mask));
                EdgeConnection maskConnection = new EdgeConnection(connectionToWarp.outputPort, new PortData(warpGridNode.guid, nameof(WarpGridNode.Mask)));
                allEdges.Add(connectionToMask);
            }
            warpGridNode.AmplifyGraphSafe(allNodes, allEdges);
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