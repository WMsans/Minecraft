using Unity.Entities;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EntityLODSystem : ISystem
{
    private EntityQuery highDetailQuery;
    private EntityQuery lowDetailQuery;

    public void OnCreate(ref SystemState state)
    {
        // Query for entities that have the high detail tag but whose chunk is no longer high detail
        highDetailQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<HighDetail, ChunkOwner>()
            .Build(ref state);

        // Query for entities that DON'T have the high detail tag but whose chunk IS now high detail
        lowDetailQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ChunkOwner>()
            .WithNone<HighDetail>()
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Get the list of high-detail node indices from the terrain manager
        // This is a simplification; a more robust solution might use a NativeArray
        // populated by a job.
        var highDetailNodes = OctreeTerrainManager.Instance.GetEntityProcessingChunks(); 

        // Add HighDetail component to entities in high-detail chunks
        var lowDetailChunks = lowDetailQuery.ToArchetypeChunkArray(Allocator.Temp);
        foreach (var chunk in lowDetailChunks)
        {
            var entities = chunk.GetNativeArray(SystemAPI.GetEntityTypeHandle());
            var chunkOwners = chunk.GetSharedComponentManaged(SystemAPI.GetSharedComponentTypeHandle<ChunkOwner>(), state.EntityManager);
            for (int i = 0; i < chunk.Count; i++)
            {
                if (highDetailNodes.Contains(chunkOwners.NodeIndex))
                {
                    ecb.AddComponent<HighDetail>(entities[i]);
                }
            }
        }

        // Remove HighDetail component from entities no longer in high-detail chunks
        var highDetailChunks = highDetailQuery.ToArchetypeChunkArray(Allocator.Temp);
        foreach (var chunk in highDetailChunks)
        {
            var entities = chunk.GetNativeArray(SystemAPI.GetEntityTypeHandle());
            var chunkOwners = chunk.GetSharedComponentManaged(SystemAPI.GetSharedComponentTypeHandle<ChunkOwner>(), state.EntityManager);
            for (int i = 0; i < chunk.Count; i++)
            {
                if (!highDetailNodes.Contains(chunkOwners.NodeIndex))
                {
                    ecb.RemoveComponent<HighDetail>(entities[i]);
                }
            }
        }

        ecb.Playback(state.EntityManager);
    }
}