using UnityEngine;
namespace sapra.InfiniteLands.MeshProcess{
    public readonly struct MeshResult {
        public readonly Vector3Int ID;
        public readonly Mesh mesh;
        public readonly bool PhysicsBaked;
        public MeshResult(Vector3Int ID, Mesh mesh, bool PhysicsBaked){
            this.ID = ID;
            this.PhysicsBaked = PhysicsBaked;
            this.mesh = mesh;
        }
    }
}