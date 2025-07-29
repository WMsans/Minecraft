using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class TreeData
    {
        private CompletitionToken token;
        public StringObjectStore<object> GlobalStore;
        private BranchData Trunk;
        private List<InfiniteLandsNode> outputNodes = new List<InfiniteLandsNode>();
        private List<BranchData> SubBranches = new List<BranchData>();
        public List<IInitializeBranch> BranchInitializers = new();
        public List<ICloseBranch> BranchClosers = new();

        public MeshSettings OriginalMeshSettings;
        public void Reuse(StringObjectStore<object> globalStore, InfiniteLandsNode[] startingNodes, MeshSettings settings)
        {
            this.OriginalMeshSettings = settings;
            this.outputNodes.Clear();
            this.SubBranches.Clear();
            for (int i = 0; i < startingNodes.Length; i++)
            {
                this.outputNodes.Add(startingNodes[i]);
            }
            this.token = globalStore.GetData<CompletitionToken>();
            this.GlobalStore = globalStore;

            BranchInitializers.Clear();
            BranchClosers.Clear();
            var objects = globalStore.GetManyDataRaw();
            foreach (var initialzier in objects)
            {
                if (initialzier is IInitializeBranch initializer)
                {
                    BranchInitializers.Add(initializer);
                }

                if (initialzier is ICloseBranch closer)
                {
                    BranchClosers.Add(closer);
                }
            }
            this.Trunk = null;
        }

        public bool ForceToComplete => token.complete;
        public bool ForcedOrFinished(JobHandle job)
        {
            if (!job.IsCompleted && !ForceToComplete) return false;
            job.Complete();
            return true;
        }
        public int AddBranch(BranchData branch)
        {
            if (SubBranches.Count <= 0)
                Trunk = branch;
            var cnt = SubBranches.Count;
            SubBranches.Add(branch);
            return cnt;
        }
        public BranchData GetTrunk() => Trunk;
        public bool ProcessTree()
        {
            var processor = new ProcessNode(Trunk);
            return AwaitableTools.IterateOverItems(outputNodes, ref processor);
        }

        public void CloseTree()
        {
            foreach (var branch in SubBranches)
            {
                branch.CloseBranch();
            }
            GenericPoolLight.Release(this);
        }

        public static TreeData NewTree(StringObjectStore<object> globalStore,
                MeshSettings meshSettings, TerrainConfiguration terrain, InfiniteLandsNode[] startingNodes)
        {
            TreeData treeSettings = GenericPoolLight<TreeData>.Get();
            treeSettings.Reuse(globalStore, startingNodes, meshSettings);

            var settings = GenericPoolLight<BranchData>.Get();
            settings.Reuse(treeSettings, meshSettings, terrain, startingNodes);
            BranchData.InitializeBranch(settings);
            return treeSettings;
        }
        
        
        private struct ProcessNode : ICallMethod<InfiniteLandsNode>
        {
            BranchData trunk;
            public ProcessNode(BranchData trunk){
                this.trunk = trunk;
            }
            public bool Callback(InfiniteLandsNode value)
            {
                if (value.isValid)
                {
                    var writableNode = trunk.GetWriteableNode(value);
                    return writableNode.ProcessNode(trunk);
                }
                return true;
            }
        }
    }
}