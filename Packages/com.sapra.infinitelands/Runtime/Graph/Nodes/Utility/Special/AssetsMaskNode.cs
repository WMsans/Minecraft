using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands{
    [CustomNode("Assets Mask", docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/special/assetsmask.html", singleInstance = true)]
    public class AssetsMaskNode : InfiniteLandsNode, IAmplifyGraph
    {
        [Input] public HeightData Mask;
        public bool Amplified { get; set; }


        protected override bool SetInputValues(BranchData branch)
        {
            return true;
        }

        protected override bool Process(BranchData branch)
        {
            return true;
        }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            var outputNodes = Graph.GetBaseNodes().Where(a => typeof(IOutput).IsAssignableFrom(a.GetType()));
            var ogNode = Graph.GetBaseNodes().OfType<AssetsMaskNode>().FirstOrDefault();
            if (ogNode == null)
                return;
            string originalGuid = ogNode.guid;
            string parentGuid = guid.Replace(originalGuid, "");

            int currentCount = 0;
            var currentMaskConnection = allEdges.Where(a =>
                a.inputPort.nodeGuid.Equals(guid) &&
                a.inputPort.fieldName.Equals(nameof(Mask))).FirstOrDefault();
            if (currentMaskConnection == null)
                return;

            foreach (var outputNode in outputNodes)
            {
                var connectionIntoOutputNode = allEdges.Where(a => a.inputPort.nodeGuid.Equals(outputNode.guid + parentGuid)).FirstOrDefault();
                if (connectionIntoOutputNode != null)
                {
                    string newNodeGuid = this.guid + "-extra" + currentCount;
                    var appplyMaskNode = new ApplyMaskNode();
                    appplyMaskNode.SetupNode(newNodeGuid, position);
                    appplyMaskNode.ValueAtZero = ApplyMaskNode.ToValue.Zero;
                    allNodes.Add(appplyMaskNode);

                    EdgeConnection toOutputNodeInput = new EdgeConnection(new PortData(newNodeGuid, nameof(ApplyMaskNode.Output)), connectionIntoOutputNode.inputPort);
                    EdgeConnection toApplyMaskInput = new EdgeConnection(connectionIntoOutputNode.outputPort, new PortData(newNodeGuid, nameof(ApplyMaskNode.Input)));
                    EdgeConnection toApplyMaskMask = new EdgeConnection(currentMaskConnection.outputPort, new PortData(newNodeGuid, nameof(ApplyMaskNode.Mask)));
                    allEdges.Remove(connectionIntoOutputNode);
                    allEdges.Add(toOutputNodeInput);
                    allEdges.Add(toApplyMaskInput);
                    allEdges.Add(toApplyMaskMask);

                    appplyMaskNode.AmplifyGraphSafe(allNodes, allEdges);
                }
                currentCount++;
            }
        }
    }
}