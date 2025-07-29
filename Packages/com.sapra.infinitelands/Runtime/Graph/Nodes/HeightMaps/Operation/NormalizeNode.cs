using UnityEngine;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    [CustomNode("Normalize", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/normalize")]
    public class NormalizeNode : InfiniteLandsNode
    {
        [Input] public HeightData Input;
        [Output] public HeightData Output;
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Input, nameof(Input));
        }
        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);

            JobHandle job = RemapHeightJob.ScheduleParallel(map, Input.indexData,
                targetSpace, new Vector2(0, 1), Input.minMaxValue,
                 Input.jobHandle);

            Output = new HeightData(job, targetSpace, new Vector2(0, 1));
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}