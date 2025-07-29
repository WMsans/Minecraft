using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class PointInstance
    {
        public readonly PointInstance PreviousInstance;
        public readonly IProcessPoints Processor;
        public readonly float GridSize;
        public readonly int MaxPlacementDistance = 4;
        public readonly int Seed;

        private Dictionary<Vector2Int, List<PointTransform>> PointsInGrid = new();
        private Dictionary<Vector2Int, WaitingFor> ProcessingPoints = new();

        private List<PointTransform> NewPoints;
        private List<PointTransform> AllPoints;
        private bool StoreNewPoints;
        private readonly PointManager pointManager;

        private bool ReadOnlyInstance;

        public PointInstance(IProcessPoints Processor, float gridSize, PointInstance PreviousInstance, PointManager pointManager, int seed, bool StoreNewPoints = false)
        {
            this.Processor = Processor;
            this.GridSize = gridSize;
            this.pointManager = pointManager;
            this.PreviousInstance = PreviousInstance;
            this.StoreNewPoints = StoreNewPoints;
            this.Seed = seed;
            ReadOnlyInstance = false;
            AllPoints = new();
            if (StoreNewPoints)
                NewPoints = new();
        }

        public void AddPoints(Vector2Int id, List<PointTransform> points)
        {
            PointsInGrid.Add(id, points);
        }
        public void SetReadonly()
        {
            ReadOnlyInstance = true;
        }

        public HeightAtPoint GetDataAtPoint(InfiniteLandsNode node, string fieldName,
            Vector3 position, MeshSettings meshSettings)
        {
            return pointManager.GetDataAtPoint(node, fieldName, position, meshSettings);
        }
/* 
        public HeightAtPoint GetDataAtPoint(InfiniteLandsNode node, string fieldName, 
            Vector3 position, MeshSettings meshSettings)
        {
            return GetDataAtPoint(node, fieldName, position, meshSettings, out _);
        }
 */
        public bool GetAllPoints(float size, Vector3 origin, out List<PointTransform> AllPoints){
            return GetPoints(size, origin, out AllPoints); 
        }
        
        public bool GetAllPoints(PointGenerationSettings settings, out List<PointTransform> AllPoints){
            return GetPoints(settings.CheckupSize, settings.Origin, out AllPoints); 
        }


        public bool GetNewPoints(float size, Vector3 origin, out List<PointTransform> NewPoints)
        {
            if (!StoreNewPoints)
            {
                Debug.LogWarning("Retrieving new points where it's not allowed!");
                NewPoints = new();
                return true;
            }
            var isCompleted = GetPoints(size, origin, out _);
            NewPoints = new List<PointTransform>(this.NewPoints);
            this.NewPoints.Clear();
            return isCompleted;
        }

        public bool GetAllPointsInMesh(BranchData settings, out List<PointTransform> AllPoints){
            float size = settings.meshSettings.MeshScale;
            Vector3 origin = settings.terrain.Position;
            return GetAllPoints(size, origin, out AllPoints);
        }

        public bool GetNewPointsInMesh(BranchData settings, out List<PointTransform> NewPoints){
            float size = settings.meshSettings.MeshScale;
            Vector3 origin = settings.terrain.Position;
            return GetNewPoints(size, origin, out NewPoints);
        }

        private bool GetPoints(float size, Vector3 origin, out List<PointTransform> results){
            return RetrieveAndReturn(size, origin, out results);
        }

        private struct WaitingFor{
            public Vector2Int id;
            public AwaitableData<List<PointTransform>> awaitable;
            public WaitingFor(Vector2Int id, AwaitableData<List<PointTransform>> awaitable){
                this.id = id;
                this.awaitable = awaitable;
            }
        }

        private bool RetrieveAndReturn(float size, Vector3 origin, out List<PointTransform> results){
            Vector2 position = new Vector2(origin.x, origin.z);
            Vector2Int mapped = Vector2Int.RoundToInt(position/GridSize);
            int pointsToCheck = Mathf.Max(Mathf.CeilToInt(size*0.5f/GridSize), 1);
           
            AllPoints.Clear();
            results = AllPoints;
            if(pointsToCheck > MaxPlacementDistance){
                return true;
            }

            if(size == GridSize)
                pointsToCheck = 0;

            var waitingForData = ListPoolLight<WaitingFor>.Get();
            for(int x = -pointsToCheck; x <= pointsToCheck; x++){
                for(int y = -pointsToCheck; y <= pointsToCheck; y++){
                    Vector2Int gridPosition = new Vector2Int(x,y)+mapped;
                    if(PointsInGrid.TryGetValue(gridPosition, out List<PointTransform> found)){
                        results.AddRange(found);
                    }
                    else if(!ReadOnlyInstance){
                        if(ProcessingPoints.TryGetValue(gridPosition, out WaitingFor waitingFor)){
                            waitingForData.Add(waitingFor);
                        }
                        else{
                            Vector3 chunkOrigin = new Vector3(gridPosition.x, 0, gridPosition.y)*GridSize;
                            PointGenerationSettings pointSettings = new PointGenerationSettings(gridPosition, GridSize, chunkOrigin);
                            var generatedPoints = Processor.ProcessDataSpace(this, pointSettings);
                            var quickComplete = new AfterCompletition(PointsInGrid, ProcessingPoints, results, NewPoints, StoreNewPoints);
                            WaitingFor waiting = new WaitingFor(gridPosition, generatedPoints);
                            if(!quickComplete.Callback(waiting)){
                                ProcessingPoints.Add(gridPosition, waiting);
                                waitingForData.Add(waiting);
                            }
                        }
                    }
                }
            }

            var completer = new AfterCompletition(PointsInGrid, ProcessingPoints, results, NewPoints, StoreNewPoints);
            var allDone = AwaitableTools.IterateOverItems(waitingForData, ref completer);
            ListPoolLight<WaitingFor>.Release(waitingForData);

            if(allDone){
                return true;
            }

            return false;
        }
        private struct AfterCompletition : ICallMethod<WaitingFor>
        {
            private Dictionary<Vector2Int, List<PointTransform>> PointsInGrid;
            private Dictionary<Vector2Int, WaitingFor> ProcessingPoints;
            private List<PointTransform> FoundPoints;
            private List<PointTransform> NewPoints;

            private bool StoreNewPoints;
            public AfterCompletition(Dictionary<Vector2Int, List<PointTransform>> PointsInGrid, 
                Dictionary<Vector2Int, WaitingFor> ProcessingPoints, 
                List<PointTransform> FoundPoints,List<PointTransform> NewPoints,bool StoreNewPoints)
            {
                this.PointsInGrid = PointsInGrid;
                this.ProcessingPoints = ProcessingPoints;
                this.FoundPoints = FoundPoints;
                this.NewPoints = NewPoints;
                this.StoreNewPoints = StoreNewPoints;
            }
            public bool Callback(WaitingFor value)
            {
                if(!value.awaitable.ProcessData()) return false;
                
                PointsInGrid.Add(value.id, new List<PointTransform>(value.awaitable.Result));
                FoundPoints.AddRange(value.awaitable.Result);

                if (StoreNewPoints)
                    NewPoints.AddRange(value.awaitable.Result);

                value.awaitable.Result.Clear();
                ProcessingPoints.Remove(value.id);
                return true;
            }
        }
    }
}