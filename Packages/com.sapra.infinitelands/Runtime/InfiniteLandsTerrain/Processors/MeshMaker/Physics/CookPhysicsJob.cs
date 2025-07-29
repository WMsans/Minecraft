using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands.MeshProcess
{
    //Actual responsability to generate a mesh
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct CookPhysicsJob : IJobFor
    {
        [ReadOnly] private NativeArray<int> meshIds;

        [ReadOnly] public readonly static MeshColliderCookingOptions cookingOptions = 
            MeshColliderCookingOptions.UseFastMidphase | MeshColliderCookingOptions.CookForFasterSimulation;


        public void Execute(int index)
        {
            #if UNITY_2022_1_OR_NEWER
            Physics.BakeMesh(meshIds[index], false, cookingOptions);
            #else
            Physics.BakeMesh(meshIds[index], false);
            #endif

        }

        public static JobHandle ScheduleParallel(NativeArray<int> meshIds, JobHandle dependency)
        {
            return new CookPhysicsJob()
            {
                meshIds = meshIds,
            }.ScheduleParallel(meshIds.Length, 2, dependency);
        }
    }
}