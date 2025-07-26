using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MarchingCubesJob : IJob
{
    // Data for the chunk
    [ReadOnly] public NativeArray<float> density;

    // Lookup tables passed into the job
    [ReadOnly] public NativeArray<int> triangulationTable;
    [ReadOnly] public NativeArray<int3> cornerOffsets;
    [ReadOnly] public NativeArray<int> cornerIndexAFromEdge;
    [ReadOnly] public NativeArray<int> cornerIndexBFromEdge;

    // Output mesh data
    [WriteOnly] public NativeList<float3> vertices;
    [WriteOnly] public NativeList<int> triangles;

    // Chunk parameters
    public int chunkSize;
    public int lod;
    // World space conversion
    [ReadOnly] public float3 nodeMin;
    [ReadOnly] public float nodeSize;
    // Voxel types
    [ReadOnly] public NativeArray<byte> voxelTypes;
    [WriteOnly] public NativeList<float> vertexTypes;

    public void Execute()
    {
        int step = 1 << lod; // lod 0 -> 1, lod 1 -> 2, etc.
        int numVerts = 0;
        float scale = nodeSize / chunkSize;

        for (int x = 0; x < chunkSize; x += step)
        {
            for (int y = 0; y < chunkSize; y += step)
            {
                for (int z = 0; z < chunkSize; z += step)
                {
                    var cubeDensities = new NativeArray<float>(8, Allocator.Temp);
                    var cubeVoxelTypes = new NativeArray<byte>(8, Allocator.Temp);
                    for (int i = 0; i < 8; i++)
                    {
                        int3 cornerOffset = cornerOffsets[i];
                        int3 corner = new int3(x, y, z) + cornerOffset * step;
                        cubeDensities[i] = density[CornerToIndex(corner)];
                        cubeVoxelTypes[i] = voxelTypes[CornerToIndex(corner)];
                    }

                    int cubeIndex = 0;
                    if (cubeDensities[0] < 0) cubeIndex |= 1;
                    if (cubeDensities[1] < 0) cubeIndex |= 2;
                    if (cubeDensities[2] < 0) cubeIndex |= 4;
                    if (cubeDensities[3] < 0) cubeIndex |= 8;
                    if (cubeDensities[4] < 0) cubeIndex |= 16;
                    if (cubeDensities[5] < 0) cubeIndex |= 32;
                    if (cubeDensities[6] < 0) cubeIndex |= 64;
                    if (cubeDensities[7] < 0) cubeIndex |= 128;

                    int triangulationTableIndex = cubeIndex * 16;

                    for (int i = 0; triangulationTable[triangulationTableIndex + i] != -1; i += 3)
                    {
                        int edgeIndex1 = triangulationTable[triangulationTableIndex + i];
                        int edgeIndex2 = triangulationTable[triangulationTableIndex + i + 1];
                        int edgeIndex3 = triangulationTable[triangulationTableIndex + i + 2];

                        int a0 = cornerIndexAFromEdge[edgeIndex1];
                        int b0 = cornerIndexBFromEdge[edgeIndex1];

                        int a1 = cornerIndexAFromEdge[edgeIndex2];
                        int b1 = cornerIndexBFromEdge[edgeIndex2];

                        int a2 = cornerIndexAFromEdge[edgeIndex3];
                        int b2 = cornerIndexBFromEdge[edgeIndex3];

                        byte typeA = cubeDensities[a0] < 0 ? cubeVoxelTypes[a0] : cubeVoxelTypes[b0];
                        byte typeB = cubeDensities[a1] < 0 ? cubeVoxelTypes[a1] : cubeVoxelTypes[b1];
                        byte typeC = cubeDensities[a2] < 0 ? cubeVoxelTypes[a2] : cubeVoxelTypes[b2];

                        float3 p1_a = new float3(x,y,z) + cornerOffsets[a0] * step;
                        float3 p1_b = new float3(x,y,z) + cornerOffsets[b0] * step;
                        
                        float3 p2_a = new float3(x,y,z) + cornerOffsets[a1] * step;
                        float3 p2_b = new float3(x,y,z) + cornerOffsets[b1] * step;

                        float3 p3_a = new float3(x,y,z) + cornerOffsets[a2] * step;
                        float3 p3_b = new float3(x,y,z) + cornerOffsets[b2] * step;


                        float3 vertA_local = p1_a + (0 - cubeDensities[a0]) / (cubeDensities[b0] - cubeDensities[a0]) * (p1_b - p1_a);
                        float3 vertB_local = p2_a + (0 - cubeDensities[a1]) / (cubeDensities[b1] - cubeDensities[a1]) * (p2_b - p2_a);
                        float3 vertC_local = p3_a + (0 - cubeDensities[a2]) / (cubeDensities[b2] - cubeDensities[a2]) * (p3_b - p3_a);


                        // Convert local vertex positions to world space before adding them
                        vertices.Add(vertA_local * scale + nodeMin);
                        vertices.Add(vertB_local * scale + nodeMin);
                        vertices.Add(vertC_local * scale + nodeMin);
                        
                        vertexTypes.Add(typeA);
                        vertexTypes.Add(typeB);
                        vertexTypes.Add(typeC);
                        
                        triangles.Add(numVerts++);
                        triangles.Add(numVerts++);
                        triangles.Add(numVerts++);
                    }
                }
            }
        }
    }

    private int CornerToIndex(int3 pos)
    {
        return pos.x + (chunkSize + 1) * (pos.y + (chunkSize + 1) * pos.z);
    }
}