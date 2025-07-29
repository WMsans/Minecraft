using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public static class ControlerExtensions
    {
        public static Vector3 WorldToLocalPoint(this IControlTerrain controlTerrain, Vector3 position) => controlTerrain.worldToLocalMatrix.MultiplyPoint(position);
        public static Vector3 LocalToWorldPoint(this IControlTerrain controlTerrain, Vector3 position) => controlTerrain.localToWorldMatrix.MultiplyPoint(position);

        public static Vector3 LocalToWorldVector(this IControlTerrain controlTerrain, Vector3 vector) => controlTerrain.localToWorldMatrix.MultiplyVector(vector);
        public static Vector3 WorldToLocalVector(this IControlTerrain controlTerrain, Vector3 vector) => controlTerrain.worldToLocalMatrix.MultiplyVector(vector);

        public static bool IsPointInChunk(this IControlTerrain controlTerrain, Vector3 position, TerrainConfiguration terrainData)
        {
            Vector3 flattened = controlTerrain.worldToLocalMatrix.MultiplyPoint(position);
            return controlTerrain.IsPointInChunkAtGrid(new Vector2(flattened.x, flattened.z), terrainData);
        }
        public static bool IsPointInChunkAtGrid(this IControlTerrain controlTerrain, Vector2 flatPosition, TerrainConfiguration terrainData)
        {
            var internalGridOffset = MapTools.GetOffsetInGrid(controlTerrain.localGridOffset, controlTerrain.meshSettings.MeshScale) + controlTerrain.meshSettings.MeshScale / 2.0f;
            Vector3Int pointInGrid = controlTerrain.GetChunkLayout().TransformPositionToID(flatPosition, terrainData.ID.z, internalGridOffset, controlTerrain.meshSettings.MeshScale);
            return terrainData.ID == pointInGrid;
        }

        public static bool TryGetChunkDataAtGridPosition<Z>(this IControlTerrain controlTerrain, Vector2 localFlatPosition, Dictionary<Vector3Int, Z> searchIn, out Z data)
        {
            var internalGridOffset = MapTools.GetOffsetInGrid(controlTerrain.localGridOffset, controlTerrain.meshSettings.MeshScale) + controlTerrain.meshSettings.MeshScale / 2.0f;
            var chunkLayout = controlTerrain.GetChunkLayout();
            for (int i = 0; i <= controlTerrain.maxLodGenerated; i++)
            {
                Vector3Int id = chunkLayout.TransformPositionToID(localFlatPosition, i, internalGridOffset, controlTerrain.meshSettings.MeshScale);
                if (searchIn.TryGetValue(id, out data))
                {
                    return true;
                }
            }
            data = default;
            return false;
        }
        public static bool GetChunkDataAtPosition<Z>(this IControlTerrain controlTerrain, Vector3 position, Dictionary<Vector3Int, Z> searchIn, out Z data)
        {
            Vector3 flattened = controlTerrain.worldToLocalMatrix.MultiplyPoint(position);
            return controlTerrain.TryGetChunkDataAtGridPosition(new Vector2(flattened.x, flattened.z), searchIn, out data);
        }
    }
}