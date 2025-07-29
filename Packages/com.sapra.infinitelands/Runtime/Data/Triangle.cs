using UnityEngine;

namespace sapra.InfiniteLands{
    public struct Triangle{
        public Vector3 C1;
        public Vector3 C2;
        public Vector3 C3;
        public Triangle(Vector3 c1, Vector3 c2, Vector3 c3) { 
            C1 = c1;
            C2 = c2;
            C3 = c3;
        }
    }
}