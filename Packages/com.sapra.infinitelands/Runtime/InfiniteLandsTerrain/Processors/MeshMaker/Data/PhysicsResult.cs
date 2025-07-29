using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace sapra.InfiniteLands.MeshProcess{
    public readonly struct PhysicsResult{
        public readonly NativeArray<int> meshIDs;
        public readonly List<MeshResult> results;
        public readonly JobHandle handle;
        public PhysicsResult(NativeArray<int> meshIDs, List<MeshResult> results, JobHandle handle){
            this.meshIDs = meshIDs;
            this.results = results;
            this.handle = handle;
        }
    }
}