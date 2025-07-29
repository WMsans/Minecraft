using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections;
using UnityEngine.Windows;
using System.Collections.Generic;

namespace sapra.InfiniteLands
{
    [CustomNode("Apply Mask", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/applymask")]
    public class ApplyMaskNode : InfiniteLandsNode, IAmplifyGraph
    {
        public enum ToValue{Minimum, Maximum, Zero}

        public ToValue ValueAtZero = ToValue.Minimum;
        [Input] public HeightData Input;
        [Input] public HeightData Mask;

        [Input, Hide] public MaskData MaskData;
        [Input, Hide] public float2 TheoreticalMinMax;

        [Output] public HeightData Output;

        private HeightMapBranch heightBranch;
        private float2 OriginMinMax;
        public bool Amplified { get; set; }

        public void AmplifyGraph(List<InfiniteLandsNode> allNodes, List<EdgeConnection> allEdges)
        {
            GraphTools.InterceptConnection<MaskValidatorNode>(guid, nameof(Mask), nameof(MaskData), nameof(MaskValidatorNode.Input), nameof(MaskValidatorNode.MaskData), allNodes, allEdges);
            GraphTools.InterceptConnection<TheoreticalMinMaxExtractorNode>(guid, nameof(Input), nameof(TheoreticalMinMax), nameof(TheoreticalMinMaxExtractorNode.Input), nameof(TheoreticalMinMaxExtractorNode.MinMaxHeight), allNodes, allEdges);
        }

        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out MaskData, nameof(MaskData));
        }

        protected override bool Process(BranchData branch)
        {
            if (state.SubState == 0)
            {
                heightBranch = branch.GetData<HeightMapBranch>();
                Mask = MaskData.MaskResult;
                if (MaskData.ContainsData)
                    state.SetSubState(21);
                else
                    state.SetSubState(11);
            }

            if (state.SubState == 11)
            {
                if (TryGetInputData(branch, out TheoreticalMinMax, nameof(TheoreticalMinMax)))
                    state.IncreaseSubState();
            }

            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            if (state.SubState == 12)
            {
                float value;
                switch (ValueAtZero)
                {
                    case ToValue.Maximum:
                        value = OriginMinMax.y;
                        break;
                    case ToValue.Minimum:
                        value = OriginMinMax.x;
                        break;
                    default:
                        value = 0;
                        break;
                }
                JobHandle job = ConstantJobSlow.ScheduleParallel(map, value, targetSpace, default);
                Output = new HeightData(job, targetSpace, GetMinMaxValue(OriginMinMax));
                state.SetSubState(30);
            }

            if (state.SubState == 21)
            {
                if (!TryGetInputData(branch, out Input, nameof(Input))) return false;
                state.IncreaseSubState();
            }

            if (state.SubState == 22)
            {
                JobHandle job;
                switch (ValueAtZero)
                {
                    case ToValue.Maximum:
                        job = ApplyMaskJob<Maximum>.ScheduleParallel(map,
                            Mask.indexData, Input.indexData, targetSpace, Input.minMaxValue, Input.jobHandle);
                        break;
                    case ToValue.Zero:
                        job = ApplyMaskJob<Zero>.ScheduleParallel(map,
                            Mask.indexData, Input.indexData, targetSpace, Input.minMaxValue, Input.jobHandle);
                        break;
                    default:
                        job = ApplyMaskJob<Minimum>.ScheduleParallel(map,
                            Mask.indexData, Input.indexData, targetSpace, Input.minMaxValue, Input.jobHandle);
                        break;
                }
                Output = new HeightData(job, targetSpace, GetMinMaxValue(Input.minMaxValue));
                state.SetSubState(30);
            }
            return state.SubState == 30;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }

        private Vector2 GetMinMaxValue(Vector2 InputMinMax)
        {
            if (ValueAtZero == ToValue.Zero)
                return new Vector2(Mathf.Min(0, InputMinMax.x),
                    Mathf.Max(0, InputMinMax.y));
            return InputMinMax;
        }

        private struct Minimum : MaskMultiplyMode
        {
            public float GetValue(float2 minMax, float value, float mask)
            {
                return lerp(minMax.x, value, mask);
            }
        }

        private struct Maximum : MaskMultiplyMode
        {
            public float GetValue(float2 minMax, float value, float mask)
            {
                return lerp(minMax.y, value, mask);
            }
        }

        private struct Zero : MaskMultiplyMode
        {
            public float GetValue(float2 minMax, float value, float mask)
            {
                return value*mask;
            }
        }
    }
}