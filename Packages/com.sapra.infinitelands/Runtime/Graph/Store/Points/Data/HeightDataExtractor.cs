using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct HeighDataExtractor : AwaitableData<HeightData>
    {
        public HeightData Result{get; private set;}
        TreeData newTree;
        BranchData branchData;

        InfiniteLandsNode node;
        private string fieldName;

        public HeighDataExtractor(InfiniteLandsNode node, string fieldName, StringObjectStore<object> globalStore, Vector3 position, MeshSettings meshSettings, out TreeData newTree)
        {
            this.node = node;
            this.fieldName = fieldName;
            Result = default;

            TerrainConfiguration terrain = new TerrainConfiguration(default, default, position);
            var related = node.GetNodesInInput(fieldName);
            newTree = TreeData.NewTree(globalStore ,meshSettings, terrain, related);
            this.newTree = newTree;
            this.branchData = this.newTree.GetTrunk();
        }
        public bool ProcessData()
        {
            if(!node.TryGetInputData(branchData, out HeightData resultsHeight, fieldName)) return false;
            Result = resultsHeight;
            return true;
        }
    }
}