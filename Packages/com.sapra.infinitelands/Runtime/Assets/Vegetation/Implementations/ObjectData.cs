using UnityEngine;
using static sapra.InfiniteLands.IHoldVegetation;

namespace sapra.InfiniteLands{
    public readonly struct ObjectData{
        public readonly bool SpawnsObject;
        public readonly GameObject gameObject;
        public readonly ColliderMode ColliderMode;
        public readonly float CollisionDistance;
        public ObjectData(bool SpawnsObject, GameObject gameObject, ColliderMode colliderMode, float CollisionDistance)
        {
            this.SpawnsObject = SpawnsObject;
            this.gameObject = gameObject;
            this.ColliderMode = colliderMode;
            this.CollisionDistance = CollisionDistance;
        }
    }
}