using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public interface IRenderChunk
    {
        public GameObject gameObject { get; }
        public bool DataRequested { get; set; }
        public bool VisibilityCheck(List<Vector3> playerPosition, float GenerationDistance, bool parentDisabled);
        public void DisableChunk();
        public void EnableChunk(TerrainConfiguration config, MeshSettings meshSettings);
    }
}