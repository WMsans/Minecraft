using UnityEngine;

namespace sapra.InfiniteLands{
    public class QuadLayout : ILayoutChunks
    {
        public InfiniteSettings GetInfiniteSettings(MeshSettings userData, float ViewDistance)
        {
            int lodLevels = Mathf.Max(Mathf.CeilToInt(Mathf.Log(ViewDistance / userData.MeshScale, 2)), 0) + 1;
            float MaxScale = Mathf.Pow(2, lodLevels-1)*userData.MeshScale;
            int VisibleChunks = Mathf.CeilToInt(ViewDistance / MaxScale);
            return new InfiniteSettings(lodLevels, VisibleChunks);
        }
        public MeshSettings GetMeshSettingsFromID(MeshSettings userData, Vector3Int ID)
        {
            MeshSettings selected = userData;
            selected.MeshScale = Mathf.Pow(2, ID.z)*userData.MeshScale;
            return selected;
        }

        public Vector3Int TransformPositionToID(Vector2 gridPosition, int lod, Vector2 gridOffset, float MeshScale)
        {
            float MeshSize = Mathf.Pow(2, lod) * MeshScale;
            Vector2Int flat = Vector2Int.FloorToInt((gridPosition+gridOffset)/MeshSize);
            return new Vector3Int(flat.x, flat.y, lod);
        }
    }
}