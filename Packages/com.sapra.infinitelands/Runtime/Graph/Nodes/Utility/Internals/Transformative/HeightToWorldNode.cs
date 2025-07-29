using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class HeightToWorldNode : InfiniteLandsNode, IAmplifyGraph, IOutput
    {
        [Input] public HeightData HeightMap;
        [Input, Hide] public NormalMapData NormalMap;
        [Output] public WorldData Output;

        private float[] edges = new float[2];
        public bool Amplified { get; set; }

        public string OutputVariableName => nameof(Output);
        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            GraphTools.InterceptConnection<HeightToNormalNode>(this.guid, nameof(HeightMap), nameof(NormalMap), nameof(HeightToNormalNode.Input), nameof(HeightToNormalNode.NormalMap), allNodes, allEdges);
        }

        protected override bool SetInputValues(BranchData branch)
        {
            bool input = TryGetInputData(branch, out HeightMap, nameof(HeightMap));
            bool normalMap = TryGetInputData(branch, out NormalMap, nameof(NormalMap));
            return input && normalMap;
        }

        protected override bool Process(BranchData branch)
        {
            if (state.SubState == 0)
            {
                edges[0] = HeightMap.minMaxValue.y;
                edges[1] = HeightMap.minMaxValue.x;

                var heightMapBranch = branch.GetData<HeightMapBranch>();
                int length = MapTools.LengthFromResolution(branch.meshSettings.Resolution);
                var globalMap = heightMapBranch.GetMap();

                NativeArray<Vertex> finalPositions = branch.GetData<ReturnableBranch>().GetData<Vertex>(length);
                NativeArray<float> MinMaxHeight = branch.GetData<ReturnableBranch>().GetData(edges);

                JobHandle applyHeight = ApplyHeightJob.ScheduleParallel(finalPositions, globalMap,
                    MinMaxHeight, HeightMap.indexData, branch.meshSettings.Resolution, branch.meshSettings.MeshScale,
                    NormalMap.NormalMap, NormalMap.indexData, NormalMap.jobHandle);
                Output = new WorldData(MinMaxHeight, finalPositions, HeightMap.minMaxValue, applyHeight);
                state.IncreaseSubState();
            }
            if (state.SubState == 1)
            {
                if (!branch.treeData.ForcedOrFinished(Output.jobHandle)) return false;
                state.IncreaseSubState();
            }
            return state.SubState == 2;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}