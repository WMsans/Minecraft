using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    [CustomNode("Spawn Around", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/input/spawnaround")]
    public class SpawnAroundNode : InfiniteLandsNode, IProcessPoints
    {
        [Input] public PointInstance Origins;
        [Output] public PointInstance Output;
        public bool IncludeOriginalPoints;
        [Min(0.001f)] public float Scale = 1;


        [Min(1)] public int MinCount = 3;
        [Min(1)] public int MaxCount = 10;
        public bool CircularRandomization;
        public Vector2 MinMaxDistance = new Vector2(10,20);
        public string processorID => guid;

        public AwaitableData<List<PointTransform>> ProcessDataSpace(PointInstance currentPoints, PointGenerationSettings pointSettings)
        {
            SpawnAroundPoints gridPoints = GenericPoolLight<SpawnAroundPoints>.Get();
            gridPoints.Reuse(currentPoints, pointSettings, this);
            return gridPoints;
        }

        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Origins, nameof(Origins));
        }

        protected override bool Process(BranchData branch)
        {
            PointManager manager = branch.GetGlobalData<PointManager>();
            Output = manager.GetPointInstance(this, Origins.GridSize, Origins, branch.meshSettings.Seed);
            return true;
        }
        
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }

        private class SpawnAroundPoints : AwaitableData<List<PointTransform>>
        {
            public List<PointTransform> Result{get; private set;}
            SpawnAroundNode node;
            PointInstance currentPoints;
            PointInstance previousPoints;

            PointGenerationSettings pointSettings;
            int SubState;
            public SpawnAroundPoints()
            {
                Result = new();
            }

            public void Reuse(PointInstance currentPoints, PointGenerationSettings pointSettings, SpawnAroundNode node)
            {
                this.node = node;
                this.currentPoints = currentPoints;
                this.pointSettings = pointSettings;
                this.SubState = 0;
                previousPoints = currentPoints.PreviousInstance;
            }
            public bool ProcessData()
            {
                if (SubState == 0)
                {
                    if (!previousPoints.GetAllPoints(pointSettings, out var foundPoints)) return false;

                    int chunkSeed = currentPoints.Seed + pointSettings.forChunkID.x * 123 + pointSettings.forChunkID.y * 123123;
                    System.Random random = new System.Random(chunkSeed);
                    foreach (var point in foundPoints)
                    {
                        int amountToGenerate = random.Next(node.MinCount, node.MaxCount);
                        for (int i = 0; i < amountToGenerate; i++)
                        {
                            float maxAngle = random.Next(0, 200) / 200.0f;
                            float angle = 6.283185307f * Mathf.Lerp((float)i / amountToGenerate, maxAngle, node.CircularRandomization ? 1 : 0);

                            float maxDistance = random.Next(0, 200) / 200.0f;
                            float3 jittered = point.Position + new float3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * Mathf.Lerp(node.MinMaxDistance.x, node.MinMaxDistance.y, maxDistance);
                            Result.Add(new PointTransform()
                            {
                                Position = jittered,
                                Scale = node.Scale,
                                YRotation = 0
                            });
                        }

                        if (node.IncludeOriginalPoints)
                        {
                            Result.Add(point);
                        }
                    }
                    GenericPoolLight.Release(this);
                    SubState++;
                }

                return SubState == 1;
            }
        }

    }
}