using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct BlurJob<T> : IJobFor where T : ChannelBlurFast
    {
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<float> current;
        int currentResolution;

        [NativeDisableContainerSafetyRestriction]
        NativeSlice<float> target;
        int targetResolution;
        
        int EffectSize;
        float ExtraStrength;
        float averageMax;
        public void Execute(int x)
        {   
            float CT = 0;
            T blurInstance = default;
            for(int y = 0; y < targetResolution+1; y++){
                int2 vector = blurInstance.Flip(int2(x, y));
                float average = blurInstance.BlurValue(current, currentResolution, targetResolution, vector, EffectSize, ExtraStrength, averageMax, ref CT);
                int index = MapTools.VectorToIndex(vector, targetResolution);
                target[index] = average;
            }
        }

        public static JobHandle ScheduleParallel(NativeSlice<float> current, int currentResolution,
            NativeSlice<float> target, int targetResolution, int EffectSize, float ExtraStrength, float averageMax,
             JobHandle dependency) => new BlurJob<T>(){
                current = current,
                currentResolution = currentResolution,
                target = target,
                targetResolution = targetResolution,
                EffectSize = EffectSize,
                ExtraStrength = ExtraStrength,
                averageMax = averageMax,
            }.ScheduleParallel(targetResolution+1, 32, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct BlurJobMasked<T> : IJobFor where T : ChannelBlurFast
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalMap;
        
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<float> current;
        int currentResolution;

        [NativeDisableContainerSafetyRestriction]
        NativeSlice<float> target;
        int targetResolution;

        IndexAndResolution original;
        IndexAndResolution mask;
        
        int EffectSize;
        float ExtraStrength;
        float averageMax;
        public void Execute(int x)
        {           
            float CT = 0;
            T blurInstance = default;

            for(int y = 0; y < targetResolution+1; y++){
                int2 vector = blurInstance.Flip(int2(x, y));

                float average = blurInstance.BlurValue(current, currentResolution, targetResolution, vector, EffectSize, ExtraStrength, averageMax, ref CT);
                int index = MapTools.VectorToIndex(vector, targetResolution);

                int maskIndex = MapTools.RemapIndex(index, targetResolution, mask.Resolution);
                int originalIndex = MapTools.RemapIndex(index, targetResolution, original.Resolution);

                float maskValue = globalMap[mask.StartIndex + maskIndex];
                float originalValue = globalMap[original.StartIndex + originalIndex]; 
                target[index] = lerp(originalValue, average,maskValue);
            }
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap,
            NativeSlice<float> current, int currentResolution,
            NativeSlice<float> target, int targetResolution,
            int EffectSize, float ExtraStrength, float averageMax,
            IndexAndResolution mask,IndexAndResolution original, JobHandle dependency) => new BlurJobMasked<T>()
            {
                globalMap = globalMap,
                EffectSize = EffectSize,
                ExtraStrength = ExtraStrength,
                target = target,
                current = current,
                averageMax = averageMax,
                mask = mask,
                original = original,
                currentResolution = currentResolution,
                targetResolution = targetResolution,
            }.ScheduleParallel(targetResolution+1, 32, dependency);
    }


    public interface ChannelBlurFast{
        public float BlurValue(NativeSlice<float> current, int currentResolution,
            int targetResolution, int2 vector, 
            int EffectSize, float ExtraStrength, float averageMax, ref float currentTotal);
        public int2 Flip(int2 val);
    }

    public static class BlurMethods{
                    
        public static float getValueAtIndex(NativeSlice<float> current, int currentResolution,
            int targetResolution, int2 coord){
            int index = MapTools.GetFlatIndex(coord, targetResolution, currentResolution);
            index = Mathf.Clamp(index, 0, current.Length-1);
            return current[index];
        }

        public static float BlurValue(NativeSlice<float> current, int currentResolution,
            int targetResolution, int2 vector, 
            int EffectSize, float ExtraStrength, float averageMax, ref float currentTotal, bool isXJob){
            int primary = isXJob ? vector.x : vector.y;
            int secondary = isXJob ? vector.y : vector.x;

            if(secondary == 0)
            {
                currentTotal = 0;
                for(int z = -EffectSize; z <= EffectSize; z++){
                    int n = secondary+z;
                    currentTotal += getValueAtIndex(current, currentResolution, targetResolution,makeCoord(primary, n, isXJob));
                    int s = (int)sign(z);
                    if(z == -EffectSize || z == EffectSize)
                    {
                        float next = getValueAtIndex(current, currentResolution, targetResolution,makeCoord(primary, n+s, isXJob));
                        currentTotal += next*ExtraStrength;
                    }
                }
            }
            else{
                //Fully remove the previous edge
                currentTotal -= getValueAtIndex(current, currentResolution, targetResolution,makeCoord(primary, secondary-EffectSize-2, isXJob))*ExtraStrength;
                
                //Remove the full edge and at the half one
                currentTotal -= getValueAtIndex(current, currentResolution, targetResolution,makeCoord(primary, secondary-EffectSize-1, isXJob));
                currentTotal += getValueAtIndex(current, currentResolution, targetResolution,makeCoord(primary, secondary-EffectSize-1, isXJob))*ExtraStrength;
                
                //Remove the half edge, and add it fully
                currentTotal -= getValueAtIndex(current, currentResolution, targetResolution,makeCoord(primary, secondary+EffectSize, isXJob))*ExtraStrength;
                currentTotal += getValueAtIndex(current, currentResolution, targetResolution,makeCoord(primary, secondary+EffectSize, isXJob));
                
                //Add the new edge
                currentTotal += getValueAtIndex(current, currentResolution, targetResolution,makeCoord(primary, secondary+EffectSize+1, isXJob))*ExtraStrength;
            }
            
            return currentTotal/averageMax;
        }

        private static int2 makeCoord(int primary, int secondary, bool isXJob) {
            return isXJob ? new int2(primary, secondary) : new int2(secondary, primary);
        }

    }
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct BlurItJobX : ChannelBlurFast{
        public float BlurValue(NativeSlice<float> current, int currentResolution,
            int targetResolution, int2 vector, 
            int EffectSize, float ExtraStrength, float averageMax, ref float currentTotal){
            return BlurMethods.BlurValue(current, currentResolution, targetResolution, vector, EffectSize, ExtraStrength, averageMax,ref currentTotal, true);
        }

        public int2 Flip(int2 val) => new int2(val.x, val.y);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct BlurItJobY : ChannelBlurFast{
        public float BlurValue(NativeSlice<float> current, int currentResolution,
            int targetResolution, int2 vector, 
            int EffectSize, float ExtraStrength, float averageMax, ref float currentTotal){
            return BlurMethods.BlurValue(current, currentResolution, targetResolution, vector, EffectSize, ExtraStrength, averageMax,ref currentTotal, false);
        }
        
        public int2 Flip(int2 val) => new int2(val.y, val.x);

    }
}