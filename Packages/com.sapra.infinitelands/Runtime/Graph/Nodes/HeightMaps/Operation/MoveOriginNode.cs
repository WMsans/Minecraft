using UnityEngine;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    [CustomNode("Move Origin", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/moveorigin")]
    public class MoveOriginNode : InfiniteLandsNode, IHeightMapConnector
    {
        public Vector2 NewPosition;

        [Input] public HeightData Input;
        [Output] public HeightData Output;

        private BranchData NewBranchSettings;
        public void  ConnectHeightMap(PathData currentBranch, float scaleToResolutionRatio, int acomulatedResolution)
        {
            currentBranch.AllocateOutputs(this);
        }

        protected override bool SetInputValues(BranchData branch)
        {
            if (state.SubState == 0)
            {
                TerrainConfiguration configuration = new TerrainConfiguration(branch.terrain);
                configuration.Position -= new Vector3(NewPosition.x, 0, NewPosition.y);
                NewBranchSettings = BranchData.NewTerrainSettings(configuration, branch, GetNodesInInput(nameof(Input)));
                state.IncreaseSubState();
            }
            if (state.SubState == 1)
            {
                if (!TryGetInputData(NewBranchSettings, out Input, nameof(Input))) return false;
                state.IncreaseSubState();
            }
            return state.SubState == 2;
        }

        protected override bool Process(BranchData branch)
        {
            Output = GraphTools.CopyHeightFromTo(this, nameof(Output), Input, NewBranchSettings, branch);
            return true;
        }

        protected override void CacheOutputValues()
        {
            CacheOutputValue(Output, nameof(Output));
        }
    }
}