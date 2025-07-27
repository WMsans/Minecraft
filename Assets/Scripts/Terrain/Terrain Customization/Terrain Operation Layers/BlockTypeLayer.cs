using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public static unsafe class BlockTypeLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale)
    {
        if (!layer.enabled) return;

        for (int i = 0; i < densityLength; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            // We only need the y component to determine the block type from height
            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;

            if (worldY < -20)
            {
                voxelTypes[i] = 2; // Stone
            }
            else if (worldY < -10)
            {
                voxelTypes[i] = 1; // Dirt
            }
            else
            {
                voxelTypes[i] = 0; // Grass
            }
        }
    }

    public static TerrainLayer Create()
    {
        return new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
            noiseScale = 0, // Not used by this layer
            noiseStrength = 0 // Not used by this layer
        };
    }
}