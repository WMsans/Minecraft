using UnityEngine;
using System.Collections.Generic;
using System;

namespace sapra.InfiniteLands
{
    [CustomNode("Find By Tag", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/input/findbytag.html")]
    public class FindByTag : InfiniteLandsNode, IProcessPoints
    {
        [Output] public PointInstance Output;
        [Tag] public string tag = "Untagged";

        public string processorID => guid;
        private PointManager manager;
        private bool Skip;

        public AwaitableData<List<PointTransform>> ProcessDataSpace(PointInstance currentPoints, PointGenerationSettings pointSettings)
        {
            Debug.Log("this shouldn't happen");
            return default;
        }
        protected override bool SetInputValues(BranchData branch)
        {
            manager = branch.GetGlobalData<PointManager>();
            Skip = manager.TryGetValue(this, branch.meshSettings.Seed, out Output, out _);
            return true;
        }

        protected override bool Process(BranchData branch)
        {
            if (Skip) return true;
            
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            var length = taggedObjects.Length;
            double average = 0;
            uint count = 0;
            // calc dist for each distinct pair Dist(P_1, P_2) == Dist(P_2, P_1)
            for (var i = 0; i < length - 1; i++)
            {
                for (var j = i + 1; j < length; j++)
                {
                    var positionI = taggedObjects[i].transform.position;
                    var positionJ = taggedObjects[j].transform.position;

                    var dX = positionI.x - positionJ.x;
                    var dY = positionI.y - positionJ.y;
                    // don't calculate the Square Root yet
                    var dist = dX * dX + dY * dY;
                    average += dist;
                    count++;
                }
            }

            average = Math.Sqrt(average / Mathf.Max(1, count));
            float casted = Mathf.Max(200, (float)average);

            Output = manager.GetPointInstance(this, casted, null, branch.meshSettings.Seed);
            Output.SetReadonly();

            Dictionary<Vector2Int, List<PointTransform>> FoundPoints = new();
            HashSet<Vector2Int> createdPoints = HashSetPoolLight<Vector2Int>.Get();
            for (int i = 0; i < length; i++)
            {
                var obj = taggedObjects[i].transform;
                var id = Vector3Int.FloorToInt(obj.position / casted + Vector3.one * 0.5f);
                var finalId = new Vector2Int(id.x, id.z);
                createdPoints.Add(finalId);
                if (!FoundPoints.TryGetValue(finalId, out var currentPoints))
                {
                    currentPoints = new List<PointTransform>();
                    FoundPoints.Add(finalId, currentPoints);
                }

                PointTransform pointTransform = new PointTransform()
                {
                    Position = obj.position,
                    Scale = obj.localScale.magnitude,
                    YRotation = obj.eulerAngles.y
                };

                currentPoints.Add(pointTransform);
            }

            foreach (var point in createdPoints)
            {
                Output.AddPoints(point, FoundPoints[point]);
            }
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}