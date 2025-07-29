using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public readonly struct LandmarkResult
    {
        public readonly GameObject prefab;
        public readonly string PrefabName;
        public readonly List<PointTransform> newPoints;
        public readonly bool AlignWithTerrain;
        public LandmarkResult(GameObject prefab, string _prefabName, List<PointTransform> newPoints, bool alignWithTerrain)
        {
            this.prefab = prefab;
            this.newPoints = newPoints;
            this.AlignWithTerrain = alignWithTerrain;
            this.PrefabName = _prefabName;
        }
    }
}