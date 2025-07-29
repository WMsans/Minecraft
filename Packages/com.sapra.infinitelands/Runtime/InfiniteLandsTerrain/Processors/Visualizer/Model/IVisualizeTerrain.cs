using UnityEngine;
using System;

namespace sapra.InfiniteLands
{
    public interface IVisualizeTerrain : IGenerate<ChunkData>
    {
        public void StartGeneration();
        public void RequestMesh(TerrainConfiguration config);
        public void UnrequestMesh(TerrainConfiguration config);
        public void DisableChunk(IRenderChunk chunk);
        public IRenderChunk GenerateChunk(Vector3Int ID);
        public int MaxLODGenerated { get; }
        public bool DrawGizmos { get; }
        public bool CanTriggerAutoUpdate { get; }
        public Vector2 localGridOffset { get; }
        public Matrix4x4 localToWorldMatrix { get; }
        public Matrix4x4 worldToLocalMatrix { get; }
        public bool InstantProcessors{ get; }
    }
}