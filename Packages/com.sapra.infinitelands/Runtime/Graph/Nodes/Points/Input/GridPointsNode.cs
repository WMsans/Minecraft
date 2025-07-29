using UnityEngine;
using System.Collections.Generic;

namespace sapra.InfiniteLands
{
    [CustomNode("Grid", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/input/grid")]
    public class GridPointsNode : InfiniteLandsNode, IProcessPoints
    {
        [Output] public PointInstance Output;
        [Min(40)] public float GridSize = 140;
        [Min(0.001f)] public float Scale = 1;

        public string processorID => guid;
        public AwaitableData<List<PointTransform>> ProcessDataSpace(PointInstance currentPoints, PointGenerationSettings pointSettings)
        {
            GridPoints gridPoints = GenericPoolLight<GridPoints>.Get();
            gridPoints.Reuse(pointSettings,Scale);
            return gridPoints;
        }

        protected override bool Process(BranchData branch)
        {
            PointManager manager = branch.GetGlobalData<PointManager>();
            Output = manager.GetPointInstance(this, GridSize, null, branch.meshSettings.Seed);
            return true;
        }
        
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }

        private class GridPoints : AwaitableData<List<PointTransform>>
        {
            public List<PointTransform> Result{get; private set;}
            public GridPoints(){
                Result = new();
            }
            public bool ProcessData() => true;

            public void Reuse(PointGenerationSettings pointSettings, float size){
                Result.Clear();
                Result.Add(new PointTransform(){
                    Position = pointSettings.Origin,
                    Scale = size,
                    YRotation = 0
                });   
                GenericPoolLight.Release(this);
            }
        }

    }
}