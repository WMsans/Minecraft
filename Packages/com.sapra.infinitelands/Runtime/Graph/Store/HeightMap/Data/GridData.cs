using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands{
    public struct GridData{
        public JobHandle jobHandle;
        public NativeArray<float3> meshGrid;
        public int Resolution;
        public float MeshScale;
        public GridData(NativeArray<float3> grid, int Resolution, float MeshScale, JobHandle job)
        {
            this.jobHandle = job;
            this.meshGrid = grid;
            this.Resolution = Resolution;
            this.MeshScale = MeshScale;
        }
    }
}