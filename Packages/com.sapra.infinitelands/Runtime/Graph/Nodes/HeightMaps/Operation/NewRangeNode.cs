using UnityEngine;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    [CustomNode("New Range", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/newrange")]
    public class NewRangeNode : InfiniteLandsNode
    {
        //New Range
        public float MinimumValue = 0;
        public float MaximumValue = 1;

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

            Vector2 minMax = new Vector2(MinimumValue, MaximumValue);
            JobHandle job = RemapHeightJob.ScheduleParallel(map, Input.indexData,
                targetSpace, minMax, Input.minMaxValue,
                 Input.jobHandle);

            Output = new HeightData(job, targetSpace, minMax);
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}