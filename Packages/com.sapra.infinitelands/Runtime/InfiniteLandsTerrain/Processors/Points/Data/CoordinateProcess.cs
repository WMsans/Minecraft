using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct CoordianteProcess
    {
        public MeshSettings meshSettings { get; private set; }
        public TerrainConfiguration terrainConfiguration { get; private set; }
        public Vector2 MinMaxHeight { get; private set; }

        public JobHandle jobHandle;
        public NativeArray<CoordinateData> points { get; private set; }
        private ReturnablePack pack;
        public CoordianteProcess(ReturnableManager returnableManager,
            Vector2 MinMaxHeight, NativeArray<Vertex> verticesPositions,
            MeshSettings meshSettings, TerrainConfiguration terrainConfiguration)
        {
            this.meshSettings = meshSettings;
            this.terrainConfiguration = terrainConfiguration;
            this.MinMaxHeight = MinMaxHeight;

            pack = GenericPoolLight<ReturnablePack>.Get();
            points = returnableManager.GetData<CoordinateData>(pack, verticesPositions.Length);
            jobHandle = CoordinateDataJob.ScheduleParallel(verticesPositions,
                points, terrainConfiguration.Position, tr(terrainConfiguration.ID), meshSettings.Resolution, default);
        }

        public void Cancel()
        {
            jobHandle.Complete();
            pack.Release();
        }

        private static int3 tr(Vector3Int id)
        {
            return new int3(id.x, id.y, id.z);
        }

        public CoordinateResult Complete()
        {
            jobHandle.Complete();
            return new CoordinateResult(pack, points, meshSettings, terrainConfiguration, MinMaxHeight);
        }
    }
}