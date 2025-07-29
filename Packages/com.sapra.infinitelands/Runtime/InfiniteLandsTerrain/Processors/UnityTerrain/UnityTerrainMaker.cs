using UnityEngine;
using System.Linq;
using Unity.Collections;
using System;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace sapra.InfiniteLands.UnityTerrain{
    [ExecuteAlways]
    public class UnityTerrainMaker : ChunkProcessor<ChunkData>, IGenerate<UnityTerHeights>
    {
        public Action<UnityTerHeights> onProcessDone { get; set; }
        public Action<UnityTerHeights> onProcessRemoved { get; set; }

        private Dictionary<Vector3Int, UnityTerHeights> CompletedRequests = new();
        
        public HeightmapResolution TerrainResolution = HeightmapResolution._257x257;
        protected override void OnProcessAdded(ChunkData chunk)
        {
            //Create terrain
            var minMaxHeight = chunk.GlobalMinMax;
            var finalPositions = chunk.DisplacedVertexPositions;

            int resolution = (int)TerrainResolution;
            float[,] heights = new float[resolution,resolution];
            for(int x = 0; x < resolution; x++){
                for (int y = 0; y < resolution; y++)
                {
                    float height = Remap(chunk.meshSettings.Resolution + 1, resolution, y, x, finalPositions, 0);
                    heights[x, y] = Mathf.InverseLerp(minMaxHeight.x, minMaxHeight.y, height);
                }
            }
 
            UnityTerHeights result = new UnityTerHeights(chunk,
                new Vector3(chunk.meshSettings.MeshScale, minMaxHeight.y - minMaxHeight.x, chunk.meshSettings.MeshScale),
                (int)TerrainResolution,
                heights);
            CompletedRequests.TryAdd(chunk.ID, result);
            onProcessDone?.Invoke(result);
        }

        private float Remap(int currentResolution, int targetResolution, int xTarget, int yTarget, NativeArray<Vertex> map, int offset){
            float factor = (float)(currentResolution)/(float)(targetResolution);
            float ogX = xTarget*factor;
            float ogY = yTarget*factor;

            if(xTarget == 0)
                ogX = 0;
            else if(xTarget == targetResolution-1)
                ogX = currentResolution-1;
            if(yTarget == 0)
                ogY = 0;
            else if(yTarget == targetResolution-1)
                ogY = currentResolution-1;

            int btmX = Mathf.Min(Mathf.FloorToInt(ogX), currentResolution);
            int btmY = Mathf.Min(Mathf.FloorToInt(ogY), currentResolution);
            int topX = Mathf.Min(Mathf.CeilToInt(ogX), currentResolution);
            int topY = Mathf.Min(Mathf.CeilToInt(ogY), currentResolution);
            
            int maxLength = MapTools.LengthFromResolution(currentResolution-1)-1;
            int indBtmX = Mathf.Min(maxLength, btmX+btmY*(currentResolution));
            int indBtmY = Mathf.Min(maxLength, btmX+topY*(currentResolution));
            int indTopX = Mathf.Min(maxLength, topX+btmY*(currentResolution));
            int indTopY = Mathf.Min(maxLength, topX+topY*(currentResolution));

            float leftDown = map[indBtmX].position.y;
            float leftUp = map[indBtmY].position.y;
            float rightDown = map[indTopX].position.y;
            float righTUp = map[indTopY].position.y;

            float fcX = ogX-btmX;
            float fcy = ogY-btmY;
            
            float interBotm = Mathf.Lerp(leftDown, rightDown, fcX);
            float interUp = Mathf.Lerp(leftUp, righTUp, fcX);
            return Mathf.Lerp(interBotm, interUp, fcy);
        }
        protected override void OnProcessRemoved(ChunkData chunk)
        {
            if(CompletedRequests.TryGetValue(chunk.ID, out var result)){
                CompletedRequests.Remove(chunk.ID);
            }
        }

        protected override void InitializeProcessor()
        {
        }

        protected override void DisableProcessor()
        {
        }
    }
}