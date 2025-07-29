using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public enum LandsTemplate{Default, UnityTerrain, Custom}
    public enum LandsLayout{QuadTree, SimpleGrid}

    public interface IControlTerrain
    {
        public GameObject gameObject{get;}
        public Transform transform{get;}
        public Vector2 localGridOffset{get;}
        public Matrix4x4 localToWorldMatrix{get;}
        public Matrix4x4 worldToLocalMatrix{get;}
        public int maxLodGenerated{get;}
        public bool InstantProcessors{ get; }
        public IGraph graph { get; }
        public MeshSettings meshSettings{get;}

        public IRenderChunk GetChunkRenderer();
        public ILayoutChunks GetChunkLayout();

        public ViewSettings GetViewSettings();
        public void ChangeGenerator(IGraph generator);
        public void ChangeVisualizer(bool infiniteMode, bool byEditor);
        public void ChangeLayout(LandsLayout selectedLayout);
        public void ChangeChunkRenderer(LandsTemplate enumValue, bool forced);
        public void ChangeTemplate(LandsTemplate enumValue);

        public T GetInternalComponent<T>();
        public void AddComponent(Type type, bool initialize = true);
        public void RemoveComponent(Type type);
        public void StartCoroutine(IEnumerator coroutine);

        public void AddMonoForLifetime(InfiniteLandsMonoBehaviour monoBehaviour);
        public void RemoveMonoForLifetime(InfiniteLandsMonoBehaviour monoBehaviour);
        public void ClearComponents();
    }
}