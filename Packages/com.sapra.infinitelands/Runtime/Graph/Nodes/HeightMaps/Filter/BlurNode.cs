using UnityEngine;
using Unity.Jobs;

using System;
using Unity.Collections;
namespace sapra.InfiniteLands
{
    [CustomNode("Blur", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/filter/blur")]
    public class BlurNode : InfiniteLandsNode, IHeightMapConnector
    {
        [Input] public HeightData HeightMap;
        [Input, Disabled] public HeightData Mask;
        [Output] public HeightData Output;

        [Min(0.01f)]public float BlurSize = .01f;

        public void ConnectHeightMap(PathData currentBranch, float scaleToResolutionRatio, int acomulatedResolution)
        {
            int Size = Mathf.CeilToInt(scaleToResolutionRatio*BlurSize);
            currentBranch.ApplyInputPadding(this, nameof(HeightMap), Size, acomulatedResolution);
            if(IsAssigned(nameof(Mask)))
                currentBranch.AllocateInput(this, nameof(Mask), acomulatedResolution);

            currentBranch.AllocateOutputs(this);
        }

        protected override bool SetInputValues(BranchData branch)
        {
            bool heightSet = TryGetInputData(branch, out HeightMap, nameof(HeightMap));
            bool maskSet = TryGetInputData(branch, out Mask, nameof(Mask));
            return heightSet && maskSet;
        }

        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);

            float Size = Mathf.Min(branch.ScaleToResolutionRatio * BlurSize, MapTools.MaxIncreaseSize);
            int EffectSize = Mathf.Max(1, Mathf.FloorToInt(Size));
            float ExtraSize = Mathf.Clamp01(Size - EffectSize);
            float averageMa = (EffectSize + ExtraSize) * 2 + 1;

            IndexAndResolution current = HeightMap.indexData;
            var length = MapTools.LengthFromResolution(current.Resolution);
            NativeArray<float> inbetween = new NativeArray<float>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeSlice<float> currentSlice = map.Slice(current.StartIndex, current.Length);
            NativeSlice<float> targetSlice = map.Slice(targetSpace.StartIndex, targetSpace.Length);
            JobHandle job;

            bool maskAssigned = IsAssigned(nameof(Mask));
            if (maskAssigned)
            {
                JobHandle onceCompleted = JobHandle.CombineDependencies(Mask.jobHandle, HeightMap.jobHandle);
                JobHandle checkX = BlurJob<BlurItJobX>.ScheduleParallel(currentSlice, current.Resolution, inbetween, current.Resolution, EffectSize, ExtraSize, averageMa, onceCompleted);
                job = BlurJobMasked<BlurItJobY>.ScheduleParallel(map, inbetween, current.Resolution, targetSlice, targetSpace.Resolution, EffectSize, ExtraSize, averageMa, Mask.indexData, current, checkX);
            }
            else
            {
                JobHandle checkX = BlurJob<BlurItJobX>.ScheduleParallel(currentSlice, current.Resolution, inbetween, current.Resolution, EffectSize, ExtraSize, averageMa, HeightMap.jobHandle);
                job = BlurJob<BlurItJobY>.ScheduleParallel(inbetween, current.Resolution, targetSlice, targetSpace.Resolution, EffectSize, ExtraSize, averageMa, checkX);
            }

            inbetween.Dispose(job);
            Output = new HeightData(job, targetSpace, HeightMap.minMaxValue);
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}