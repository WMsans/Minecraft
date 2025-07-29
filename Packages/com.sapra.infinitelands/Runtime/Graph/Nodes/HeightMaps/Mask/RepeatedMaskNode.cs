using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Repeated Mask", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/mask/repeatedmask.html")]
    public class RepeatedMaskNode : InfiniteLandsNode
    {
        //HeightPass
        public float StartingOffset;
        public float Size;
        public float EmptySize;
        public float BlendFactor = 20;

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

            JobHandle job = RepeatedSelectorJob.ScheduleParallel(map, Input.indexData, targetSpace,
                StartingOffset, Size, EmptySize, BlendFactor,
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