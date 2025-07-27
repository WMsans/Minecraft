using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public unsafe struct BlockTypeTerrainLayer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Register()
    {
        var defaultLayer = Create();
        var defaultProps = new float[2];
        defaultProps[0] = defaultLayer.properties[0];
        defaultProps[1] = defaultLayer.properties[1];
        TerrainLayerRegistry.Register(nameof(BlockTypeTerrainLayer), Create, defaultProps);
    }

    [BurstCompile]
    private static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale)
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

    private static TerrainLayer Create(params float[] properties)
    {
        float stoneLevel = -20f;
        float dirtLevel = -10f;

        if (properties != null && properties.Length >= 2)
        {
            stoneLevel = properties[0];
            dirtLevel = properties[1];
        }

        return Create(stoneLevel, dirtLevel);
    }

    private static TerrainLayer Create(float stoneLevel = -20f, float dirtLevel = -10f)
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