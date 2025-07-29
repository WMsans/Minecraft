using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Place Prefab", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/output/placeprefab")]
    public class PlacePrefabNode : InfiniteLandsNode, ILoadPoints, IProcessPoints
    {
        [Input] public PointInstance Positions;
        [Input, Disabled, HideIf(nameof(AlignWithTerrain))] public HeightData HeightMap;

        [Output, Hide] public PointInstance Output;
        public GameObject Prefab;

        public string processorID => guid;
        public bool alignWithTerrain => AlignWithTerrain;

        public string OutputVariableName => nameof(Output);

        public bool AlignWithTerrain = true;
        public bool AffectedByScale = true;
        public bool AffectedByRotation = true;
        private string prefabName;
        public override void Restart(IGraph graph)
        {
            if(Prefab != null)
                prefabName = Prefab.name;
            base.Restart(graph);
        }
        public GameObject GetPrefab() => Prefab;
        public string GetPrefabName() => prefabName;
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Positions, nameof(Positions));
        }

        protected override bool Process(BranchData branch)
        {
            PointManager manager = branch.GetGlobalData<PointManager>();
            Output = manager.GetPointInstance(this, Positions.GridSize, Positions, branch.meshSettings.Seed, true);
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
        
        public AwaitableData<List<PointTransform>> ProcessDataSpace(PointInstance currentPoints, PointGenerationSettings pointSettings)
        {
            PlacePointsIntoTerrain placer = GenericPoolLight<PlacePointsIntoTerrain>.Get();
            placer.Reuse(this,currentPoints, pointSettings);
            return placer;
        }
        
        private class PlacePointsIntoTerrain : AwaitableData<List<PointTransform>>
        {
            public List<PointTransform> Result { get; private set; }
            PlacePrefabNode node;

            private PointGenerationSettings pointSettings;
            private PointInstance previousPoints;
            private PointInstance currentPoints;
            private int SubState = 0;
            private List<AwaitableHeight> AwaitableHeights = new();
            private bool HeightAssigned;
            private int Seed;

            private struct AwaitableHeight
            {
                public HeightAtPoint heightData;
                public PointTransform point;
            }
            public void Reuse(PlacePrefabNode node, PointInstance currentPoints, PointGenerationSettings pointSettings)
            {
                this.node = node;
                this.Seed = currentPoints.Seed;
                this.pointSettings = pointSettings;
                this.previousPoints = currentPoints.PreviousInstance;
                this.currentPoints = currentPoints;
                AwaitableHeights.Clear();
                HeightAssigned = node.IsAssigned(nameof(HeightMap));
                if (Result != null)
                    Result.Clear();
                else
                    Result = new List<PointTransform>();
                SubState = 0;
            }
            public bool ProcessData()
            {
                if (SubState == 0)
                {
                    if (!previousPoints.GetAllPoints(pointSettings, out var foundPoints)) return false;

                    foreach (var point in foundPoints)
                    {
                        var currentPoint = point;
                        if (!node.AffectedByScale)
                            currentPoint.Scale = 1;

                        if (!node.AffectedByRotation)
                            currentPoint.YRotation = 0;

                        if (node.AlignWithTerrain || (!HeightAssigned && !node.AlignWithTerrain))
                        {
                            Result.Add(currentPoint);
                            continue;
                        }

                        MeshSettings meshSettings = new MeshSettings()
                        {
                            Resolution = 3,
                            MeshScale = 100,
                            Seed = Seed,
                        };
                        var height = currentPoints.GetDataAtPoint(node, nameof(HeightMap), point.Position, meshSettings);
                        AwaitableHeights.Add(new AwaitableHeight()
                        {
                            heightData = height,
                            point = point
                        });
                    }
                    SubState++;
                }

                if (SubState == 1)
                {
                    var placer = new PlacePointIntoTerrain(Result);
                    if (AwaitableTools.IterateOverItems(AwaitableHeights, ref placer))
                    {
                        GenericPoolLight.Release(this);
                        SubState++;
                    }
                }

                return SubState == 2;
            }

            private struct PlacePointIntoTerrain : ICallMethod<AwaitableHeight>
            {
                List<PointTransform> Result;
                public PlacePointIntoTerrain(List<PointTransform> result)
                {
                    this.Result = result;
                }
                public bool Callback(AwaitableHeight value)
                {
                    if (!value.heightData.ProcessData()) return false;

                    var height = value.heightData.Result;
                    var point = value.point;
                    Vector3 grounded = height * new float3(0, 1, 0) + point.Position;
                    Result.Add(point.UpdatePosition(grounded));
                    return true;
                }
            }
        }
    }
}