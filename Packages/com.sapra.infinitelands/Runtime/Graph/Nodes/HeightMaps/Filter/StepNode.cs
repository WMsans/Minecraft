using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    [CustomNode("Step", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/filter/step")]
    public class StepNode : InfiniteLandsNode
    {
        public enum StepMode
        {
            ByDistance,
            ByCount
        }

        public StepMode mode;
        public bool byDistance => mode == StepMode.ByDistance;

        [ShowIf(nameof(byDistance))] [Min(0.001f)]
        public float Distance = 20;
        public bool byCount => mode == StepMode.ByCount;

        [ShowIf(nameof(byCount))] [Min(1)]
        public int Steps = 1;
        [Range(0.001f, 1)] public float Stepness = 0;
        [Range(0, 1)] public float Flatness = 1;
        [Range(0, 1)] public float LevelVariance;


        [Input] public HeightData HeightMap;
        [Input, Disabled] public HeightData Mask;
        [Output] public HeightData Output;

        protected override bool SetInputValues(BranchData branch)
        {
            bool heightSet = TryGetInputData(branch, out HeightMap, nameof(HeightMap));        
            bool maskSet = TryGetInputData(branch, out Mask, nameof(Mask));
            return heightSet && maskSet;
        }

        protected override bool Process(BranchData branch)
        {
            bool maskAssigned = IsAssigned(nameof(Mask));
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();

            var arrayWrapFactory = new StepFloatFactory(HeightMap.minMaxValue, this);
            ArrawyWrap<float> levelHeight = branch.GetOrCreateGlobalData<ArrawyWrap<float>, StepFloatFactory>(this.guid, ref arrayWrapFactory);
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            JobHandle job;
            if (maskAssigned)
            {
                job = StepJobMasked.ScheduleParallel(map, HeightMap.indexData, targetSpace, Mask.indexData,
                    levelHeight.values.Length, Stepness, Flatness, HeightMap.minMaxValue, levelHeight.values,
                     JobHandle.CombineDependencies(HeightMap.jobHandle, Mask.jobHandle));
            }
            else
            {
                job = StepJob.ScheduleParallel(map, HeightMap.indexData, targetSpace,
                    levelHeight.values.Length, Stepness, Flatness, HeightMap.minMaxValue, levelHeight.values,
                    HeightMap.jobHandle);
            }

            Output = new HeightData(job, targetSpace, HeightMap.minMaxValue);
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }

        private struct StepFloatFactory : IFactory<ArrawyWrap<float>>
        {
            private readonly float2 minmaxHeight;
            private StepNode stepNode;

            public StepFloatFactory(float2 MinMaxHeight, StepNode stepNode){
                this.minmaxHeight = MinMaxHeight;
                this.stepNode = stepNode;
            }
            public ArrawyWrap<float> Create()
            {
                int stepCount;
                switch (stepNode.mode)
                {
                    case StepMode.ByDistance:
                        stepCount = Mathf.CeilToInt((minmaxHeight.y - minmaxHeight.x) / stepNode.Distance);
                        break;
                    default:
                        stepCount = stepNode.Steps;
                        break;
                }

                stepCount += 1;

                var levelHeight = new NativeArray<float>(stepCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                float accomulative = 0;
                levelHeight[0] = 0;


                for (int i = 1; i < stepCount; i++)
                {
                    float rand = randValue(i + 123 + stepCount * 12312 + Mathf.RoundToInt(accomulative * 13125));
                    accomulative += rand * 2;
                    levelHeight[i] = accomulative;
                }

                for (int i = 1; i < stepCount; i++)
                {
                    levelHeight[i] = Mathf.Lerp(minmaxHeight.x, minmaxHeight.y,
                        Mathf.Lerp((float)i / (stepCount - 1), levelHeight[i] / accomulative, stepNode.LevelVariance));
                }
                return new ArrawyWrap<float>(levelHeight);
            }
            
            float randValue(int z) // iq version
            {
                const uint k = 1103515245U; // GLIB C
                uint x = (uint)z;
                x = ((x >> 8) ^ x) * k;
                x = ((x >> 8) ^ x) * k;
                x = ((x >> 8) ^ x) * k;
                return (float)x / 0xffffffffU;
            }
        }
    }
}