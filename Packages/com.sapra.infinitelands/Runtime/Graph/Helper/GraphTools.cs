using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public static class GraphTools
    {
        public static Dictionary<string, List<InfiniteLandsNode>> NodeCopies = new();
        public static InfiniteLandsNode GetWriteableNode(InfiniteLandsNode reference)
        {
            var currentlyAvailable = GetAvailableNodes(reference.guid);
            int count = currentlyAvailable.Count;
            if (count <= 0)
            {
                return CreateNode(reference, true);
            }
            else
            {
                var availableNode = currentlyAvailable[count - 1];
                currentlyAvailable.RemoveAt(count - 1);
                availableNode.ResetState();
                return availableNode;
            }
        }

        public static void ReturnNode(InfiniteLandsNode node)
        {
            node.Return();
            var currentlyAvailable = GetAvailableNodes(node.guid);
            currentlyAvailable.Add(node);
        }

        public static void MarkNodeAsInvalid(string guid)
        {
            NodeCopies.Remove(guid);
        }

        public static T InterceptConnection<T>(string nodeGuid, string referenceInput, string inputName, string newNodeInputName, string newNodeOutputName,
            List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges) where T : InfiniteLandsNode, new()
        {
            var allNormalGuids = allNodes.OfType<T>();
            var connectionToInput = allEdges.FirstOrDefault(edge => edge.inputPort.nodeGuid == nodeGuid && edge.inputPort.fieldName == referenceInput);
            if (connectionToInput == default)
            {
                return default;
            }

            var connectionToTargetInput = allEdges.FirstOrDefault(edge => edge.inputPort.nodeGuid == nodeGuid && edge.inputPort.fieldName == inputName);
            if (connectionToTargetInput != null)
            {
                allEdges.Remove(connectionToTargetInput);
                Debug.Log("already exists that connection");
            }

            var currentGiverGuid = connectionToInput.outputPort.nodeGuid;

            T existingNode = null;
            foreach (var node in allNormalGuids)
            {
                var foundConnection = allEdges.FirstOrDefault(edge => edge.inputPort.nodeGuid == node.guid && edge.outputPort.nodeGuid == currentGiverGuid);
                if (foundConnection != null)
                {
                    existingNode = node;
                    break;
                }
            }

            if (existingNode == null)
            {
                existingNode = new T();
                var position = GetNodePosition(allNodes, connectionToInput.outputPort.nodeGuid, nodeGuid);
                existingNode.SetupNode(nodeGuid + inputName, position);
                allNodes.Add(existingNode);
                allEdges.Add(new EdgeConnection(connectionToInput.outputPort, new PortData(existingNode.guid, newNodeInputName)));
            }

            if(newNodeOutputName != null)
                allEdges.Add(new EdgeConnection(new PortData(existingNode.guid, newNodeOutputName), new PortData(nodeGuid, inputName)));
            return existingNode;
        }

        public static T AddNodeToOutput<T>(string originalGuid, string outputName, string targetInputName,
            List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges) where T : InfiniteLandsNode, new()
        {
            T finalOutputNode = new T();
            var position = GetNodePosition(allNodes, originalGuid, null);
            finalOutputNode.SetupNode(originalGuid + outputName, position);
            allNodes.Add(finalOutputNode);

            EdgeConnection finalConnection = new EdgeConnection(new PortData(originalGuid, outputName), new PortData(finalOutputNode.guid, targetInputName));
            allEdges.Add(finalConnection);
            return finalOutputNode;
        }

        private static Vector2 GetNodePosition(List<InfiniteLandsNode> allNodes, string A, string B)
        {
#if UNITY_EDITOR
            InfiniteLandsNode nodeA = allNodes.FirstOrDefault(a => a.guid == A);
            InfiniteLandsNode nodeB = allNodes.FirstOrDefault(b => b.guid == B);
            if (nodeA == null && nodeB == null) return Vector2.zero;
            if (nodeA != null && nodeB == null) return nodeA.position + Vector2.right * 200;

            return (nodeA.position + nodeB.position) / 2.0f;
#else
            return Vector2.zero;
            #endif

        }

        public static HeightData CopyHeightFromTo(InfiniteLandsNode node, string ToName, HeightData Input, BranchData From, BranchData To, float scale = 1)
        {
            HeightMapBranch fromHeightBranch = From.GetData<HeightMapBranch>();
            var from = fromHeightBranch.GetMap();

            HeightMapBranch heightBranch = To.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(node, ToName, out var to);

            JobHandle job = CopyToFrom.ScheduleParallel(to, from,
                targetSpace, Input.indexData,
                Input.jobHandle, scale);

            return new HeightData(job, targetSpace, Input.minMaxValue);
        }

        public static void FindEmptyAndAddConnection(Type inputsOfType, InfiniteLandsNode createdNode, string outputNodeField, List<InfiniteLandsNode> inNodes, List<EdgeConnection> allEdges)
        {
            foreach (var node in inNodes)
            {
                var inputs = node.GetType().GetInputOutputFields<InputAttribute>();
                var gridInputs = inputs.Where(a => a.GetSimpleField().Equals(inputsOfType));
                foreach (var gridInput in gridInputs)
                {
                    var connections = allEdges.Where(a => a.inputPort.nodeGuid == node.guid && a.inputPort.fieldName == gridInput.Name);
                    if (connections.Any())
                        continue;
                    else
                    {
                        EdgeConnection connection = new EdgeConnection(new PortData(createdNode.guid, outputNodeField), new PortData(node.guid, gridInput.Name));
                        allEdges.Add(connection);
                    }
                }
            }
        }

        public static List<InfiniteLandsNode> CollectAllNodes(InfiniteLandsNode node, string inputName, List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            List<InfiniteLandsNode> connectedNodes = new();
            var connectionsIntoInput = allEdges.Where(a => a.inputPort.nodeGuid == node.guid && a.inputPort.fieldName == inputName);
            if (!connectionsIntoInput.Any()) return connectedNodes;

            HashSet<string> checkedNodes = new();
            List<EdgeConnection> connectionsToCheck = ListPoolLight<EdgeConnection>.Get();
            connectionsToCheck.AddRange(connectionsIntoInput);
            for (int i = 0; i < connectionsToCheck.Count; i++)
            {
                var connection = connectionsToCheck[i];
                var nodeGuid = connection.outputPort.nodeGuid;
                if (checkedNodes.Contains(nodeGuid)) continue;

                checkedNodes.Add(nodeGuid);
                var foundNode = allNodes.FirstOrDefault(a => a.guid == nodeGuid);
                if (foundNode != null)
                {
                    connectedNodes.Add(foundNode);
                    connectionsToCheck.AddRange(allEdges.Where(a => a.inputPort.nodeGuid == nodeGuid));
                }
            }
            ListPoolLight<EdgeConnection>.Release(connectionsToCheck);
            return connectedNodes;
        }
        public static void AmplifyGraph(IGraph graph, List<InfiniteLandsNode> toAmplify, List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            int currentCount = allNodes.Count;
            for (int i = 0; i < toAmplify.Count; i++)
            {
                var node = toAmplify[i];
                if (node is IAmplifyGraph amplify)
                {
                    node.Restart(graph);
                    amplify.AmplifyGraphSafe(allNodes, allEdges);
                }
            }
            if(toAmplify != allNodes)
                toAmplify.AddRange(allNodes.GetRange(currentCount, allNodes.Count - currentCount));
        }

        public static int CopyConnectionsToInput(IGraph graph, InfiniteLandsNode node, string inputName, string assignNewInputName,
            List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            bool removeDuplicatePorts = inputName == assignNewInputName;
            List<InfiniteLandsNode> nodesInInput = CollectAllNodes(node, inputName, allNodes, allEdges);
            AmplifyGraph(graph, nodesInInput, allNodes, allEdges);

            List<EdgeConnection> connectionsToAdd = ListPoolLight<EdgeConnection>.Get();
            int newNodes = 0;
            for (int i = 0; i < nodesInInput.Count; i++)
            {
                var ogNode = nodesInInput[i];
                var nodeCopy = CreateNode(ogNode, false);
                nodeCopy.guid += node.guid;

                allNodes.Add(nodeCopy);
                newNodes++;

                connectionsToAdd.Clear();
                connectionsToAdd.AddRange(allEdges.Where(a => a.inputPort.nodeGuid == ogNode.guid));
                foreach (var connection in connectionsToAdd)
                {
                    var edgeCopy = new EdgeConnection(connection);
                    edgeCopy.inputPort.nodeGuid = nodeCopy.guid;

                    if (nodesInInput.Any(a => a.guid == edgeCopy.outputPort.nodeGuid))
                    {
                        edgeCopy.outputPort.nodeGuid += node.guid;
                        allEdges.Add(edgeCopy);
                    }
                }


                //Add to main input
                connectionsToAdd.Clear();
                connectionsToAdd.AddRange(allEdges.Where(a => a.outputPort.nodeGuid == ogNode.guid &&
                    a.inputPort.nodeGuid == node.guid &&
                    a.inputPort.fieldName == inputName));
                foreach (var connection in connectionsToAdd)
                {
                    var edgeCopy = new EdgeConnection(connection);
                    edgeCopy.inputPort.fieldName = assignNewInputName;
                    edgeCopy.outputPort.nodeGuid = nodeCopy.guid;
                    allEdges.Add(edgeCopy);
                    if (removeDuplicatePorts)
                        allEdges.Remove(connection);
                }
            }
            return newNodes;
        }

        public static int CopyFullGraph(string requestorID, IGraph baseGraph,
            List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            List<InfiniteLandsNode> originalNodes = baseGraph.GetBaseNodes().ToList();
            List<EdgeConnection> originalEdges = baseGraph.GetBaseEdges().ToList();
            List<InfiniteLandsNode> nodesCopies = new List<InfiniteLandsNode>();

            List<EdgeConnection> connectionsToAdd = ListPoolLight<EdgeConnection>.Get();
            int newNodes = 0;
            for (int i = 0; i < originalNodes.Count; i++)
            {
                var ogNode = originalNodes[i];
                var nodeCopy = CreateNode(ogNode, false);           
                if (nodeCopy is IAmplifyGraph amplifier)
                    amplifier.Amplified = false;
                nodeCopy.guid += requestorID;
                allNodes.Add(nodeCopy);
                nodesCopies.Add(nodeCopy);
                newNodes++;

                connectionsToAdd.Clear();
                connectionsToAdd.AddRange(originalEdges.Where(a => a.inputPort.nodeGuid == ogNode.guid));
                foreach (var connection in connectionsToAdd)
                {
                    var edgeCopy = new EdgeConnection(connection);
                    edgeCopy.inputPort.nodeGuid = nodeCopy.guid;

                    if (originalNodes.Any(a => a.guid == edgeCopy.outputPort.nodeGuid))
                    {
                        edgeCopy.outputPort.nodeGuid += requestorID;
                        var existingConnection = allEdges.Any(a =>
                            a.inputPort.nodeGuid == nodeCopy.guid &&
                            a.inputPort.fieldName == edgeCopy.inputPort.fieldName &&
                            a.inputPort.listIndex == edgeCopy.inputPort.listIndex);
                        if (!existingConnection)
                            allEdges.Add(edgeCopy);
                    }
                }
            }

            AmplifyGraph(baseGraph, nodesCopies, allNodes, allEdges);
            return newNodes;
        }
        
        #region Internals
        private static List<InfiniteLandsNode> GetAvailableNodes(string guid)
        {
            if (NodeCopies == null)
                NodeCopies = new();

            if (!NodeCopies.TryGetValue(guid, out var currentlyAvailable))
            {
                currentlyAvailable = new List<InfiniteLandsNode>();
                NodeCopies.Add(guid, currentlyAvailable);
            }
            return currentlyAvailable;
        }

        private static InfiniteLandsNode CreateNode(InfiniteLandsNode reference, bool writeable)
        {
            var copy = reference.GetCopy(writeable);
            if (!writeable)
                copy.SetupNode(reference.guid, reference.position);
            return copy;
        }
        #endregion
    }
}