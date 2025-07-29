using UnityEngine;

namespace sapra.InfiniteLands{
    public interface ILoadPoints : IOutput
    {
        public GameObject GetPrefab();
        public string GetPrefabName();
        public bool alignWithTerrain { get; }
    }
}