using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Height Output", canCreate = false, canDelete = false, startCollapsed = true, docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/output/heightoutput")]
    public class HeightOutputNode : InfiniteLandsNode, IAmplifyGraph
    {
        [Input] public HeightData HeightMap;
        [Output, HideIf(nameof(HideOutput)), Disabled] public HeightData FinalTerrain;
        public bool Amplified { get; set; }

        public bool HideOutput => Graph != null && Graph.GetType().Equals(typeof(WorldTree));
        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            bool itsVariantGraph = allNodes.OfType<HeightOutputNode>().Any(a => a != this);
            if (!itsVariantGraph)
            {
                var prexistingOne = allNodes.FirstOrDefault(a => a.GetType().Equals(typeof(HeightToWorldNode)));
                if (prexistingOne != null)
                    Debug.LogError("Something went wrong");

                var heightToWorld = GraphTools.AddNodeToOutput<HeightToWorldNode>(guid, nameof(FinalTerrain), nameof(HeightToWorldNode.HeightMap), allNodes, allEdges);
                heightToWorld.AmplifyGraphSafe(allNodes, allEdges);

                //Ensures that all nodes before this one are amplified
                GraphTools.AmplifyGraph(Graph, allNodes, allNodes, allEdges);

                BasicGridNode basicGridNode = new BasicGridNode();
                basicGridNode.SetupNode("Default Grid", Vector2.zero);
                basicGridNode.Restart(Graph);
                allNodes.Add(basicGridNode);

                GraphTools.FindEmptyAndAddConnection(typeof(GridData), basicGridNode, nameof(BasicGridNode.OutputGrid), allNodes, allEdges);
            }
            else
            {
                FinalTerrainNode finalTerrain = allNodes.OfType<FinalTerrainNode>().FirstOrDefault();
                if (finalTerrain == null)
                {
                    finalTerrain = new FinalTerrainNode();
                    finalTerrain.SetupNode(guid + "-finalTerrain", position);
                    allNodes.Add(finalTerrain);
                }

                var ogNode = Graph.GetOutputNode();
                if (ogNode == null) return;

                string originalGuid = ogNode.guid;
                string parentGuid = guid.Replace(originalGuid, "");

                var edgesToTweak = allEdges.Where(a => a.outputPort.nodeGuid == guid && a.inputPort.nodeGuid.Contains(parentGuid)).Select(a => a.inputPort.nodeGuid).ToList();
                HandleConnections(finalTerrain, allEdges, edgesToTweak);
            }
        }

        private void HandleConnections(FinalTerrainNode finalTerrainNode, List<EdgeConnection> allEdges, IEnumerable<string> inputsGuids)
        {
            foreach (var portGuid in inputsGuids)
            {
                var similarConnections = allEdges.Where(a => a.inputPort.nodeGuid == portGuid).ToList();
                foreach (var similarConnection in similarConnections)
                {
                    allEdges.Remove(similarConnection);
                    EdgeConnection newConnection = new EdgeConnection(new PortData(finalTerrainNode.guid, nameof(FinalTerrainNode.Output)), similarConnection.inputPort);
                    allEdges.Add(newConnection);
                }
            }
        }

        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out HeightMap, nameof(HeightMap));
        }
        protected override bool Process(BranchData branch)
        {
            FinalTerrain = HeightMap;
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(FinalTerrain, nameof(FinalTerrain));
        }
    }
}