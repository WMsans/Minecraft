using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct NoiseJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<float> density;

    public int chunkSize;
    public float3 offset;
    public int lod; // LOD level for scaling

    public void Execute(int index)
    {
        int x = index % (chunkSize + 1);
        int y = (index / (chunkSize + 1)) % (chunkSize + 1);
        int z = index / ((chunkSize + 1) * (chunkSize + 1));

        int scale = 1 << lod; // Scale factor based on LOD

        float worldX = (x * scale) + offset.x;
        float worldY = (y * scale) + offset.y;
        float worldZ = (z * scale) + offset.z;

        // Simple Perlin noise for terrain height
        // The noise is sampled over a larger area for lower LODs
        float noiseValue = noise.snoise(new float3(worldX, worldY, worldZ) * 0.05f) * 10f;

        // Density is the vertical distance from the noise-defined surface.
        // Negative is solid, positive is air.
        // The y-position is scaled to match the larger chunk size.
        density[index] = worldY - noiseValue;
    }
}