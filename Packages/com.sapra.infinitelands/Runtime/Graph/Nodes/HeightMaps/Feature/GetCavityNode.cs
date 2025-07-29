using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Get Cavity", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/features/getcavity")]
    public class GetCavityNode : InfiniteLandsNode, IHeightMapConnector, IAmplifyGraph
    {
        [Min(0.01f)] public float CavitySize = 10;

        [Input] public HeightData Input;
        [Input, Hide] public NormalMapData NormalMapData;
        [Output] public HeightData Output;

        public GenerationModeNode FeatureMode = GenerationModeNode.Default;
        public bool Amplified { get; set; }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            GraphTools.InterceptConnection<HeightToNormalNode>(guid, nameof(Input), nameof(NormalMapData), nameof(HeightToNormalNode.Input), nameof(HeightToNormalNode.NormalMap), allNodes, allEdges);
        }

        public void ConnectHeightMap(PathData currentBranch, float scaleToResolutionRatio, int acomulatedResolution)
        {
            int Size = Mathf.CeilToInt(scaleToResolutionRatio * CavitySize);
            currentBranch.ApplyInputPadding(this, nameof(NormalMapData), Size, acomulatedResolution);
            currentBranch.AllocateOutputs(this);
        }

        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out NormalMapData, nameof(NormalMapData));
        }

        protected override bool Process(BranchData branch)
        {
            var heightMapBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightMapBranch.GetAllocationSpace(this, nameof(Output), out var map);
            float Size = Mathf.Min(branch.ScaleToResolutionRatio * CavitySize, MapTools.MaxIncreaseSize);
            int EffectSize = Mathf.Max(1, Mathf.FloorToInt(Size));
            float ExtraSize = Mathf.Clamp01(Size - EffectSize);

            var resolution = MapTools.IncreaseResolution(targetSpace.Resolution, -1);
            var channelLength = MapTools.LengthFromResolution(resolution);
            if (resolution <= 1) //skipping, too low resolution to calculate anything
            {
                Output = new HeightData(NormalMapData.jobHandle, NormalMapData.HeighData.indexData, new Vector2(0, 1));
                return true;
            }

            NativeArray<float> channelX = new NativeArray<float>(channelLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<float> channelZ = new NativeArray<float>(channelLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            Matrix4x4 targetMatrix = branch.GetVectorMatrix(FeatureMode);

            JobHandle separateMaps = CalculateChannels.ScheduleParallel(NormalMapData.NormalMap, NormalMapData.indexData,
                targetMatrix, channelX, channelZ, resolution,
                NormalMapData.jobHandle);

            JobHandle calculateCavities = GetCavityJob.ScheduleParallel(map, targetSpace, channelX, channelZ, resolution,
                EffectSize, ExtraSize, separateMaps);

            Output = new HeightData(calculateCavities, targetSpace, new Vector2(0, 1));
            channelX.Dispose(calculateCavities);
            channelZ.Dispose(calculateCavities);
            return true;
        }
        
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}