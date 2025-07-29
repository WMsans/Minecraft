using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class ChunkData
    {
        public Vector3Int ID{get; private set;}
        public TerrainConfiguration terrainConfig{get; private set;}
        public MeshSettings meshSettings{get; private set;}
        
        public Bounds ObjectSpaceBounds{get; private set;}
        public Bounds WorldSpaceBounds{get; private set;}

        public NativeArray<Vertex> DisplacedVertexPositions;
        public Vector2 ChunkMinMax;
        public Vector2 GlobalMinMax;

        private TreeData treeData;
        private TreeData separateExtraTree;
        public WorldGenerator worldGenerator;
        private List<object> processors = new();
        public bool isValid => processors.Count <= 0;
        
        public void AddProcessor(object processor)
        {
#if UNITY_EDITOR
            if (processors.Contains(processor))
                Debug.LogWarningFormat("{0} already exists!! Adding a duplicate", processor);
#endif
            processors.Add(processor);
        }

        public void RemoveProcessor(object processor){
            #if UNITY_EDITOR
            if (!processors.Contains(processor))
                Debug.LogWarningFormat("{0} doesn't exist!! Duplicate call", processor);
            #endif
            processors.Remove(processor);
            if(processors.Count <= 0 ){
                Return();
                processors.Clear();
            }
        }

        public void CompletedInvocations()
        {
            RemoveProcessor(this);
        }


        private void Return()
        {
            if (treeData != null)
            {
                treeData.CloseTree();
                treeData = null;
            }
            if (separateExtraTree != null)
            {
                separateExtraTree.CloseTree();
                separateExtraTree = null;
            }
        }

        public TreeData GetMainTree() => treeData;
        public TreeData GetVariantTree() => separateExtraTree != null ? separateExtraTree : treeData;

        public void Reuse(TerrainConfiguration _terrainConfig, MeshSettings _meshSettings,
            NativeArray<Vertex> DisplacedVertexPositions,
            NativeArray<float> chunkMinMax, Vector2 globalMinMax,
            WorldGenerator worldGenerator, TreeData treeData, TreeData separateExtraTree)
        {
            this.treeData = treeData;
            this.worldGenerator = worldGenerator;
            this.separateExtraTree = separateExtraTree;
            this.DisplacedVertexPositions = DisplacedVertexPositions;

            this.ID = _terrainConfig.ID;
            this.terrainConfig = _terrainConfig;
            this.meshSettings = _meshSettings;

            float MinValue = chunkMinMax[0];
            float MaxValue = chunkMinMax[1];
            ChunkMinMax = new Vector2(MinValue, MaxValue);
            GlobalMinMax = globalMinMax;

            float verticalOffset = (MaxValue + MinValue) / 2f;
            float displacement = MaxValue - MinValue;
            WorldSpaceBounds = new Bounds(terrainConfig.Position + verticalOffset * Vector3.up, new Vector3(_meshSettings.MeshScale, displacement, _meshSettings.MeshScale));
            ObjectSpaceBounds = new Bounds(verticalOffset * Vector3.up, new Vector3(_meshSettings.MeshScale, displacement, _meshSettings.MeshScale));

            AddProcessor(this);
        }
    }
}