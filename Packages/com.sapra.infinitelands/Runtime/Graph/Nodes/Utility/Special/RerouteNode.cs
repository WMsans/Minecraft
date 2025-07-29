using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Reroute", startCollapsed = true, docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/special/reroute")]
    public class RerouteNode : InfiniteLandsNode
    {
        [Input] public object Input;
        [Output(match_type_name: nameof(Input))] public object Output;

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
            return TryGetInputData(branch, out data, nameof(Input));
        }

        #if UNITY_EDITOR
        public override void OnDeleteNode()
        {
            var allEdges = Graph.GetBaseEdges();
            var nodeToInput = allEdges.Where(a => a.outputPort.nodeGuid.Equals(guid)).ToArray();
            var nodeToOutput = allEdges.Where(a => a.inputPort.nodeGuid.Equals(guid)).FirstOrDefault();
            if (nodeToOutput != null)
            {
                Graph.RemoveConnection(nodeToOutput);
                var outputPort = allEdges.Where(a => a.outputPort.Equals(nodeToOutput.outputPort)).Select(a => a.outputPort).FirstOrDefault();
                foreach (var input in nodeToInput)
                {
                    var inputPort = allEdges.Where(a => a.inputPort.Equals(input.inputPort)).Select(a => a.inputPort).FirstOrDefault();
                    Graph.RemoveConnection(input);
                    Graph.AddConnection(new EdgeConnection(nodeToOutput.outputPort, input.inputPort));
                }
            }
        }
        #endif
        
    }
}