using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

namespace sapra.InfiniteLands
{
    [CustomNode("Biome", typeof(WorldTree), docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/special/biome")]
    public class BiomeNode : InfiniteLandsNode, IAmplifyGraph
    {
        public string portName => "Biome Data";
        [FormerlySerializedAs("tree")] public BiomeTree biomeTree;
        private BiomeTree previousTree;

        [NonSerialized] private List<InfiniteLandsNode> NodeCopies = null;
        public bool Amplified { get; set; }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            NodeCopies?.Clear();
            if (biomeTree == null) return;

            int currentNodeCount = allNodes.Count;
            GraphTools.CopyFullGraph(guid, biomeTree, allNodes, allEdges);
            NodeCopies = allNodes.GetRange(currentNodeCount, allNodes.Count - currentNodeCount);
        }
        public List<InfiniteLandsNode> GetNodeCopies(){
            return NodeCopies;
        }
        protected override bool Process(BranchData branch)
        {
            return true;
        }
        
        public override bool ExtraValidations()
        {
            if(biomeTree == null)
                return true;
#if UNITY_EDITOR
            if (previousTree != null && previousTree != biomeTree)
            {
                previousTree.OnValuesChangedBefore -= OnValuesChangedBefore;
                previousTree.OnValuesChangedAfter -= OnValuesChangedAfter;
            }
            biomeTree.OnValuesChangedBefore -= OnValuesChangedBefore;
            biomeTree.OnValuesChangedBefore += OnValuesChangedBefore;

            biomeTree.OnValuesChangedAfter -= OnValuesChangedAfter;
            biomeTree.OnValuesChangedAfter += OnValuesChangedAfter;
            previousTree = biomeTree;
            #endif
            return true;
        }
        
        
        private void OnValuesChangedAfter() {
            if (biomeTree != null && biomeTree._autoUpdate)
            {
#if UNITY_EDITOR
                Graph.NotifyValuesChangedAfter();
#endif
            }
        }

        private void OnValuesChangedBefore()
        {
            if (biomeTree != null && biomeTree._autoUpdate)
            {
#if UNITY_EDITOR
                Graph.NotifyValuesChangedBefore();
#endif
            }
        }
    }
}