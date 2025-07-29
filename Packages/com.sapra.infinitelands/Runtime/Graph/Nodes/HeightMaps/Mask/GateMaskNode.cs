using Unity.Jobs;
using UnityEngine;
using static sapra.InfiniteLands.SampledAnimationCurve;

namespace sapra.InfiniteLands
{
    [CustomNode("Gate Mask", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/mask/gate")]
    public class GateMaskNode : InfiniteLandsNode
    {
        [ShowIf(nameof(showBoundedCurve)), BoundedCurve]
        public AnimationCurve FilterCurve =
            new AnimationCurve(new Keyframe[] { new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 0, 0) });
        [HideIf(nameof(showBoundedCurve))]
        public AnimationCurve GlobalFilterCurve =
            new AnimationCurve(new Keyframe[] { new Keyframe(0, 0, 0, 0), new Keyframe(1, 1000, 0, 0) });

        public CurveMode curveMode = CurveMode.Normalized;
        private bool showBoundedCurve => curveMode == CurveMode.Normalized;

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
            var factory = GetFactory(curveMode, GlobalFilterCurve, FilterCurve);
            SampledAnimationCurve curve = branch.GetOrCreateGlobalData<SampledAnimationCurve, SampledAnimationCurveFactory>(this.guid, ref factory);
            JobHandle job = CurveMaskJob.ScheduleParallel(map, targetSpace, Input.indexData,
                curve, Input.minMaxValue,
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