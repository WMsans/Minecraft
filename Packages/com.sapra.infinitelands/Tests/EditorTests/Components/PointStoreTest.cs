/* using UnityEngine;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using sapra.InfiniteLands.MeshProcess.Tests;

namespace sapra.InfiniteLands.Tests
{
    public class PointStoreTest
    {    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        private static Vector3[] _positions = {
            new Vector3(0,0,0), 
            new Vector3(1010,0,0), 
            new Vector3(123123,-12312,23423),
        };
        private static Vector3[] _rotations = {
            new Vector3(0,0,0),
            new Vector3(0,0,45),
            new Vector3(79,0,-45),
            new Vector3(1233123,2123,-12312),
        };

        private static float[] _meshSizes = {
            10,1000,123.232f
        };

        private static Vector3Int[] _chunkIndex = {
            new Vector3Int(0,0,0), 
            new Vector3Int(1,1,0),
            new Vector3Int(1,-30,23),
            new Vector3Int(2,5,3),
            new Vector3Int(1231,-1233,4),
        };

        private PointStore _pointStore;
        private MockTerrainControl infiniteLandsTerrain;
        private ReturnableManager manager;
        [SetUp]
        public void Setup()
        {
            infiniteLandsTerrain = new MockTerrainControl();
            _pointStore = new PointStore();
            manager = new ReturnableManager(new StringObjectStore<object>());
        }

        [TearDown]
        public void Teardown()
        {
            _pointStore.Disable();
            Object.DestroyImmediate(_pointStore.gameObject);
        }
        
        [Test]
        public void StoreFindsChunksQuad(
            [ValueSource(nameof(_chunkIndex))]Vector3Int terrainIndex, 
            [ValueSource(nameof(_meshSizes))]float MeshSize,
            [ValueSource(nameof(_positions))]Vector3 currentOrigin,
            [ValueSource(nameof(_rotations))]Vector3 currentRotation)
        {
            infiniteLandsTerrain.SetMatrix(Matrix4x4.TRS(currentOrigin, Quaternion.Euler(currentRotation), Vector3.one));
            infiniteLandsTerrain.ChangeLayout(LandsLayout.QuadTree);

            var layout = infiniteLandsTerrain.GetChunkLayout();
            _pointStore.Initialize(infiniteLandsTerrain);

            StoreFindsChunks(terrainIndex, MeshSize, infiniteLandsTerrain, layout, _pointStore); 
        }

        [Test]
        public void StoreFindsChunksSingle(
            [ValueSource(nameof(_chunkIndex))]Vector3Int terrainIndex, 
            [ValueSource(nameof(_meshSizes))]float MeshSize,
            [ValueSource(nameof(_positions))]Vector3 currentOrigin,
            [ValueSource(nameof(_rotations))]Vector3 currentRotation)
        {
            infiniteLandsTerrain.SetMatrix(Matrix4x4.TRS(currentOrigin, Quaternion.Euler(currentRotation), Vector3.one));
            infiniteLandsTerrain.ChangeLayout(LandsLayout.SimpleGrid);
            
            var layout = infiniteLandsTerrain.GetChunkLayout();
            _pointStore.Initialize(infiniteLandsTerrain);

            StoreFindsChunks(terrainIndex, MeshSize, infiniteLandsTerrain, layout, _pointStore); 
        }

        public void StoreFindsChunks(Vector3Int terrainIndex, float MeshSize, MockTerrainControl il, ILayoutChunks layout, PointStore store)
        {
            MeshSettings settings = new MeshSettings(){
                MeshScale = MeshSize,
                Resolution = 100,
            };
            il.meshSettings = settings;
            il.maxLodGenerated = terrainIndex.z;
            
            MeshSettings chunkSettings = layout.GetMeshSettingsFromID(settings, terrainIndex);
            TerrainConfiguration config = new TerrainConfiguration(terrainIndex, Vector3.up, chunkSettings.MeshScale);
            il.localGridOffset = new Vector2(config.Position.x, config.Position.z);

            //var data = GenerateCoordinateData(settings.Resolution);
            TypeStore<ProcessableData> store1 = new();
            var tempData = new ChunkData();
            CoordinateDataHolder coordinateData = new CoordinateDataHolder(GenerateCoordinateData(settings.Resolution), chunkSettings, config);
            WorldFinalData finalData = GetWorldFinalData(manager, new Vector2Int(0, 1), chunkSettings.Resolution, 1);
            coordinateData.finalJob.Complete();


            store1.AddData(coordinateData);
            store1.AddData(finalData);

            tempData.Reuse(config, chunkSettings, default, store1, default);

            store.AddChunk(tempData);
            store.AddChunk(tempData);

            float unit = chunkSettings.MeshScale/(chunkSettings.Resolution+1);
            float half = chunkSettings.MeshScale/2.0f-unit;
            

            //Center
            AssertChunkRetrieve(config.Position, config, il, store, coordinateData,chunkSettings.Resolution/2,chunkSettings.Resolution/2);

            //Corners
            AssertChunkRetrieve(config.Position+new Vector3(-half, 0, -half), config, il, store, coordinateData,01, 01);
            AssertChunkRetrieve(config.Position+new Vector3(half, 0, half), config, il, store, coordinateData,chunkSettings.Resolution-1,chunkSettings.Resolution-1);
            AssertChunkRetrieve(config.Position+new Vector3(-half, 0, half), config, il, store, coordinateData,01,chunkSettings.Resolution-1);
            AssertChunkRetrieve(config.Position+new Vector3(half, 0, -half), config, il, store, coordinateData,chunkSettings.Resolution-1,01);

            store.RemoveChunk(tempData);
            //data.Dispose();
        }
        
        private WorldFinalData GetWorldFinalData(ReturnableManager manager, Vector2Int minMaxValue, int Resolution, float valToAssign){
            Resolution = Mathf.Max(Resolution, 1);
            ReturnablePack holdReturnableData = new ReturnablePack();
            var minmax = manager.GetData<float>(holdReturnableData, 2);
            minmax[0] = minMaxValue.x;
            minmax[1] = minMaxValue.y;
            WorldFinalData worldFinalData = new WorldFinalData();
            worldFinalData.ReuseData(holdReturnableData, minmax, default, minMaxValue, default);
            return worldFinalData;
        }
        
        public NativeArray<CoordinateData> GenerateCoordinateData(int resolution)
        {
            NativeArray<CoordinateData> data = new NativeArray<CoordinateData>((resolution + 1) * (resolution + 1), Allocator.TempJob);
            for (int x = 0; x <= resolution; x++)
            {
                for (int z = 0; z <= resolution; z++)
                {
                    CoordinateData newData = new CoordinateData()
                    {
                        position = new float3(x, x + z * resolution, z)
                    };
                    data[x + z * (resolution + 1)] = newData;
                }
            }
            return data;
        }

        private void AssertChunkRetrieve(Vector3 position, TerrainConfiguration config, IControlTerrain infiniteLands, PointStore store, CoordinateDataHolder og, int x, int z)
        {
            Debug.LogFormat("Looking at {0}", position);
            Vector3 tp = infiniteLands.LocalToWorldPoint(position);
            CoordinateDataHolder retrieve = store.GetHolderAt(tp);
            Assert.AreEqual(og, retrieve);
            Assert.AreEqual(retrieve.terrainConfiguration, config);

            CoordinateData data = store.GetCoordinateDataAt(tp, out bool found, false, false);
            Assert.IsTrue(found);

            Assert.AreEqual(x, data.position.x);
            Assert.AreEqual(z, data.position.z);
        }
    }
} */