using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [ExecuteInEditMode]
    public class PointStore : ChunkProcessor<ChunkData>, IGenerate<CoordinateResult>
    {
        [Header("Base configuration")] 
        private Dictionary<Vector3Int, CoordinateResult> _chunksGenerated = new();

        public Action<CoordinateResult> onProcessDone { get; set; }
        public Action<CoordinateResult> onProcessRemoved { get; set; }

        private List<CoordianteProcess> Processing = new();

        protected override void DisableProcessor()
        {
            foreach (KeyValuePair<Vector3Int, CoordinateResult> values in _chunksGenerated)
            {
                values.Value.Return();
            }
            foreach (CoordianteProcess proces in Processing)
            {
                proces.Cancel();
            }
            _chunksGenerated.Clear();
            Processing.Clear();
        }

        protected override void InitializeProcessor()
        {
            _chunksGenerated = new();
            Processing = new();
        }
        protected override void OnProcessRemoved(ChunkData chunk) => RemoveChunk(chunk);
        protected override void OnProcessAdded(ChunkData chunk) => AddChunk(chunk);
        public void RemoveChunk(ChunkData chunk)
        {
            Vector3Int finalCord = chunk.ID;
            if(_chunksGenerated.TryGetValue(finalCord, out CoordinateResult data)){
                data.Return();
                onProcessRemoved?.Invoke(data);
                _chunksGenerated.Remove(finalCord);
            }
        }

        public void AddChunk(ChunkData chunk){
            ReturnableManager manager = chunk.worldGenerator.returnableManager;
            CoordianteProcess process = new CoordianteProcess(manager, chunk.GlobalMinMax, chunk.DisplacedVertexPositions, chunk.meshSettings, chunk.terrainConfig);
            Processing.Add(process);
        }

        public override void Update()
        {
            if (Processing.Count > 0)
            {
                for (int i = Processing.Count - 1; i >= 0; i--)
                {
                    var process = Processing[i];
                    if (process.jobHandle.IsCompleted)
                    {
                        var result = process.Complete();
                        if (!_chunksGenerated.TryAdd(result.terrainConfiguration.ID, result))
                        {
                            Debug.LogWarning("Can't add new chunk " + result.terrainConfiguration.ID.ToString());
                        }
                        onProcessDone?.Invoke(result);
                        Processing.RemoveAt(i);
                    }
                }
            }
        }

        #region APIs
        /// <summary>
        /// Get all the CoordinateDataHolder of a chunk in a grid position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool GetHolderAtGrid(Vector2 position, out CoordinateResult result)
        {
            return infiniteLands.TryGetChunkDataAtGridPosition(position, _chunksGenerated, out result);
        }

        /// <summary>
        /// Get all the CoordinateDataHolder of a chunk in a world position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool GetHolderAt(Vector3 position, out CoordinateResult result)
        {
            Vector3 flattened = infiniteLands.WorldToLocalPoint(position);
            return GetHolderAtGrid(new Vector2(flattened.x, flattened.z), out result);
        }

        /// <summary>
        /// Get a single CoordinateData in a specific grid position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="foundData"></param>
        /// <param name="interpolated"></param>
        /// <returns></returns>
        public bool GetCoordinateDataAtGrid(Vector2 position, out CoordinateData foundData, bool interpolated)
        {
            if (GetHolderAtGrid(position, out CoordinateResult coordinateDataHolder))
            {
                foundData = coordinateDataHolder.GetCoordinateDataAtGrid(position, interpolated);
                return true;
            }
            foundData = default;
            return false;
        }

        /// <summary>
        /// Get a single CoordinateData in a specific world position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="foundData"></param>
        /// <param name="interpolated"></param>
        /// <param name="inWorldSpace"></param>
        /// <returns></returns>
        public bool GetCoordinateDataAt(Vector3 position, out CoordinateData foundData, bool interpolated = false, bool inWorldSpace = true)
        {
            if(infiniteLands==null){
                foundData = default;
                return false;
            }
            Vector3 flattened = infiniteLands.WorldToLocalPoint(position);
            if (GetCoordinateDataAtGrid(new Vector2(flattened.x, flattened.z), out foundData, interpolated))
            {
                if(inWorldSpace)
                    foundData = foundData.ApplyMatrix(infiniteLands.localToWorldMatrix);
                return true;
            }

            return false;
        }

        #endregion
    }
}