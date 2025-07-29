using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct HeightAtPoint : AwaitableData<float>
    {
        public float Result{get; private set;}
        public Vector2 MinMaxRange;

        HeighDataExtractor awaitableData;
        private HeightData previousResult;
        private TreeData newSettings;
        private int SubState;
        private HeightMapBranch heightBranch;
        public HeightAtPoint(HeighDataExtractor awaiterHeightData, TreeData newSettings)
        {
            awaitableData = awaiterHeightData;
            this.newSettings = newSettings;
            this.heightBranch = newSettings.GetTrunk().GetData<HeightMapBranch>();
            Result = 0;
            SubState = 0;
            previousResult = default;
            MinMaxRange = default;
        }
        public bool ProcessData()
        {
            if(SubState == 0){
                if(!awaitableData.ProcessData()) return false;
                previousResult = awaitableData.Result;
                SubState++;
            }

            if(SubState == 1){
                if (!newSettings.ForcedOrFinished(previousResult.jobHandle)) return false;

                previousResult.jobHandle.Complete();
                SubState++;
            }

            if(SubState == 2){
                var map = heightBranch.GetMap();
                var targetMiddleIndex = Mathf.RoundToInt(previousResult.indexData.Resolution/2.0f);
                var targetFlatIndex = MapTools.VectorToIndex(new int2(targetMiddleIndex, targetMiddleIndex), previousResult.indexData.Resolution);
                var targetIndex = previousResult.indexData.StartIndex+targetFlatIndex;
                if(targetIndex > map.Length){
                    Debug.LogError("Something went wrong!");
                }
                else
                    Result = map[targetIndex];

                MinMaxRange = previousResult.minMaxValue;
                newSettings.CloseTree();
                SubState ++;
            }

            return SubState == 3;
            
        }
    }
}