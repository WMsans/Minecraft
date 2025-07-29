using NUnit.Framework;
using UnityEngine;
using Unity.Mathematics;
using System.Reflection;
using sapra.InfiniteLands.Tests;

namespace sapra.InfiniteLands.MeshProcess.Tests
{
    public class MeshMakerTests
    {
        private MeshMaker _meshMaker;
        private ReturnableManager manager;

        [SetUp]
        public void Setup()
        {
            _meshMaker = new MeshMaker();
            manager = new ReturnableManager(new StringObjectStore<object>());
            var mockTerrain = new MockTerrainControl();
            _meshMaker.Initialize(mockTerrain);
            _meshMaker.ApplicationPlaying = true;
        }

        [TearDown]
        public void Teardown()
        {
            _meshMaker.Disable();
            //manager.Dispose(default);
        }

        private static float[] vertPos = {
            0, float.MinValue, float.MaxValue, float.NaN,
        };
        private static int[] resolutions = {
            0, 10, int.MinValue, 1000,
        };
        [Test]
        public void MeshSchedule_AddsChunksToProcess([ValueSource(nameof(vertPos))]float position, [ValueSource(nameof(resolutions))]int resolution)
        {
            // Arrange
            MeshSettings settings = new MeshSettings(){Resolution = resolution};
            var chunk1 = new ChunkData();
            var worldData1 = GetWorldFinalData(manager, new Vector2Int(0,1), settings.Resolution, position);
            chunk1.Reuse(default, settings, worldData1.FinalPositions, worldData1.ChunkMinMax, worldData1.GlobalMinMax,default, default, default);

            var chunk2 = new ChunkData();
            var worldData2 = GetWorldFinalData(manager, new Vector2Int(0,1), settings.Resolution, position);
            chunk2.Reuse(default, settings, worldData2.FinalPositions, worldData2.ChunkMinMax, worldData2.GlobalMinMax, default, default, default);

            _meshMaker.MaxMeshesPerFrame = 2;
            _meshMaker.AddChunk(chunk1);
            _meshMaker.AddChunk(chunk2);

            MethodInfo method = _meshMaker.GetType().GetMethod("MeshSchedule", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(_meshMaker, new object[] {  });

            // Assert
            Assert.AreEqual(0, _meshMaker.GetChunksToProcess.Count); // All chunks scheduled
            Assert.AreEqual(1, _meshMaker.GetMeshGenerationCalls.Count); // Check one generation call
            
            _meshMaker.UpdateRequests(true);
        }

        private WorldData GetWorldFinalData(ReturnableManager manager, Vector2Int minMaxValue, int Resolution, float valToAssign) {
            Resolution = Mathf.Max(Resolution, 1);
            ReturnablePack holdReturnableData = new ReturnablePack();
            var values = manager.GetData<Vertex>(holdReturnableData, MapTools.LengthFromResolution(Resolution));
            var minmax = manager.GetData<float>(holdReturnableData, 2);
            minmax[0] = minMaxValue.x;
            minmax[1] = minMaxValue.y;

            for (int x = 0; x <= Resolution; x++) {
                for (int y = 0; y <= Resolution; y++)
                {
                    int index = MapTools.VectorToIndex(new int2(x, y), Resolution);
                    values[index] = new Vertex()
                    {
                        position = new float3(x, valToAssign, y),
                        normal = Vector3.up
                    };
                }
            }
            
            return new WorldData(minmax, values, minMaxValue, default);
        }

        [Test]
        public void Consolidate_ProcessesGeneratedMeshes([ValueSource(nameof(vertPos))]float position, [ValueSource(nameof(resolutions))]int resolution)
        {
            // Arrange
            MeshSettings settings = new MeshSettings(){Resolution = resolution};

            var chunk = new ChunkData();

            var worldData1 = GetWorldFinalData(manager, new Vector2Int(0,1), settings.Resolution, position);
            chunk.Reuse(default, settings, worldData1.FinalPositions, worldData1.ChunkMinMax, worldData1.GlobalMinMax,default, default, default);

            
            _meshMaker.AddChunk(chunk);
            _meshMaker.UpdateRequests(true);

            // Act
            MethodInfo method = _meshMaker.GetType().GetMethod("Consolidate",  BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(_meshMaker, new object[] { true });

            // Assert
            Assert.AreEqual(0, _meshMaker.GetMeshGenerationCalls.Count);
            Assert.IsTrue(_meshMaker.GetMeshResults.ContainsKey(chunk.terrainConfig.ID));
        }


        [Test]
        public void PhysicsConsolidate_ProcessesPhysicsResults([ValueSource(nameof(vertPos))]float position, [ValueSource(nameof(resolutions))]int resolution)
        {
            // Arrange
            MeshSettings settings = new MeshSettings(){Resolution = resolution};
            var chunk1 = new ChunkData();
            var worldData1 = GetWorldFinalData(manager, new Vector2Int(0,1), settings.Resolution, position);
            chunk1.Reuse(new TerrainConfiguration(){ID = new Vector3Int(0,0,0)}, settings, worldData1.FinalPositions, worldData1.ChunkMinMax, worldData1.GlobalMinMax, default, default, default);


            var chunk2 = new ChunkData();
            var worldData2 = GetWorldFinalData(manager, new Vector2Int(0,1), settings.Resolution, position);
            chunk2.Reuse(new TerrainConfiguration(){ID = new Vector3Int(0,0,0)}, settings, worldData2.FinalPositions, worldData2.ChunkMinMax, worldData2.GlobalMinMax, default,default, default);

            _meshMaker.MaxMeshesPerFrame = 2;
            _meshMaker.MaxLODWithColliders = 0;

            MeshResult result1 = default;
            MeshResult result2 = default;
            _meshMaker.onProcessDone += (MeshResult res)=>{
                if(res.ID.z == 0)
                    result1 = res;
                else
                    result2 = res;
            };

 
            _meshMaker.AddChunk(chunk1);
            _meshMaker.AddChunk(chunk2);

            _meshMaker.UpdateRequests(true);
   
            Assert.AreEqual(0, _meshMaker.GetPhysicsCalls.Count);
            Assert.IsNotNull(result1);
            Assert.IsTrue(result1.PhysicsBaked);

            Assert.IsNotNull(result2);
            Assert.IsFalse(result2.PhysicsBaked);
        }
    }
}