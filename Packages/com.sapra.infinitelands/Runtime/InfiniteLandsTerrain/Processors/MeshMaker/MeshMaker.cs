using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Pool;

namespace sapra.InfiniteLands.MeshProcess{
    [ExecuteAlways]
    public class MeshMaker : ChunkProcessor<ChunkData>, IGenerate<MeshResult>
    {
        public Action<MeshResult> onProcessDone{get;set;}
        public Action<MeshResult> onProcessRemoved{get;set;}
        public enum MeshType
        {
            Normal,
            Decimated
        };
        public bool DecimatedForNonCloseLOD;
        [ShowIf(nameof(DecimatedForNonCloseLOD))][Min(1)] public int DecimatedLOD = 2;
        public MeshType meshType = MeshType.Normal;
        [ShowIf(nameof(isDecimated))][Min(1)] public int CoreGridSpacing = 6;
        [ShowIf(nameof(isDecimated))][Range(0,1)]public float NormalReduceThreshold = 0.5f;

        //public int coreGridSpacing => Mathf.CeilToInt(Resolution / (float)Mathf.CeilToInt(Resolution / (float)CoreGridSpacing));
        private bool isDecimated => meshType == MeshType.Decimated;

        [Min(1)] public int MaxMeshesPerFrame = 1;
        [Min(-1)] public int MaxLODWithColliders = 0;

        private ObjectPool<Mesh> meshPool;
        private Dictionary<Vector3Int, MeshResult> meshResults = new();
        private List<MeshProcess> chunksToProcess = new();
        private List<MeshGenerationData> meshGenerationCalls = new();
        private List<MeshResult> physicsToProcess = new();
        private List<PhysicsResult> physicsCalls = new();
        private HashSet<Vector3Int> _removeAfterGeneration = new();

        public IReadOnlyDictionary<Vector3Int, MeshResult> GetMeshResults => meshResults;
        public IReadOnlyList<MeshProcess> GetChunksToProcess => chunksToProcess;
        public IReadOnlyList<MeshResult> GetPhysicsToProcess => physicsToProcess;
        public IReadOnlyList<MeshGenerationData> GetMeshGenerationCalls => meshGenerationCalls;
        public IReadOnlyList<PhysicsResult> GetPhysicsCalls => physicsCalls;

        public bool ApplicationPlaying{get; set;}
        
        protected override void InitializeProcessor()
        {
            if(meshPool == null){
                meshPool = new ObjectPool<Mesh>(
                    MeshBuilder.CreateMesh, 
                    actionOnGet: MeshBuilder.ReuseMesh, 
                    actionOnDestroy: AdaptiveDestroy);
            }

            ApplicationPlaying = Application.isPlaying;
        }

        protected override void OnProcessAdded(ChunkData chunk) => AddChunk(chunk);
        protected override void OnProcessRemoved(ChunkData chunk) => RemoveChunk(chunk);

        public void AddChunk(ChunkData chunk){
            chunk.AddProcessor(this);

            var ID = chunk.ID;
            MeshType target = ID.z >= DecimatedLOD ? MeshType.Decimated : meshType;
            target = DecimatedForNonCloseLOD ? target : meshType;
            MeshProcess process = new MeshProcess(chunk, target, CoreGridSpacing, NormalReduceThreshold);
            chunksToProcess.Add(process);
            if(infiniteLands.InstantProcessors)
                UpdateRequests(true);
        }
        public void RemoveChunk(ChunkData chunk)
        {
            RemoveAfterGeneration(chunk.ID);
            for(int i = chunksToProcess.Count-1; i >= 0; i--){
                var chunkToProcess = chunksToProcess[i];
                var terrainConfig = chunkToProcess.terrainConfiguration;
                if(!terrainConfig.ID.Equals(chunk.ID))
                    continue;
                chunk.RemoveProcessor(this);
                chunksToProcess.RemoveAt(i);
            }

            
            for(int i = physicsToProcess.Count-1; i >= 0; i--){
                var physicsToProcess = this.physicsToProcess[i];
                if(!physicsToProcess.ID.Equals(chunk.ID))
                    continue;

                meshPool.Release(physicsToProcess.mesh);
                this.physicsToProcess.RemoveAt(i);
            }

            if(meshResults.TryGetValue(chunk.ID, out MeshResult finalMesh)){
                onProcessRemoved?.Invoke(finalMesh);
                meshPool.Release(finalMesh.mesh);
                meshResults.Remove(chunk.ID);
            }
        }

        private void RemoveAfterGeneration(Vector3Int ID){
            bool storedToRemove = false;
            foreach(var meshGenerationCall in meshGenerationCalls){
                foreach(var generatedChunk in meshGenerationCall.generatedChunks){
                    if(generatedChunk.terrainConfiguration.ID.Equals(ID)){
                        _removeAfterGeneration.Add(ID);
                        storedToRemove = true;
                        break;
                    }
                }
                if(storedToRemove)
                    break;
            }

            if(!storedToRemove){
                foreach(var physicsCall in physicsCalls){
                    foreach(var physics in physicsCall.results){
                        if(physics.ID.Equals(ID)){
                            _removeAfterGeneration.Add(ID);
                            storedToRemove = true;
                            break;
                        }
                    }
                    if(storedToRemove)
                        break;
                }
            }
        }

        public override void Update(){
            UpdateRequests(false);
        }

