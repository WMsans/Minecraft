using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct FlatTerrainLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale, void* entities)
    {
        if (!layer.enabled) return;

        // Get property from the layer struct
        float yLevel = layer.properties[0];

        for (int i = 0; i < densityLength; i++)
        {
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
            density[i] = worldY - yLevel;
        }
    }

    public static TerrainLayer Create(params float[] properties)
    {
        float yLevel = 0f;

        if (properties != null && properties.Length >= 1)
        {
            yLevel = properties[0];
        }

        return Create(yLevel);
    }

    public static TerrainLayer Create(float yLevel = 0f)
    {
        var layer = new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        // Set the property for this layer
        layer.properties[0] = yLevel;

        return layer;
    }

    public static string[] Fields() => new[] { "Y Level" };
}