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
    public float scale;

    public void Execute(int index)
    {
        int x = index % (chunkSize + 1);
        int y = (index / (chunkSize + 1)) % (chunkSize + 1);
        int z = index / ((chunkSize + 1) * (chunkSize + 1));

        float worldX = offset.x + (x / (float)chunkSize - 0.5f) * scale;
        float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
        float worldZ = offset.z + (z / (float)chunkSize - 0.5f) * scale;

        // Simple Perlin noise for terrain height
        float noiseValue = noise.snoise(new float3(worldX, worldY, worldZ) * 0.05f) * 10f;

        // Density is the vertical distance from the noise-defined surface.
        // Negative is solid, positive is air.
        density[index] = worldY - noiseValue;
    }
}