using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class BasicGridNode : InfiniteLandsNode
    {
        [Output] public GridData OutputGrid;
        protected override bool Process(BranchData branch)
        {
            ReturnableBranch returnableBranch = branch.GetData<ReturnableBranch>();
            HeightMapBranch heightMapBranch = branch.GetData<HeightMapBranch>();
            var resolution = heightMapBranch.GetMaxResolution();
            var length = MapTools.LengthFromResolution(resolution);
            var points = returnableBranch.GetData<float3>(length);
            var trueScale = resolution * branch.ResolutionToScaleRatio;
            var result = SimpleGridMap.ScheduleParallel(points,
                    resolution, trueScale, default);

            OutputGrid = new GridData(points, resolution, trueScale, result);
            return true;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(OutputGrid, nameof(OutputGrid));
        }
    }
}