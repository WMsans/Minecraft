using UnityEngine;

namespace sapra.InfiniteLands{
    public struct PointGenerationSettings{
        public Vector2Int forChunkID;
        public float CheckupSize;
        public Vector3 Origin;
        public PointGenerationSettings(Vector2Int id, float grid, Vector3 origin){
            forChunkID = id;
            CheckupSize = grid;
            Origin = origin;
        }
    }
}