        public void UpdateRequests(bool instantGeneration){
            MeshSchedule();
            Consolidate(instantGeneration);
            PhysicsSchedule();
            PhysicsConsolidate(instantGeneration);
        }

        void MeshSchedule(){
            if(chunksToProcess.Count <= 0)
                return;
                
            List<MeshProcess> subset = ListPoolLight<MeshProcess>.Get();
            var maxCount = Mathf.Min(chunksToProcess.Count, MaxMeshesPerFrame);
            for(int i = 0; i < maxCount; i++){
                subset.Add(chunksToProcess[i]);
            }
            MeshGenerationData _meshData = MeshBuilder.ScheduleParallel(subset);
            meshGenerationCalls.Add(_meshData);
            chunksToProcess.RemoveRange(0, subset.Count);
        }

        void Consolidate(bool instantGeneration){
            if(meshGenerationCalls.Count <= 0)
                return;

            for(int d = meshGenerationCalls.Count-1; d >= 0; d--){
                MeshGenerationData _meshData = meshGenerationCalls[d];
                if(_meshData.handle.IsCompleted || instantGeneration){
                    _meshData.handle.Complete();
                    var toConsolidate = ListPoolLight<Mesh>.Get();
                    for(int i = 0; i < _meshData.meshDataArray.Length; i++){
                        toConsolidate.Add(meshPool.Get());
                    }
                    MeshBuilder.Consolidate(_meshData, toConsolidate);
                    for(int m = 0; m < toConsolidate.Count; m++){
                        MeshProcess meshProcess = _meshData.generatedChunks[m];
                        MeshResult finalMesh = new MeshResult(meshProcess.terrainConfiguration.ID, toConsolidate[m], false);
                        if(meshProcess.terrainConfiguration.ID.z <= MaxLODWithColliders && ApplicationPlaying){
                            physicsToProcess.Add(finalMesh);
                        }
                        else
                            InformMeshCreated(finalMesh);
                        meshProcess.chunkData.RemoveProcessor(this);
                    }
                    ListPoolLight<Mesh>.Release(toConsolidate);
                    ListPoolLight<MeshProcess>.Release(_meshData.generatedChunks);
                    meshGenerationCalls.RemoveAt(d);
                }
            }
        }

        void PhysicsSchedule(){
            if(physicsToProcess.Count <= 0)
                return;

            NativeArray<int> meshIDs = new NativeArray<int>(physicsToProcess.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var referenceMeshResults = ListPoolLight<MeshResult>.Get();
            referenceMeshResults.Clear();
            for(int i = 0; i < physicsToProcess.Count; i++){
                var previous = physicsToProcess[i];
                meshIDs[i] = physicsToProcess[i].mesh.GetInstanceID();
                referenceMeshResults.Add(previous);
            }
            JobHandle processPhysics = CookPhysicsJob.ScheduleParallel(meshIDs, default);
            PhysicsResult result = new PhysicsResult(meshIDs, referenceMeshResults, processPhysics);
            meshIDs.Dispose(processPhysics);
            physicsCalls.Add(result);
            physicsToProcess.Clear();
        }

        void PhysicsConsolidate(bool instantGeneration){
            if(physicsCalls.Count <= 0)
                return;

            for(int d = physicsCalls.Count-1; d >=0; d--){
                PhysicsResult _meshData = physicsCalls[d];
                if(_meshData.handle.IsCompleted || instantGeneration){
                    _meshData.handle.Complete();

                    for(int m = 0; m < _meshData.results.Count; m++){
                        MeshResult finalMesh = new MeshResult(_meshData.results[m].ID, _meshData.results[m].mesh, true);
                        InformMeshCreated(finalMesh);
                    }
                    ListPoolLight<MeshResult>.Release(_meshData.results);
                    physicsCalls.RemoveAt(d);
                }
            }
        }
        private void InformMeshCreated(MeshResult finalMesh){
            if(_removeAfterGeneration.Contains(finalMesh.ID)){
                meshPool.Release(finalMesh.mesh);
                _removeAfterGeneration.Remove(finalMesh.ID);
            }
            else{
                meshResults.TryAdd(finalMesh.ID, finalMesh);
                onProcessDone?.Invoke(finalMesh);
            }
        }

        protected override void DisableProcessor()
        {   
            foreach(var result in physicsToProcess){
                meshPool.Release(result.mesh);
            }

            foreach(var meshProcess in chunksToProcess){
                meshProcess.chunkData.RemoveProcessor(this);            
            }

            foreach(var result in physicsCalls){
                result.handle.Complete();
                foreach(var meshes in result.results){
                    meshPool.Release(meshes.mesh);
                }           
            }

            foreach(var result in meshGenerationCalls){
                result.handle.Complete();    
                foreach(var meshProcess in result.generatedChunks){
                    meshProcess.chunkData.RemoveProcessor(this);                
                }
            }

            foreach(var result in meshResults){
                meshPool.Release(result.Value.mesh);
            }

            meshResults.Clear();
            physicsToProcess.Clear();
            chunksToProcess.Clear();
            meshGenerationCalls.Clear();
            physicsCalls.Clear();
            _removeAfterGeneration.Clear();

            if(meshPool != null){    
                if(meshPool.CountActive > 0)
                    Debug.LogWarning("Not all meshes have been returned");
                meshPool.Dispose();
            }
        }
    }
}