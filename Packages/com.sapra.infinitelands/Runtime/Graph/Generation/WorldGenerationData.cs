using UnityEngine;

namespace sapra.InfiniteLands{
    [System.Serializable]
    public class WorldGenerationData : AwaitableData<ChunkData>
    {
        public ChunkData Result{get; private set;}
        public TerrainConfiguration terrain{get; private set;}
        public MeshSettings meshSettings{get; private set;}
        
        private int SubState = 0;

        private TreeData tree;
        private TreeData separateOutputTree;
        private MeshSettings separetOutputMeshSettings;


        private StringObjectStore<object> store;
        private HeightToWorldNode heightToWorldNode;
        private WorldGenerator worldGenerator;
        private CompletitionToken token;

        private WorldData biomeOutput;
        public void Reuse(WorldGenerator worldGenerator, TerrainConfiguration terrain, MeshSettings meshSettings)
        {
            this.worldGenerator = worldGenerator;
            this.store = worldGenerator.store;
            this.token = worldGenerator.token;
            heightToWorldNode = worldGenerator.heightToWorldNode;

            this.meshSettings = meshSettings;
            this.worldGenerator = worldGenerator;
            this.terrain = terrain;
            SubState = 0;
            tree = TreeData.NewTree(store, meshSettings, terrain, worldGenerator.mainBranchNodes);
            if(worldGenerator.SepartedOutputs){
                separetOutputMeshSettings = meshSettings;
                separetOutputMeshSettings.Resolution = meshSettings.TextureResolution;
                separateOutputTree = TreeData.NewTree(store, separetOutputMeshSettings, terrain, worldGenerator.separateBranchNodes);
            }else{
                separateOutputTree = null;
            }
            Result = null;
        }

        public bool ProcessData()
        {
            if (tree == null) return true;
            if (SubState == 0)
            {
                if (!tree.ProcessTree()) return false;
                if (separateOutputTree != null && !separateOutputTree.ProcessTree()) return false;
                SubState++;
            }

            if(SubState == 1){
                var targetNode = tree.GetTrunk().GetWriteableNode(heightToWorldNode);
                if(!targetNode.TryGetOutputData(tree.GetTrunk(), out biomeOutput, nameof(heightToWorldNode.Output)))
                {
                    Debug.LogError("System not finished");
                    SubState = 3;
                    return false;
                }
                Result = GenericPoolLight<ChunkData>.Get();
                Result.Reuse(terrain, meshSettings, biomeOutput.FinalPositions, biomeOutput.ChunkMinMax, biomeOutput.GlobalMinMax, worldGenerator, tree, separateOutputTree);
                SubState++;
            }

            return SubState == 2;
        }

        public bool ForceComplete(){
            token?.Complete();
            return ProcessData() && tree != null;
        }
    }
}