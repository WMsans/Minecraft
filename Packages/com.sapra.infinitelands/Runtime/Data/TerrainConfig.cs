using UnityEngine;

namespace sapra.InfiniteLands
{
    [System.Serializable]
    public struct TerrainConfiguration
    {
        public Vector3Int ID;
        public Vector3 Position;
        public Vector3 TerrainNormal;
                
        public TerrainConfiguration(TerrainConfiguration og)
        {
            this.ID = og.ID;
            this.Position = og.Position;
            this.TerrainNormal = og.TerrainNormal;
        }
        public TerrainConfiguration(Vector3Int _id, Vector3 worldNormal, Vector3 flatPosition)
        {
            this.ID = _id;
            this.Position = flatPosition;
            this.TerrainNormal = worldNormal;
        }

        public TerrainConfiguration(Vector3Int _id, Vector3 worldNormal, float _MeshScale)
        {
            this.ID = _id;
            this.Position = new Vector3(ID.x + .5f, 0, ID.y + .5f) * _MeshScale;
            this.TerrainNormal = worldNormal;
        }
    }
}