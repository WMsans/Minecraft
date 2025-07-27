using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct BlockTypeTerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale)
    {
        if (!layer.enabled) return;

        // Get properties from the layer struct
        float stoneLevel = layer.properties[0];
        float dirtLevel = layer.properties[1];

        for (int i = 0; i < densityLength; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;

            if (worldY < stoneLevel)
            {
                voxelTypes[i] = 2; // Stone
            }
            else if (worldY < dirtLevel)
            {
                voxelTypes[i] = 1; // Dirt
            }
            else
            {
                voxelTypes[i] = 0; // Grass
            }
        }
    }

    public static TerrainLayer Create(float stoneLevel = -20f, float dirtLevel = -10f)
    {
        var layer = new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        // Set the properties for this layer
        layer.properties[0] = stoneLevel;
        layer.properties[1] = dirtLevel;

        return layer;
    }
}