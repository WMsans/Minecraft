using UnityEngine;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    [CustomNode("Fold", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/fold", synonims = new string[]{"Absolute", "Flip", "Reverse"})]
    public class FoldNode : InfiniteLandsNode
    {
        public enum Refe{Bottom, Top}
        [Range(0,1)] public float FoldingLine = 0.5f;
        public Refe RelativeTo;

        private Vector2 GetMinMaxValue(Vector2 InputMinMax)
        {
            Vector2 newMinMax;
            Vector2 currentMinMax = InputMinMax;

            float amountOfValue = .5f-Mathf.Abs(FoldingLine-.5f);

            float displacement = currentMinMax.y-currentMinMax.x;
            switch(RelativeTo){
                case Refe.Top:
                {
                    newMinMax.x = currentMinMax.x+displacement*amountOfValue;
                    newMinMax.y = currentMinMax.y;
                    break;
                }
                default:
                {
                    newMinMax.x = currentMinMax.x;
                    newMinMax.y = currentMinMax.y-displacement*amountOfValue;
                    break;
                }
            }
            return InputMinMax;
        }

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

            JobHandle job;
            switch (RelativeTo)
            {
                case Refe.Top:
                    {
                        job = FoldJobTop.ScheduleParallel(map, Input.indexData, targetSpace,
                            FoldingLine, Input.minMaxValue,
                             Input.jobHandle);
                        break;
                    }
                default:
                    {
                        job = FoldJobBottom.ScheduleParallel(map, Input.indexData, targetSpace,
                            FoldingLine, Input.minMaxValue,
                             Input.jobHandle);
                        break;
                    }
            }

            Output = new HeightData(job, targetSpace, GetMinMaxValue(Input.minMaxValue));
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}