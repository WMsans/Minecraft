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
    
    public void Execute(int index)
    {
        int x = index % (chunkSize + 1);
        int y = (index / (chunkSize + 1)) % (chunkSize + 1);
        int z = index / ((chunkSize + 1) * (chunkSize + 1));
        
        float worldX = x + offset.x;
        float worldY = y + offset.y;
        float worldZ = z + offset.z;

        // Simple Perlin noise for terrain height
        float noiseValue = noise.snoise(new float3(worldX, worldY, worldZ) * 0.05f) * 10f;
        
        // Density is the vertical distance from the noise-defined surface.
        // Negative is solid, positive is air.
        density[index] = worldY - noiseValue;
    }
}