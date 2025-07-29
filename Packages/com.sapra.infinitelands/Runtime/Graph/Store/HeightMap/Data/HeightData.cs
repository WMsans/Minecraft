using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    public struct HeightData
    {
        public float2 minMaxValue;
        public IndexAndResolution indexData;
        public JobHandle jobHandle;

        public HeightData(JobHandle job, IndexAndResolution indexData, float2 MinMaxHeight)
        {
            this.jobHandle = job;
            this.indexData = indexData;
            this.minMaxValue = MinMaxHeight;
        }
    }
}