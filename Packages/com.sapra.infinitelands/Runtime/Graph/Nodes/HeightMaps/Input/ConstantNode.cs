using UnityEngine;

using Unity.Mathematics;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    [CustomNode("Constant", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/constant")]
    public class ConstantNode : InfiniteLandsNode
    {
        public float Value;
        [Output] public HeightData Output;

        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpaceVectorized(this, nameof(Output), out var targetMap);
            JobHandle job = ConstantJob.ScheduleParallel(targetMap, Value,
                    targetSpace, default);

            Output = new HeightData(job, targetSpace, new Vector2(Value - 0.001f, Value));
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}