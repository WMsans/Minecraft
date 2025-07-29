using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class LandmarkPlacer : ChunkProcessor<ChunkData>
    {
        private Transform LandmarkParent;
        private Dictionary<string, LandmarkManager> PrefabManagers = new();
        private FloatingOrigin floatingOrigin;

        private List<LandmarkResult> ToCreate = new();
        public int ConsecutiveSpawns = 10;


        private List<PointTransform> previewPoints = new();
        protected override void DisableProcessor()
        {
            foreach (var keyValye in PrefabManagers)
            {
                if (keyValye.Value != null)
                    keyValye.Value.Disable();
            }

            if (LandmarkParent != null)
            {
                AdaptiveDestroy(LandmarkParent.gameObject);
                LandmarkParent = null;
            }
            PrefabManagers.Clear();
        }

        protected override void InitializeProcessor()
        {
            LandmarkParent = RuntimeTools.FindOrCreateObject("Landmarks", transform).transform;

            floatingOrigin = GetComponent<FloatingOrigin>();
            ToCreate.Clear();
            previewPoints.Clear();
        }

        public override void OnGraphUpdated()
        {
            Disable();
            Initialize(infiniteLands);
        }

        protected override void OnProcessAdded(ChunkData chunk)
        {
            var nodes = chunk.worldGenerator.nodes;
            var settings = chunk.GetMainTree().GetTrunk();
            foreach (var node in nodes)
            {
                if (!node.isValid) continue;
                if (node is ILoadPoints pointLoader)
                {
                    var writeableNode = settings.GetWriteableNode(node);
                    if (!writeableNode.TryGetOutputData(settings, out PointInstance points, pointLoader.OutputVariableName))
                        continue;
                    var prefab = pointLoader.GetPrefab();
                    var name = pointLoader.GetPrefabName();
                    if (points != null)
                    {
                        points.GetNewPointsInMesh(settings, out var newPoints);
                        if (newPoints != null)
                        {
                            LandmarkResult result = new LandmarkResult(prefab, name, new List<PointTransform>(newPoints), pointLoader.alignWithTerrain);
                            ToCreate.Add(result);
                        }
                    }
                }
            }

            var isValidPreview = GraphSettingsController.TryGetPreviewData(infiniteLands.graph, settings, out PointInstance previewResult);
            if (isValidPreview)
            {
                if (previewResult.GetAllPointsInMesh(settings, out var points))
                {
                    previewPoints.AddRange(points);
                    previewPoints = previewPoints.Distinct().ToList();
                }
            }
        }

        public LandmarkManager GetPrefabParent(GameObject prefab, string prefabName, bool AlignWithTerrain)
        {
            if (!PrefabManagers.TryGetValue(prefabName, out var prefabManager))
            {
                var parent = transform.Find(prefabName);
                if (!parent)
                {
                    var existTransf = RuntimeTools.CreateObjectAndRecord(prefabName);
                    existTransf.transform.SetParent(LandmarkParent);
                    parent = existTransf.transform;
                }
                prefabManager = new LandmarkManager(prefab, parent, AlignWithTerrain, floatingOrigin != null);
                PrefabManagers.Add(prefabName, prefabManager);
            }
            return prefabManager;
        }

        protected override void OnProcessRemoved(ChunkData chunk)
        {
        }

        public override void Update()
        {
            int currentCount = 0;
            for (int t = ToCreate.Count - 1; t >= 0; t--)
            {
                var land = ToCreate[t];
                var pnts = land.newPoints;
                var prefab = land.prefab;
                if (prefab == null || pnts == null) continue;

                var finalPoints = land.newPoints;
                LandmarkManager prefabManager = GetPrefabParent(prefab, land.PrefabName, land.AlignWithTerrain);
                for (int i = finalPoints.Count - 1; i >= 0; i--)
                {
                    PointTransform point = finalPoints[i];
                    prefabManager.CreateObject(point, infiniteLands.localToWorldMatrix);
                    finalPoints.RemoveAt(i);
                    currentCount++;
                    if (currentCount >= ConsecutiveSpawns)
                        return;
                }
                ToCreate.RemoveAt(t);
            }
        }

        public override void OnDrawGizmos()
        {
            if (Camera.current == null) return;
            foreach (var point in previewPoints)
            {

                point.MultiplyMatrix(infiniteLands.localToWorldMatrix, Vector3.one, Vector3.zero,
                    out Vector3 position, out Quaternion rotation, out Vector3 finalScale);

                var matrix = Matrix4x4.TRS(position, rotation, finalScale);
                Gizmos.matrix = matrix;

                float distance = Vector3.Distance(Camera.current.transform.position, position);
                float dynamicScale = Mathf.Max(point.Scale, 0.05f * distance);
                if (dynamicScale > point.Scale)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(Vector3.zero, Vector3.one*dynamicScale);
                }

                Gizmos.color = Color.green;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }
        }
    }
}
