using UnityEngine;

namespace sapra.InfiniteLands{
    [System.Serializable]
    public class GraphSettings : ScriptableObject
    {
        [Min(100)] public float MeshScale = 1000;
        public int Resolution;
        public Vector2 WorldOffset = Vector2.zero;
        public int Seed = 0;
    }
}