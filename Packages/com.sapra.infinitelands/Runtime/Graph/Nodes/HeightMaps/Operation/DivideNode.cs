using Unity.Jobs;
using UnityEngine;
using System;

namespace sapra.InfiniteLands
{
    [CustomNode("Divide", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/divide")]
    public class DivideNode : InfiniteLandsNode
    {
        [Input] public HeightData Dividend;
        [Input] public HeightData Divisor;
        [Output] public HeightData Output;

        protected override bool SetInputValues(BranchData branch)
        {
            bool dividend = TryGetInputData(branch, out Dividend, nameof(Dividend));
            bool divisor = TryGetInputData(branch, out Divisor, nameof(Divisor));
            return dividend && divisor;
        }
        protected override bool Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);

            JobHandle job = DivideJob.ScheduleParallel(map,
                Dividend.indexData, Divisor.indexData, targetSpace,
                 JobHandle.CombineDependencies(Dividend.jobHandle, Divisor.jobHandle));

            Output = new HeightData(job, targetSpace, Dividend.minMaxValue);
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}