using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class HeightToNormalNode : InfiniteLandsNode, IHeightMapConnector
    {
        [Input] public HeightData Input;
        [Input] public GridData Grid;
        [Output] public NormalMapData NormalMap;

        public void  ConnectHeightMap(PathData currentBranch, float scaleToResolutionRatio, int acomulatedResolution)
        {
            currentBranch.ApplyInputPadding(this, nameof(Input), 1, acomulatedResolution);
        }

        protected override bool SetInputValues(BranchData branch)
        {
            bool input = TryGetInputData(branch, out Input, nameof(Input));
            bool grid = TryGetInputData(branch, out Grid, nameof(Grid));
            return input && grid;
        }

        protected override bool Process(BranchData branch)
        {
            int length = MapTools.LengthFromResolution(Input.indexData.Resolution);
            IndexAndResolution real = Input.indexData;
            NormalMapData resultingData = new NormalMapData();
            resultingData.indexData = new IndexAndResolution(0, MapTools.IncreaseResolution(real.Resolution, -1), length);
            resultingData.NormalMap = branch.GetData<ReturnableBranch>().GetData<float3>(length);
            resultingData.HeighData = Input;
            JobHandle combined = JobHandle.CombineDependencies(Grid.jobHandle, Input.jobHandle);
            var heightMapBranch = branch.GetData<HeightMapBranch>();
            resultingData.jobHandle = GenerateNormalMap.ScheduleParallel(Grid.meshGrid, Grid.Resolution, resultingData.NormalMap, heightMapBranch.GetMap(),
                resultingData.indexData, real, combined);
            NormalMap = resultingData;
            return true;
   
        }



        protected override void CacheOutputValues()
        {
            CacheOutputValue(NormalMap, nameof(NormalMap));
        }
    }
}