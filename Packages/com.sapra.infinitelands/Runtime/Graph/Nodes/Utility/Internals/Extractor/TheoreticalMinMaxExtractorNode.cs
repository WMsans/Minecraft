using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class TheoreticalMinMaxExtractorNode : InfiniteLandsNode
    {
        [Input] public HeightData Input;
        [Output] public float2 MinMaxHeight;

        private BranchData LowQualityQuickSettings;
        private TreeData Tree;

        private TreeData independentTree;
        private bool Skip;

        private struct internalSnapshot
        {
            public BranchData branchData;
            public InfiniteLandsNode node;
            public bool completed;
            public float2 TheoreticalMinMax;
            public bool ProcessSnapshot()
            {
                return node.ProcessNode(branchData);
            }
        }

        private static Dictionary<string, internalSnapshot> CurrentlyProcessingNode = new();

        public override bool ExtraValidations()
        {
            CurrentlyProcessingNode.Remove(guid);
            return true;
        }
        protected override bool SetInputValues(BranchData branch)
        {
            if (state.SubState == 0)
            {
                Tree = branch.treeData;
                if (CurrentlyProcessingNode.TryGetValue(guid, out internalSnapshot node))
                {
                    if (node.completed || node.ProcessSnapshot())
                    {
                        Skip = true;
                        MinMaxHeight = node.TheoreticalMinMax;
                        return true;
                    }
                    else
                    {
                        Skip = false;
                        return false;
                    }
                }
                else
                {
                    Skip = false;
                    CurrentlyProcessingNode.Add(guid, new internalSnapshot()
                    {
                        branchData = branch,
                        node = this,
                        completed = false,
                    });
                    state.IncreaseSubState();
                }
            }

            if (state.SubState == 1)
            {
                MeshSettings meshSettings = new MeshSettings()
                {
                    Resolution = 1,
                    MeshScale = 100000,
                    Seed = branch.meshSettings.Seed,
                };

                TerrainConfiguration terrain = new TerrainConfiguration(default, default, Vector3.zero);

                independentTree = TreeData.NewTree(Tree.GlobalStore, meshSettings, terrain, GetNodesInInput(nameof(Input)));
                LowQualityQuickSettings = independentTree.GetTrunk();
                state.IncreaseSubState();
            }

            if (state.SubState == 2)
            {
                if (!TryGetInputData(LowQualityQuickSettings, out Input, nameof(Input))) return false;
                MinMaxHeight = Input.minMaxValue;

                CurrentlyProcessingNode.Remove(guid);
                CurrentlyProcessingNode.Add(guid, new internalSnapshot()
                {
                    completed = true,
                    TheoreticalMinMax = MinMaxHeight
                });
                state.IncreaseSubState();
            }

            return state.SubState == 3;
        }

        protected override bool Process(BranchData branch)
        {
            if (Skip) return true;

            if (!independentTree.ForcedOrFinished(Input.jobHandle)) return false;
            if (!independentTree.ProcessTree()) return false;

            independentTree.CloseTree();
            return true;
        }

        protected override void CacheOutputValues()
        {               

            CacheOutputValue(MinMaxHeight, nameof(MinMaxHeight));
        }
    }
}