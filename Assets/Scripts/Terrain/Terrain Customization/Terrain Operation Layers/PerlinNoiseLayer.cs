using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class PerlinNoiseLayer : TerrainLayer
{
    public float noiseScale = 0.05f;
    public float noiseStrength = 10f;

    public override void Apply(NativeArray<float> density, int chunkSize, float3 offset, float scale)
    {
        if (!enabled) return;

        for (int i = 0; i < density.Length; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            float worldX = offset.x + (x / (float)chunkSize - 0.5f) * scale;
            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
            float worldZ = offset.z + (z / (float)chunkSize - 0.5f) * scale;

            float noiseValue = noise.snoise(new float3(worldX, worldY, worldZ) * noiseScale) * noiseStrength;
            density[i] = worldY - noiseValue;
        }
    }
}
