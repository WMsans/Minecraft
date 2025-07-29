using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    public class ScaleGridNode : InfiniteLandsNode{

        [Input] public GridData BaseGrid;
        [Output] public GridData OutputGrid;

        public float Amount;

        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out BaseGrid, nameof(BaseGrid));
        }

        protected override bool Process(BranchData branch)
        {
            JobHandle dependancy = BaseGrid.jobHandle;
            ReturnableBranch returnableBranch = branch.GetData<ReturnableBranch>();
            var points = returnableBranch.GetData<float3>(BaseGrid.meshGrid.Length);

            JobHandle finalJob = ScaleJob.ScheduleParallel(BaseGrid.meshGrid, BaseGrid.Resolution,
                    points, BaseGrid.Resolution,
                    Amount, branch.terrain.Position, dependancy);

                
            OutputGrid = new GridData(points, BaseGrid.Resolution, BaseGrid.MeshScale, finalJob);
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(OutputGrid, nameof(OutputGrid));
        }
    }
}