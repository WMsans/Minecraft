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
    public struct CalculateChannels : IJobFor
    {
        [WriteOnly] NativeArray<float> channelX;
        [WriteOnly] NativeArray<float> channelZ;
        int channelsResolution;

        [ReadOnly] NativeArray<float3> normalMap;
        IndexAndResolution normalIndex;

        float4x4 localToWorld;
        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, channelsResolution, normalIndex.Resolution);
            float3 normal = normalMap[index];
            float4 inWorldSpace = mul(float4(normal.xyz, 1.0f),localToWorld);
            channelX[i] = inWorldSpace.x;
            channelZ[i] = inWorldSpace.z;
        }
        
        public static JobHandle ScheduleParallel(
            NativeArray<float3> normalMap, IndexAndResolution normalIndex,
            float4x4 localToWorld,
            NativeArray<float> channelX, NativeArray<float> channelZ, int channelsResolution,
            JobHandle dependency) => new CalculateChannels()
        {
            localToWorld = localToWorld,
            normalMap = normalMap,
            normalIndex = normalIndex,
            channelX = channelX,
            channelZ = channelZ,
            channelsResolution = channelsResolution,
        }.ScheduleParallel(channelX.Length, channelsResolution, dependency);
    }



    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct GetCavityJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalMap;

        [ReadOnly] NativeArray<float> channelX;
        [ReadOnly] NativeArray<float> channelZ;
        int channelsResolution;

        IndexAndResolution target;

        int EffectSize;
        float ExtraStrength;
        public void Execute(int i)
        {
            int2 vectorIndex = MapTools.IndexToVector(i, target.Resolution);
            float finalCountour = calculateDataRedMatrix(vectorIndex) + calculateDataBlueMatrix(vectorIndex);
            finalCountour = (finalCountour+2f)/4f;
            globalMap[target.StartIndex + i] = saturate(finalCountour);//(clamp(finalCountour,-totalAmount*2,totalAmount*2)+totalAmount*2)/(totalAmount*4f);
        }

        float calculateDataRedMatrix(int2 index){
            float result = 0;
            for(int dx = -EffectSize; dx <= EffectSize; dx++){
                int sng = (int)sign(dx);
                float currentNormal = getValueAtIndex(index.x+dx, index.y, channelX)*sign(dx);
                result += currentNormal;
                if(ExtraStrength != 0 && (dx == -EffectSize || dx == EffectSize)){
                    float next = getValueAtIndex(index.x+dx+sng, index.y, channelX)*sng;
                    result += next*ExtraStrength;
                }
            }
            result /= EffectSize+ExtraStrength;

            return result;
        }
        float calculateDataBlueMatrix(int2 index){          
            float result = 0;
            for(int dy = -EffectSize; dy <= EffectSize; dy++){
                int sng = (int)sign(dy);
                float currentNormal = getValueAtIndex(index.x, index.y+dy, channelZ)*sign(dy);
                result += currentNormal;

                if(ExtraStrength != 0 && (dy == -EffectSize || dy == EffectSize)){
                    float next = getValueAtIndex(index.x, index.y+dy+sng, channelZ)*sng;
                    result += next*ExtraStrength;
                }
                
            }
            result /= EffectSize+ExtraStrength;

            return result;
        }
        
        
        float getValueAtIndex(int x, int y, NativeArray<float> channel){
            int index = MapTools.GetFlatIndex(int2(x, y), target.Resolution, channelsResolution);
            return channel[index];
        }


        public static JobHandle ScheduleParallel(NativeArray<float> globalMap,      
            IndexAndResolution target, 
            NativeArray<float> channelX, NativeArray<float> channelZ, int channelsResolution,
            int EffectSize, float ExtraStrength,
            JobHandle dependency) => new GetCavityJob()
            {
                globalMap = globalMap,
                target = target,
                EffectSize = EffectSize,
                ExtraStrength = ExtraStrength,
                channelX = channelX,
                channelZ = channelZ,
                channelsResolution = channelsResolution,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}