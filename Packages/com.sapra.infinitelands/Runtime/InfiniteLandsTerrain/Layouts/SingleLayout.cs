using UnityEngine;

namespace sapra.InfiniteLands{
    public class SingleLayout : ILayoutChunks
    {
        public InfiniteSettings GetInfiniteSettings(MeshSettings userData, float ViewDistance)
        {
            int lodLevels = 1;
            int VisibleChunks = Mathf.CeilToInt(ViewDistance / userData.MeshScale);
            return new InfiniteSettings(lodLevels, VisibleChunks);
        }
        
        public Vector3Int TransformPositionToID(Vector2 gridPosition, int lod, Vector2 gridOffset, float MeshScale)
        {
            Vector2Int flat = Vector2Int.FloorToInt((gridPosition+gridOffset)/MeshScale);
            return new Vector3Int(flat.x, flat.y, lod);
        }

        public MeshSettings GetMeshSettingsFromID(MeshSettings userData, Vector3Int ID)
        {
            return userData;
        }
    }
}