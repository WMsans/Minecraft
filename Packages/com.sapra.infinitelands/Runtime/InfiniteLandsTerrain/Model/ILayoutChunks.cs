using UnityEngine;

namespace sapra.InfiniteLands{
    public interface ILayoutChunks
    {
        public InfiniteSettings GetInfiniteSettings(MeshSettings userData, float ViewDistance);
        public MeshSettings GetMeshSettingsFromID(MeshSettings userData, Vector3Int ID);
        public Vector3Int TransformPositionToID(Vector2 gridPosition, int lod, Vector2 gridOffset, float MeshScale);
    }
}