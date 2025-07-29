using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands.Tests{
    public class MockTerrainControl : IControlTerrain
    {
        public Matrix4x4 localToWorldMatrix{get; private set;}
        public Matrix4x4 worldToLocalMatrix{get; private set;}
        public void SetMatrix(Matrix4x4 matrix){
            localToWorldMatrix = matrix.inverse;
            worldToLocalMatrix = matrix;
        }
        
        private ILayoutChunks chunkLayout;
        public GameObject gameObject => null;
        public Transform transform => null;

        public Vector2 localGridOffset{get; set;}

        public int maxLodGenerated{get; set;}

        public IGraph graph => throw new NotImplementedException();

        public MeshSettings meshSettings{get; set;}

        public bool ShouldDoThat => throw new NotImplementedException();

        public bool InstantProcessors => false;

        public Action<IControlTerrain> InitializeComponent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Action DisableComponent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Action UpdateComponent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Action GraphUpdate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void ChangeLayout(LandsLayout selectedLayout){
            switch(selectedLayout){
                case LandsLayout.QuadTree:
                    chunkLayout = new QuadLayout();
                    break;
                default: 
                    chunkLayout = new SingleLayout();
                    break;
            }
        }
       
        public ILayoutChunks GetChunkLayout() => chunkLayout;

        public IRenderChunk GetChunkRenderer()
        {
            throw new NotImplementedException();
        }

        public void ChangeGenerator(IGraph generator)
        {
            throw new NotImplementedException();
        }

        public void ChangeVisualizer(bool infiniteMode, bool byEditor)
        {
            throw new NotImplementedException();
        }

        public void ChangeChunkRenderer(LandsTemplate enumValue, bool forced)
        {
            throw new NotImplementedException();
        }

        public void ChangeTemplate(LandsTemplate enumValue)
        {
            throw new NotImplementedException();
        }

        public T GetInternalComponent<T>() => default;
        public void AddComponent(Type type, bool initialize = true)
        {
            throw new NotImplementedException();
        }

        public void RemoveComponent(Type type)
        {
            throw new NotImplementedException();
        }

        public void AddMonoForLifetime(InfiniteLandsMonoBehaviour monoBehaviour)
        {
            throw new NotImplementedException();
        }

        public void RemoveMonoForLifetime(InfiniteLandsMonoBehaviour monoBehaviour)
        {
            throw new NotImplementedException();
        }

        public void ClearComponents()
        {
            throw new NotImplementedException();
        }

        public void StartCoroutine(IEnumerator coroutine)
        {
            throw new NotImplementedException();
        }

        public ViewSettings GetViewSettings()
        {
            throw new NotImplementedException();
        }
    }
}