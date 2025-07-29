using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class MaskValidatorNode : InfiniteLandsNode
    {
        [Input] public HeightData Input;
        [Output] public MaskData MaskData;

        private NativeArray<int> MinMaxArray;
        private JobHandle WaitingForMask;
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Input, nameof(Input));
        }

        protected override bool Process(BranchData branch)
        {
            if (state.SubState == 0)
            {
                var returnableBranch = branch.GetData<ReturnableBranch>();
                var heightBranch = branch.GetData<HeightMapBranch>();

                MinMaxArray = returnableBranch.GetData<int>(1);
                MinMaxArray[0] = -1;
                var originMap = heightBranch.GetMap();
                WaitingForMask = CheckTreshold.ScheduleParallel(MinMaxArray, originMap,
                    Input.indexData, Input.indexData.Length, Input.indexData.Resolution, 0, Input.jobHandle);
                state.IncreaseSubState();
            }

            if (state.SubState == 1)
            {
                if (!branch.ForcedOrFinished(WaitingForMask)) return false;

                WaitingForMask.Complete();
                var minValue = MinMaxArray[0];
                MaskData = new MaskData()
                {
                    ContainsData = minValue > 0,
                    MaskResult = Input
                };
                state.IncreaseSubState();
                
            }
            return state.SubState == 2;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(MaskData, nameof(MaskData));
        }
    }
}