using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class PathData
    {
        private class NodeOutputSpace
        {
            public int OutputResolution;
            public Dictionary<string, int> KeysAndCount;
            public InfiniteLandsNode node;
            public int ArrayOffset;
            private int MapLength;
            public int CurrentCount;

            public NodeOutputSpace(InfiniteLandsNode node, int OutputResolution)
            {
                this.OutputResolution = OutputResolution;
                this.node = node;
                this.KeysAndCount = new();

                ArrayOffset = -1;
                MapLength = -1;
                CurrentCount = 0;
            }

            public void Add(string key, int count)
            {
                KeysAndCount.TryAdd(key, CurrentCount);
                CurrentCount += count;
            }
            public bool ContainsKey(string key)
            {
                return KeysAndCount.ContainsKey(key);
            }

            public bool TryGetIndexOf(string key, out int index)
            {
                return KeysAndCount.TryGetValue(key, out index);
            }

            public int CompleteNodeOutputSpace(int ArrayOffset)
            {
                this.ArrayOffset = ArrayOffset;
                this.MapLength = MapTools.LengthFromResolution(OutputResolution);
                return MapLength;
            }

            public IndexAndResolution GetSpace(string fieldName)
            {            
                if (KeysAndCount.TryGetValue(fieldName, out int index))
                {
                    return new IndexAndResolution(ArrayOffset+index*MapLength, OutputResolution, MapLength);
                }

                Debug.LogErrorFormat("Trying to get space that wasn't preallocated {0} for {1} in {2}:{3}", fieldName, node.small_index, node.GetType());
                return default;
            }
        }

        public IEnumerable<InfiniteLandsNode> startingNodes;
        private Dictionary<int, NodeOutputSpace> NodeSpace = new();
        private List<int> Keys = new();

        public int TotalLength { get; private set; }
        public int MaxResolution { get; private set; }

        public HeightMapManager manager { get; private set; }
        private IGraph graph;
        private float ScaleToResolutionRatio;

        public PathData(HeightMapManager manager, float ScaleToResolutionRatio, IEnumerable<InfiniteLandsNode> startedNodes)
        {
            this.manager = manager;
            this.ScaleToResolutionRatio = ScaleToResolutionRatio;
            this.startingNodes = startedNodes;
            this.graph = manager.graph;
        }


        private NodeOutputSpace GetNodeOutputSpace(InfiniteLandsNode node)
        {
            if (NodeSpace.TryGetValue(node.small_index, out var spaceOutput))
                return spaceOutput;
            else
                Debug.LogErrorFormat("Store wasn't created for node {0} : {1} and it doesn't want to be created. Something went wrong", node.GetType(), node.small_index);
            return default;
        }

        #region Allocation
        private void AllocateOutputSpaceDirect(InfiniteLandsNode node, string key, int count)
        {
            GetNodeOutputSpace(node).Add(key, count);
        }

        /// <summary>
        /// Allocates space with the provided key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public void AllocateOutputSpace(InfiniteLandsNode node, string key)
        {
            AllocateOutputSpaceDirect(node, key, 1);
        }
        /// <summary>
        /// Allocates space and increases the amount of items stored by count
        /// </summary>
        /// <param name="key"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public void AllocateOutputSpace(InfiniteLandsNode node, string key, int count)
        {
            AllocateOutputSpaceDirect(node, key, count);
        }

        /// <summary>
        /// Allocates all the outputs defined in a node
        /// </summary>
        /// <param name="node"></param>
        public void AllocateOutputs(InfiniteLandsNode node)
        {
            var outputFields = node.GetOutputFields();
            foreach (var ndOutput in outputFields)
            {
                var fieldType = ndOutput.GetSimpleField();
                if (fieldType == typeof(HeightData))
                {
                    var attribute = ndOutput.GetCustomAttribute<OutputAttribute>();
                    int count = 1;
                    if (attribute.matchingList != null && attribute.matchingList != "")
                    {
                        count = node.GetCountOfNodesInInput(attribute.matchingList);
                    }
                    AllocateOutputSpace(node, ndOutput.Name, count);
                }
            }
        }


        public void FromInputToOutput(InfiniteLandsNode ogNode, string ogFieldName, int currentMaxResolution)
        {
            var edgesOfNode = ogNode.GetPortsToInput(ogFieldName);
            foreach (var outputPort in edgesOfNode)
            {
                var connectedNode = outputPort.node;
                StartRecursiveNode(currentMaxResolution, connectedNode);
            }
        }

        public void AllocateInput(InfiniteLandsNode node, string ogFieldName, int resolution)
        {
            FromInputToOutput(node, ogFieldName, resolution);
        }

        private bool ShouldUpdateNode(InfiniteLandsNode node, int currentResolution)
        {
            if (!NodeSpace.TryGetValue(node.small_index, out NodeOutputSpace space))
            {
                space = new NodeOutputSpace(node, currentResolution);
                NodeSpace.Add(node.small_index, space);
                Keys.Add(node.small_index);
                return true;
            }

            var storedResolution = space.OutputResolution;
            space.OutputResolution = Mathf.Max(storedResolution, currentResolution);
            return currentResolution > storedResolution;
        }

        /// <summary>
        /// Allocates space with padding in the resolution
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fieldName"></param>
        /// <param name="padding"></param>
        /// <param name="currentMaxResolution"></param>
        /// <returns></returns>
        public void ApplyInputPadding(InfiniteLandsNode node, string fieldName, int padding, int currentMaxResolution)
        {
            FromInputToOutput(node, fieldName, MapTools.IncreaseResolution(currentMaxResolution, padding));
        }

        #endregion

        public IndexAndResolution GetSpace(InfiniteLandsNode node, string fieldName)
        {
            var NodeSpace = GetNodeOutputSpace(node);
            return NodeSpace.GetSpace(fieldName);
        }

        public void StartNodeApplication(int InitialResolution)
        {
            int currentMaxResolution = InitialResolution;
            foreach (var startedNode in startingNodes)
            {
                StartRecursiveNode(currentMaxResolution, startedNode);
            }

            int finalLength = 0;
            int maxResolution = 0;
            foreach (var key in Keys)
            {
                var currentSpace = NodeSpace[key];
                int length = currentSpace.CompleteNodeOutputSpace(finalLength);
                finalLength += length * currentSpace.CurrentCount;
                maxResolution = Mathf.Max(maxResolution, currentSpace.OutputResolution);
            }
            TotalLength = finalLength;
            MaxResolution = maxResolution;
        }

        public void RecursiveNodeAllocation(int acomulatedResolution, InfiniteLandsNode node)
        {
            if (node is IHeightMapConnector customImplementation)
                customImplementation.ConnectHeightMap(this, ScaleToResolutionRatio, acomulatedResolution);
            else
                DefaultImplementation(acomulatedResolution, node);
        }

        private void DefaultImplementation(int resolution, InfiniteLandsNode node)
        {
            AllocateOutputs(node); //Allocate all the outputs of that node

            IEnumerable<PortData> connectionsToNode = node.GetPortsToInput(); //Go through each input node and allcoate the necessary outputs
            foreach (var outputPort in connectionsToNode)
            {
                var connectedNode = graph.GetNodeFromGUID(outputPort.nodeGuid);
                StartRecursiveNode(resolution, connectedNode);
            }
        }

        private void StartRecursiveNode(int currentMaxResolution, InfiniteLandsNode node)
        {
            var shouldUpdate = ShouldUpdateNode(node, currentMaxResolution);
            if (shouldUpdate)
                RecursiveNodeAllocation(currentMaxResolution, node);
        }
    }
}