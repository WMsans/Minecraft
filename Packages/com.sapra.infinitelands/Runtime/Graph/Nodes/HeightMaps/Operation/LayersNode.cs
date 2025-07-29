using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace sapra.InfiniteLands
{
    [CustomNode("Layers", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/layers")]
    public class LayersNode : InfiniteLandsNode
    {
        [Input] public List<HeightData> Input = new();
        [Output(match_list_name:nameof(Input))] public List<HeightData> Weights = new();
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, ref Input, nameof(Input));;
        }

        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            int length = Input.Count;


            NativeArray<JobHandle> combinedJobs = new NativeArray<JobHandle>(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<HeightData> heightdatas = new NativeArray<HeightData>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < length; i++)
            {
                combinedJobs[i] = Input[i].jobHandle;
                heightdatas[i] = Input[i];
            }
            JobHandle onceChild = JobHandle.CombineDependencies(combinedJobs);
            combinedJobs.Dispose();

            var weigthSpace = heightBranch.GetAllocationSpace(this, nameof(Weights), out var map);
            NativeArray<IndexAndResolution> weightDatas = new NativeArray<IndexAndResolution>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int x = 0; x < length; x++)
            {
                IndexAndResolution nIndex = IndexAndResolution.CopyAndOffset(weigthSpace, x);
                weightDatas[x] = nIndex;
            }

            JobHandle combineJob = LayersJob.ScheduleParallel(map, heightdatas, weigthSpace, weightDatas, length, onceChild);
            Weights.Clear();
            for (int i = 0; i < length; i++)
            {
                Weights.Add(new HeightData(combineJob, weightDatas[i], new Vector2(0, 1)));
            }

            weightDatas.Dispose(combineJob);
            heightdatas.Dispose(combineJob);
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Weights, nameof(Weights));
        }
    }
}