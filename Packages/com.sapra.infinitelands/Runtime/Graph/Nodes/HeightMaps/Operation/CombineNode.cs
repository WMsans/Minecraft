using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;

using System;

namespace sapra.InfiniteLands
{
    [CustomNode("Combine", 
        docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/combine", 
        synonims = new string[]{"Add", "Max", "Max", "Min", "HeightBased", "NormalizedMultiply", "Blend"})]
    public class CombineNode : InfiniteLandsNode, IHeightMapConnector
    {
        public enum Operation
        {
            Add,
            Max,
            Min,
            HeightBased,
            NormalizedMultiply
        }

        private bool isHeight => operation == Operation.HeightBased;
        public bool CalculateWeights;

        [ShowIf(nameof(isHeight))] public float BlendFactor = 10;
        public Operation operation;

        [Input] public List<HeightData> Input = new();
        [Output] public HeightData Output;
        [Output(match_list_name:nameof(Input)), Disabled, ShowIf(nameof(CalculateWeights))] public List<HeightData> Weights = new();
        public override bool ExtraValidations()
        {
            if (GetCountOfNodesInOutput(nameof(Weights)) > 0 && !CalculateWeights)
                return false;
            return true;
        }
        public void  ConnectHeightMap(PathData currentBranch, float scaleToResolutionRatio, int acomulatedResolution)
        {
            currentBranch.AllocateInput(this, nameof(Input), acomulatedResolution);
            currentBranch.AllocateOutputSpace(this, nameof(Output));
            if (CalculateWeights)
            {
                currentBranch.AllocateOutputSpace(this, nameof(Weights), GetCountOfNodesInInput(nameof(Input)));
            }
        }
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, ref Input, nameof(Input));            
        }

        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            int length = Input.Count;

            NativeArray<JobHandle> combinedJobs = new NativeArray<JobHandle>(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<HeightData> heightdatas = new NativeArray<HeightData>(length, Allocator.Persistent);

            for (int i = 0; i < length; i++)
            {
                combinedJobs[i] = Input[i].jobHandle;
                heightdatas[i] = Input[i];
            }
            JobHandle onceChild = JobHandle.CombineDependencies(combinedJobs);
            combinedJobs.Dispose();
            var minMax = GetMinMaxValue(Input);

            JobHandle combineJob;
            if (CalculateWeights)
            {
                var weigthSpace = heightBranch.GetAllocationSpace(this, nameof(Weights));

                NativeArray<IndexAndResolution> weightDatas = new NativeArray<IndexAndResolution>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                for (int x = 0; x < length; x++)
                {
                    IndexAndResolution nIndex = IndexAndResolution.CopyAndOffset(weigthSpace, x);
                    weightDatas[x] = nIndex;
                }
                switch (operation)
                {
                    case Operation.Max:
                        combineJob = MaxJobWeights.ScheduleParallel(map, heightdatas, targetSpace, weightDatas,
                            length, onceChild);
                        break;
                    case Operation.Min:
                        combineJob = MinJobWeights.ScheduleParallel(map, heightdatas, targetSpace, weightDatas,
                            length, onceChild);
                        break;
                    case Operation.HeightBased:
                        combineJob = HeightBlendWeights.ScheduleParallel(map, heightdatas, minMax, BlendFactor, weightDatas, targetSpace,
                            length, onceChild);
                        break;
                    case Operation.NormalizedMultiply:
                        combineJob = NormalizedMultiplyJobWeights.ScheduleParallel(map, heightdatas, weightDatas, targetSpace,
                            length,
                            onceChild);
                        break;
                    default:
                        combineJob = AddJobWeights.ScheduleParallel(map, heightdatas, targetSpace, weightDatas,
                            length,
                            onceChild);
                        break;
                }
                Weights.Clear();
                for (int i = 0; i < length; i++)
                {
                    Weights.Add(new HeightData(combineJob, weightDatas[i], new Vector2(0, 1)));
                }
                weightDatas.Dispose(combineJob);

            }
            else
            {
                switch (operation)
                {
                    case Operation.Max:
                        combineJob = MaxJob.ScheduleParallel(map, heightdatas, targetSpace,
                            length, onceChild);
                        break;
                    case Operation.Min:
                        combineJob = MinJob.ScheduleParallel(map, heightdatas, targetSpace,
                            length, onceChild);
                        break;
                    case Operation.HeightBased:
                        combineJob = HeightBlend.ScheduleParallel(map, heightdatas, targetSpace,
                            length, minMax, BlendFactor, onceChild);
                        break;
                    case Operation.NormalizedMultiply:
                        combineJob = NormalizedMultiplyJob.ScheduleParallel(map, heightdatas, targetSpace,
                            length,
                            onceChild);
                        break;
                    default:
                        combineJob = AddJob.ScheduleParallel(map, heightdatas, targetSpace,
                            length,
                            onceChild);
                        break;
                }
            }

            Output = new HeightData(combineJob, targetSpace, minMax);
            heightdatas.Dispose(combineJob);
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
            if(CalculateWeights){   
                CacheOutputValue(Weights, nameof(Weights));
            }
        }

        private Vector2 GetMinMaxValue(List<HeightData> dataGivers)
        {
            switch (operation)
            {
                case Operation.Max:
                {
                    Vector2 MinMaxValue = new Vector2(float.MinValue, float.MinValue);
                    foreach (HeightData data in dataGivers)
                    {
                        var minMax = data.minMaxValue;
                        MinMaxValue.x = math.max(MinMaxValue.x, minMax.x);
                        MinMaxValue.y = math.max(MinMaxValue.y, minMax.y);
                    }

                    return MinMaxValue;
                }
                case Operation.Min:
                {
                    Vector2 MinMaxValue = new Vector2(float.MaxValue, float.MaxValue);
                    foreach (HeightData data in dataGivers)
                    {
                        var minMax = data.minMaxValue;
                        MinMaxValue.x = math.min(MinMaxValue.x, minMax.x);
                        MinMaxValue.y = math.min(MinMaxValue.y, minMax.y);
                    }

                    return MinMaxValue;
                }
                case Operation.HeightBased:
                {
                    Vector2 MinMaxValue = new Vector2(float.MaxValue, float.MinValue);
                    foreach (HeightData data in dataGivers)
                    {
                        var minMax = data.minMaxValue;
                        MinMaxValue.x = math.min(MinMaxValue.x, minMax.x);
                        MinMaxValue.y = math.max(MinMaxValue.y, minMax.y);
                    }

                    return MinMaxValue;
                }
                case Operation.NormalizedMultiply:
                    return new Vector2(0, 1);
                default:
                {
                    Vector2 MinMaxValue = Vector2.zero;
                    foreach (HeightData data in dataGivers)
                    {
                        var minMax = data.minMaxValue;
                        MinMaxValue.x += minMax.x;
                        MinMaxValue.y += minMax.y;
                    }

                    return MinMaxValue;
                }
            }
        }
    }
}