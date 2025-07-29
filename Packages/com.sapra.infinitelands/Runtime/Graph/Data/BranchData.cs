using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class BranchData : StringObjectStore<object>
    {
        public TreeData treeData{get; private set;}
        public int branchID{get; private set;}
        public TerrainConfiguration terrain{get; private set;}
        public MeshSettings meshSettings{get; private set;}

        public float ScaleToResolutionRatio;
        public float ResolutionToScaleRatio;

        public InfiniteLandsNode[] StartingNodes { get; private set; }
        private Dictionary<int, InfiniteLandsNode> UsedNodes = new();
        private List<InfiniteLandsNode> AllUsedNodes = new();
        public bool isClosed { get; private set; }
        private List<NodeStore> RequestedStores = new();
        public void Reuse(TreeData treeSettings, 
            MeshSettings meshSettings, TerrainConfiguration terrain, InfiniteLandsNode[] startingNodes)
        {
            this.branchID = treeSettings.AddBranch(this);
            this.meshSettings = meshSettings;
            this.terrain = terrain;
            this.treeData = treeSettings;
            this.StartingNodes = startingNodes;
            isClosed = false;

            this.ScaleToResolutionRatio = meshSettings.ScaleToResolution;
            this.ResolutionToScaleRatio = 1.0f / ScaleToResolutionRatio;

            UsedNodes.Clear();
            RequestedStores.Clear();
            Reuse();
        }

        public void CloseBranch(){
            if(isClosed) return;

            foreach(var val in RequestedStores){
                val.Release();
                GenericPoolLight.Release(val);
            }
            foreach (var usedNode in AllUsedNodes)
            {
                GraphTools.ReturnNode(usedNode);
            }
            AllUsedNodes.Clear();

            foreach (var item in treeData.BranchClosers)
            {
                item.CloseBranch(this);
            }
            GenericPoolLight.Release(this);
            Release();
            isClosed = true;
        }

        public bool ForcedOrFinished(JobHandle job)
        {
            return treeData.ForcedOrFinished(job);
        }

        #region Getting Data
        public InfiniteLandsNode GetWriteableNode(InfiniteLandsNode referenceNode)
        {
            if (!UsedNodes.TryGetValue(referenceNode.small_index, out InfiniteLandsNode node))
            {
                node = GraphTools.GetWriteableNode(referenceNode);
                UsedNodes.Add(referenceNode.small_index, node);
            }
            return node;
        }

        public InfiniteLandsNode GetWriteableNode(int small_index)
        {
            if (UsedNodes.TryGetValue(small_index, out InfiniteLandsNode node))
            {
                return node;
            }
            else
                return default;
        }

        public T GetGlobalData<T>(bool required = true)
        {
            return treeData.GlobalStore.GetData<T>(required);
        }
        
        public TResult GetOrCreateGlobalData<TResult, TFactory>(string key, ref TFactory FactoryMaker)
            where TFactory : struct, IFactory<TResult>
        {
            return treeData.GlobalStore.GetOrCreateData<TResult, TFactory>(key, ref FactoryMaker);
        }
        #endregion

        #region Helper Methods
        public static BranchData NewMeshSettings(MeshSettings meshSettings, BranchData original, InfiniteLandsNode[] startingNodes)
        {
            var settings = GenericPoolLight<BranchData>.Get();
            settings.Reuse(original.treeData, meshSettings, original.terrain, startingNodes);
            InitializeBranch(settings, original);
            return settings;
        }

        public static BranchData NewTerrainSettings(TerrainConfiguration terrainConfig, BranchData original, InfiniteLandsNode[] startingNodes)
        {
            var settings = GenericPoolLight<BranchData>.Get();
            settings.Reuse(original.treeData, original.meshSettings, terrainConfig, startingNodes);
            InitializeBranch(settings, original);
            return settings;
        }

        public static BranchData NewChildBranch(MeshSettings newSettings, TerrainConfiguration newTerrain, BranchData original, InfiniteLandsNode[] startingNodes)
        {
            var settings = GenericPoolLight<BranchData>.Get();
            settings.Reuse(original.treeData, newSettings, newTerrain, startingNodes);
            InitializeBranch(settings, original);
            return settings;
        } 

        public static void InitializeBranch(BranchData currentBranch, BranchData previousBranch = null){
            foreach(var item in currentBranch.treeData.BranchInitializers){
                item.InitializeBranch(currentBranch, previousBranch);
            }
        }

        #endregion

        public Matrix4x4 GetVectorMatrix(GenerationModeNode nodeMode){
            Vector3 up;
            switch(nodeMode){
                case GenerationModeNode.Default:
                    switch(meshSettings.generationMode){
                        case MeshSettings.GenerationMode.RelativeToWorld:
                            up = terrain.TerrainNormal;
                            break;
                        default:
                            up = Vector3.up;
                            break;
                    }
                    break;
                case GenerationModeNode.RelativeToWorld:
                    up = terrain.TerrainNormal;
                    break;
                default: 
                    up = Vector3.up;
                    break;
            }

            return Matrix4x4.TRS(Vector3.zero, Quaternion.FromToRotation(Vector3.up, up), Vector3.one).inverse; 
        }
    }
}